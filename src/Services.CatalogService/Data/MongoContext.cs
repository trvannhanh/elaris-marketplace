using MongoDB.Driver;
using Services.CatalogService.Models;

namespace Services.CatalogService.Data;

public class MongoContext
{
    private readonly IMongoDatabase _db;

    public MongoContext(IConfiguration config)
    {
        var client = new MongoClient(config["Mongo:ConnectionString"]);
        _db = client.GetDatabase(config["Mongo:Database"]);
    }

    //public IMongoCollection<Product> Products => _db.GetCollection<Product>("Products");

    // Thay vì trả về IMongoCollection<Product>, trả về SoftDeleteCollection<Product> để áp dụng Softdelete Middleware/Filter:
    public SoftDeleteCollection<Product> Products
            => new SoftDeleteCollection<Product>(_db.GetCollection<Product>("Products"));
}
