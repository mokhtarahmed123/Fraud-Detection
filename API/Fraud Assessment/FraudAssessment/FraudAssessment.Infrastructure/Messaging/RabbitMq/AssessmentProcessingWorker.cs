
using FraudAssessment.Application.Interface;
using FraudAssessment.Application.Models;
using FraudAssessment.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FraudAssessment.Infrastructure.Messaging.RabbitMq;

public class AssessmentProcessingWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<AssessmentProcessingWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private const string QueueName = "assessment-processing";

    public AssessmentProcessingWorker(
        IConnection connection,
        ILogger<AssessmentProcessingWorker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _connection = connection;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await using var channel =
            await _connection.CreateChannelAsync(
                cancellationToken: stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                await using var scope =
                    _scopeFactory.CreateAsyncScope();

                var assessmentRepository = scope.ServiceProvider
                    .GetRequiredService<IAssessmentRepository>();

                // 1. Read message.
                var json = Encoding.UTF8.GetString(
                    eventArgs.Body.ToArray());

                _logger.LogInformation(
                    "Received assessment message. MessageId: {MessageId}",
                    eventArgs.BasicProperties.MessageId);

                // 2. Deserialize message.
                var processMessage =
                    JsonSerializer.Deserialize<ProcessAssessmentMessage>(
                        json,
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));

                if (processMessage is null)
                    throw new InvalidOperationException(
                        "Invalid assessment message.");

                // 3. Retrieve assessment.
                var assessment = await assessmentRepository
                    .GetAssessmentByIdAsync(
                        processMessage.AssessmentId,
                        stoppingToken);

                if (assessment is null)
                    throw new InvalidOperationException(
                        $"Assessment {processMessage.AssessmentId} not found.");

                // 4. Check current status.
                if (assessment.Status != AssessmentStatus.Received)
                {
                    _logger.LogWarning(
                        "Assessment {AssessmentId} cannot be processed again. Current Status: {Status}",
                        assessment.Id,
                        assessment.Status);

                    await channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        false,
                        cancellationToken: stoppingToken);

                    return;
                }

                // 5. Validate assessment type.
                if (assessment.Type != processMessage.Type)
                {
                    throw new InvalidOperationException(
                        "Assessment type does not match message type.");
                }

                // 6. Apply domain logic.
                assessment.StartProcessing();

                // 7. Atomic update.
                var updated = await assessmentRepository
                    .TryUpdateReceivedAssessmentAsync(
                        assessment.Id,
                        assessment,
                        stoppingToken);

                if (!updated)
                {
                    _logger.LogWarning(
                        "Assessment {AssessmentId} was already claimed by another consumer.",
                        assessment.Id);

                    await channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        false,
                        cancellationToken: stoppingToken);

                    return;
                }

                // 8. Determine target service.
                switch (assessment.Type)
                {
                    case AssessmentType.Payment:
                        _logger.LogInformation(
                            "Assessment {AssessmentId} routed to Payment Fraud Service.",
                            assessment.Id);
                        break;

                    case AssessmentType.AccountChange:
                        _logger.LogInformation(
                            "Assessment {AssessmentId} routed to ATO Detection Service.",
                            assessment.Id);
                        break;
                    case AssessmentType.Login:
                        _logger.LogInformation(
                            "Assessment {AssessmentId} routed to ATO Detection Service.",
                            assessment.Id);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported assessment type: {assessment.Type}");
                }

                // 9. Acknowledge message.
                await channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    false,
                    cancellationToken: stoppingToken);

                _logger.LogInformation(
                    "Assessment {AssessmentId} started processing successfully.",
                    assessment.Id);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing assessment message.");

                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Assessment Processing Worker started. Listening to queue: {QueueName}",
            QueueName);

        await Task.Delay(
            Timeout.InfiniteTimeSpan,
            stoppingToken);
    }
}
