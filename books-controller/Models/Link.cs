using MongoDB.Bson.Serialization.Attributes;

public class Link
{
    [BsonElement("rel")]
    public string Rel { get; set; }

    [BsonElement("href")]
    public string Href { get; set; }

    [BsonElement("title")]
    public string Title { get; set; }
}

