using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using QualityDMS.Domain.Interfaces;

namespace QualityDMS.Infrastructure.Services;

public sealed class MongoDocumentSearchService : IDocumentSearchService
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger<MongoDocumentSearchService> _logger;

    public MongoDocumentSearchService(IConfiguration configuration, ILogger<MongoDocumentSearchService> logger)
    {
        _logger = logger;
        var connectionString = configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
        var client = new MongoClient(connectionString);
        _collection = client.GetDatabase("dms_metadata").GetCollection<BsonDocument>("file_tags");
    }

    public async Task<IReadOnlyList<int>> SearchDocumentIdsAsync(string query, CancellationToken ct = default)
    {
        var filter = Builders<BsonDocument>.Filter.Text(query);
        var projection = Builders<BsonDocument>.Projection
            .MetaTextScore("score")
            .Include("doc_id")
            .Exclude("_id");
        var sort = Builders<BsonDocument>.Sort.MetaTextScore("score");

        var docs = await _collection
            .Find(filter)
            .Project(projection)
            .Sort(sort)
            .Limit(50)
            .ToListAsync(ct);

        return docs
            .Where(d => d.Contains("doc_id"))
            .Select(d => d["doc_id"].AsInt32)
            .ToList();
    }
}
