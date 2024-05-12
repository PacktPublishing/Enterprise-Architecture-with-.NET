using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace books_controller.Models;

public class Book : ICloneable
{
    [BsonId()]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore]
    public ObjectId TechnicalId { get; set; }

    [BsonElement("entityId")]
    [Required(ErrorMessage = "Business identifier of a book is mandatory")]
    public string EntityId { get; set; }

    [BsonElement("isbn")]
    public string? ISBN { get; set; }

    [BsonElement("title")]
    public string? Title { get; set; }

    [BsonElement("numberOfPages")]
    public int? NumberOfPages { get; set; }

    [BsonElement("publishDate")]
    public DateTime? PublishDate { get; set; }

    [BsonElement("editing")]
    public EditingPetal? Editing { get; set; }

    [BsonElement("sales")]
    public SalesPetal? Sales { get; set; }

    public object Clone()
    {
        string serialized = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<Book>(serialized);
    }
}