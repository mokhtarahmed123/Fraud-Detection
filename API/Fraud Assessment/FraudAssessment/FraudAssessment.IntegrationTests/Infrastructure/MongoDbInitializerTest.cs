
using FraudAssessment.Domain.Entities;
using FraudAssessment.IntegrationTests.Fixtures;
using MongoDB.Driver;

namespace FraudAssessment.IntegrationTests.Infrastructure;

[Collection("MongoDB Integration Tests")]
public class MongoDbInitializerTests
{
    private readonly MongoDbFixture _fixture;

    public MongoDbInitializerTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateAssessmentIndex()
    {
        // Act
        var indexes = await _fixture.Context.Assessments.Indexes
            .ListAsync();

        var indexList = await indexes.ToListAsync();

        // Assert
        var index = indexList.FirstOrDefault(x =>
            x["name"] == "IX_ClientId_CreatedAt");

        Assert.NotNull(index);

        var keys = index["key"].AsBsonDocument;

        Assert.Equal(1, keys["ClientId"].ToInt32());
        Assert.Equal(-1, keys["CreatedAt"].ToInt32());
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateUniqueIdempotencyIndex()
    {
        // Act
        var indexes = await _fixture.Context.IdempotencyRecords.Indexes
            .ListAsync();

        var indexList = await indexes.ToListAsync();

        // Assert
        var index = indexList.FirstOrDefault(x =>
            x["name"] == "UX_ClientId_Key");

        Assert.NotNull(index);

        var keys = index["key"].AsBsonDocument;

        Assert.Equal(1, keys["ClientId"].ToInt32());
        Assert.Equal(1, keys["Key"].ToInt32());

        Assert.True(index["unique"].AsBoolean);
    }

    [Fact]
    public async Task UniqueIndex_ShouldRejectDuplicateClientIdAndKey()
    {
        // Arrange
        var clientId = "client-duplicate-test";
        var key = "same-idempotency-key";

        var firstRecord = IdempotencyRecord.Create(
            key,
            clientId,
            "HASH-001",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(24));

        var secondRecord = IdempotencyRecord.Create(
            key,
            clientId,
            "HASH-002",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(24));

        // Insert first record
        await _fixture.Context.IdempotencyRecords.InsertOneAsync(
            firstRecord);

        // Act & Assert
        await Assert.ThrowsAsync<MongoWriteException>(() =>
            _fixture.Context.IdempotencyRecords.InsertOneAsync(
                secondRecord));
    }

    [Fact]
    public async Task UniqueIndex_ShouldAllowSameKeyForDifferentClients()
    {
        // Arrange
        var key = "same-key";

        var firstRecord = IdempotencyRecord.Create(
            key,
            "client-A",
            "HASH-001",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(24));

        var secondRecord = IdempotencyRecord.Create(
            key,
            "client-B",
            "HASH-002",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(24));

        // Act
        await _fixture.Context.IdempotencyRecords.InsertOneAsync(
            firstRecord);

        await _fixture.Context.IdempotencyRecords.InsertOneAsync(
            secondRecord);

        // Assert
        var count = await _fixture.Context.IdempotencyRecords.CountDocumentsAsync(
            Builders<IdempotencyRecord>.Filter.In(
                x => x.ClientId,
                new[] { "client-A", "client-B" }));

        Assert.Equal(2, count);
    }
}
