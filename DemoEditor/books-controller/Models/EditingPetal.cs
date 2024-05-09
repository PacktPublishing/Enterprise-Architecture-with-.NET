using MongoDB.Bson.Serialization.Attributes;

namespace books_controller.Models;

public class EditingPetal
{
    [BsonElement("numberOfChapters")]
    public int? NumberOfChapters { get; set; }

    [BsonElement("status")]
    public Status? Status { get; set; }

    [BsonElement("mainAuthor")]
    public AuthorLink? mainAuthor { get; set; }

    [BsonElement("prospectAuthors")]
    public List<ProspectAuthorLink> ProspectAuthors { get; set; } = new List<ProspectAuthorLink>();
}

public class Status
{
    [BsonElement("value")]
    public string Value { get; set; }
}