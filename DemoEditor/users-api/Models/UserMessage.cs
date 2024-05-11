using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace users_api.Models;

public class UserMessage
{
    [BsonId()]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore]
    public ObjectId TechnicalId { get; set; }

    public string DestinationUserEntityId { get; set; }

    public string Title { get; set; }

    public string Message { get; set; }

    public bool HasBeenRead { get; set; } = false;
}