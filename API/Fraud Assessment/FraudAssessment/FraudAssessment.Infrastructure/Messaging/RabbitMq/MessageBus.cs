
using FraudAssessment.Application.MessagingInterface;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;

namespace FraudAssessment.Infrastructure.Messaging.RabbitMq;

public class MessageBus : IMessageBus
{
    private readonly IConnection _connection;
    private readonly string _exchangeName;

    public MessageBus(
        IConnection connection,
        IConfiguration configuration)
    {
        _connection = connection;

        _exchangeName = configuration["RabbitMQ:ExchangeName"]
            ?? throw new InvalidOperationException(
                "RabbitMQ ExchangeName is not configured.");
    }

    public async Task PublishAsync(
        Guid MessageId,
        string eventType,
        string payload,
        CancellationToken cancellationToken)
    {
        await using var channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        var body = Encoding.UTF8.GetBytes(payload);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = eventType,
            MessageId = MessageId.ToString()
        };

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: eventType,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
