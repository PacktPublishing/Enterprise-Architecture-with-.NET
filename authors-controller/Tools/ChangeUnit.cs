using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ChangeUnit
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

    [BsonElement("patchContent")]
    [Required]
    public BsonArray PatchContent { get; set; }
}
