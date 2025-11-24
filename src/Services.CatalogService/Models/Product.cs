using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Services.CatalogService.Models;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string SellerId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ProductFileUrl { get; set; }   
    public string? PreviewImageUrl { get; set; }
    public bool IsDeleted { get; set; } = false;
    public ProductStatus Status { get; set; } = ProductStatus.PendingApproval;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ProductStatus
{
    PendingApproval,
    Approved,
    Rejected,
    Suspended
}
