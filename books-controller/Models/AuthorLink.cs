using MongoDB.Bson.Serialization.Attributes;

public class AuthorLink : Link
{
    [BsonElement("userEmailAddress")]
    public string UserEmailAddress { get; set; }

    [BsonElement("fullEntity")]
    public Author? FullEntity { get; set; }
}

