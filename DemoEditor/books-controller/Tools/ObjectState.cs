using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ObjectState<T>
{
    [BsonId()]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore]
    public ObjectId TechnicalId { get; set; }

    [BsonElement("entityId")]
    [Required]
    public string EntityId { get; set; }

    [BsonElement("valueDate")]
    [Required]
    public DateTimeOffset ValueDate { get; set; }

    [BsonElement("state")]
    public T State { get; set; }
}