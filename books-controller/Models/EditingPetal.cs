using MongoDB.Bson.Serialization.Attributes;

public class EditingPetal
{
    [BsonElement("numberOfChapters")]
    public int? NumberOfChapters { get; set; }

    [BsonElement("status")]
    public Status? Status { get; set; }

    [BsonElement("mainAuthor")]
    public AuthorLink? mainAuthor { get; set; }
}

public class Status
{
    [BsonElement("value")]
    public string Value { get; set; }
}