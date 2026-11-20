
using FraudAssessment.Domain.Entities;
using FraudAssessment.Domain.Enums;
using FraudAssessment.Infrastructure.Persistence.MongoDB.Context;
using FraudAssessment.Infrastructure.Persistence.MongoDB.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace FraudAssessment.Test.IntegrationTests.Persistence;

public class AssessmentRepositoryTests
{
    static AssessmentRepositoryTests()
    {
        BsonSerializer.RegisterSerializer(
            new GuidSerializer(GuidRepresentation.Standard));
    }

    [Fact]
    public async Task TryUpdateReceivedAssessmentAsync_WhenCalledConcurrently_OnlyOneShouldSucceed()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDB:ConnectionString"] =
                    Environment.GetEnvironmentVariable("MONGODB_TEST_CONNECTION_STRING")
                    ?? "mongodb://127.0.0.1:27017/?replicaSet=rs0",

                ["MongoDB:DatabaseName"] =
                    "FraudAssessment_IntegrationTests"
            })
            .Build();

        var context = new AssessmentDbContext(configuration);

        var repository = new AssessmentRepository(
            context,
            NullLogger<AssessmentRepository>.Instance);

        var assessment = Assessment.Create(
            "concurrency-client",
            "concurrency-account",
            AssessmentType.Payment);

        try
        {
            await context.Assessments.InsertOneAsync(assessment);

            // Read the same Assessment twice before either update.
            var firstAssessment =
                await repository.GetAssessmentByIdAsync(
                    assessment.Id, CancellationToken.None);

            var secondAssessment =
                await repository.GetAssessmentByIdAsync(
                    assessment.Id, CancellationToken.None);

            Assert.NotNull(firstAssessment);
            Assert.NotNull(secondAssessment);

            Assert.Equal(
                AssessmentStatus.Received,
                firstAssessment.Status);

            Assert.Equal(
                AssessmentStatus.Received,
                secondAssessment.Status);

            // Simulate two consumers processing separate copies.
            firstAssessment.StartProcessing();
            secondAssessment.StartProcessing();

            // Act
            var results = await Task.WhenAll(
                repository.TryUpdateReceivedAssessmentAsync(
                    assessment.Id,
                    firstAssessment,
                    CancellationToken.None),

                repository.TryUpdateReceivedAssessmentAsync(
                    assessment.Id,
                    secondAssessment,
                    CancellationToken.None)
            );

            // Assert
            Assert.Single(results.Where(result => result));

            Assert.Single(results.Where(result => !result));

            var finalAssessment =
                await repository.GetAssessmentByIdAsync(
                    assessment.Id,
                    CancellationToken.None);

            Assert.NotNull(finalAssessment);

            Assert.Equal(
                AssessmentStatus.Processing,
                finalAssessment.Status);
        }
        finally
        {
            // Cleanup
            var filter = Builders<Assessment>.Filter
                .Eq(x => x.Id, assessment.Id);

            await context.Assessments.DeleteOneAsync(filter);
        }
    }
}
