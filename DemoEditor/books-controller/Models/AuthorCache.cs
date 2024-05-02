using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// This class corresponds to an author as cached by the book referential
// It may not contain all the data in the complete author model
public class AuthorCache
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

    public AuthorCache(Author author)
    {
        EntityId = author.EntityId;
        FirstName = author.FirstName;
        LastName = author.LastName;
        UserEmailAddress = author.UserEmailAddress;
        Restriction = author.Restriction;
    }

    public Author ConvertToAuthor()
    {
        return new Author() {
            EntityId = this.EntityId,
            FirstName = this.FirstName,
            LastName = this.LastName,
            UserEmailAddress = this.UserEmailAddress,
            Restriction = this.Restriction  
        };
    }
}