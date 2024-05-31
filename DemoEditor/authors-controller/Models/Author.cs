using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;

namespace authors_controller.Models;

public class Author
{
    [BsonId()]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore]
    public ObjectId TechnicalId { get; set; }

    [BsonElement("entityId")]
    [Required(ErrorMessage = "Business identifier of an author is mandatory")]
    public string EntityId { get; set; }

    [BsonElement("firstName")]
    public string? FirstName { get; set; }

    [BsonElement("lastName")]
    public string? LastName { get; set; }

    [BsonElement("userEmailAddress")]
    public string? UserEmailAddress { get; set; }

    [BsonElement("restriction")]
    public string? Restriction { get; set; }

    [BsonElement("contacts")]
    public ContactsPetal? Contacts { get; set; }

    public object Clone()
    {
        string serialized = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<Author>(serialized);
    }
}