
using FraudAssessment.Application.Interface;
using FraudAssessment.Application.MessagingInterface;
using FraudAssessment.Infrastructure.Messaging.OutboxMessage;
using FraudAssessment.Infrastructure.Messaging.RabbitMq;
using FraudAssessment.Infrastructure.Persistence.MongoDB.Context;
using FraudAssessment.Infrastructure.Persistence.MongoDB.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RabbitMQ.Client;

namespace FraudAssessment.Infrastructure.DependencyInjection;

public static class InfrastructureDependencies
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        #region Database

        services.AddSingleton<IMongoClient>(_ =>
        {
            var connectionString =
                configuration["MongoDB:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "MongoDB connection string is missing.");

            return new MongoClient(connectionString);
        });

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var databaseName =
                configuration["MongoDB:DatabaseName"]
                ?? throw new InvalidOperationException(
                    "MongoDB database name is missing.");

            var client = sp.GetRequiredService<IMongoClient>();

            return client.GetDatabase(databaseName);
        });

        services.AddSingleton<AssessmentDbContext>();

        #endregion

        #region Initializers

        services.AddSingleton<MongoDbInitializer>();
        services.AddSingleton<RabbitMqTopologyInitializer>();

        #endregion

        #region RabbitMQ

        services.AddSingleton<IConnection>(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"]
                    ?? throw new InvalidOperationException("RabbitMQ HostName is missing."),

                Port = configuration.GetValue<int>("RabbitMQ:Port"),

                UserName = configuration["RabbitMQ:UserName"]
                    ?? throw new InvalidOperationException("RabbitMQ UserName is missing."),

                Password = configuration["RabbitMQ:Password"]
                    ?? throw new InvalidOperationException("RabbitMQ Password is missing.")
            };

            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddSingleton<IMessageBus, MessageBus>();



        #endregion

        #region Repositories

        services.AddScoped<IAssessmentRepository, AssessmentRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IAssessmentUnitOfWork, AssessmentUnitOfWork>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        #endregion

        #region Workers
        var enableAssessmentWorker =
          configuration.GetValue<bool>("RabbitMQ:EnableAssessmentWorker");

        if (enableAssessmentWorker)
        {
            services.AddHostedService<AssessmentProcessingWorker>();
        }
        #endregion


        #region Messaging

        services.AddScoped<IOutboxPublisher, OutboxPublisher>();

        services.AddHostedService<OutboxBackgroundService>();

        #endregion

        return services;
    }
}
