
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FraudAssessment.Infrastructure.Messaging.RabbitMq;

public class RabbitMqTopologyInitializer
{
    private readonly IConnection _connection;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqTopologyInitializer> _logger;

    public RabbitMqTopologyInitializer(
        IConnection connection,
        IConfiguration configuration,
        ILogger<RabbitMqTopologyInitializer> logger)
    {
        _connection = connection;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await using var channel =
            await _connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

        var exchangeName =
            _configuration["RabbitMQ:ExchangeName"]!;

        const string queueName = "assessment-processing";
        const string routingKey = "ProcessAssessment";

        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "RabbitMQ topology initialized successfully.");
    }
}
