using MongoDB.Bson.Serialization.Attributes;

namespace books_controller.Models;

public class ProspectAuthorLink : AuthorLink
{
    [BsonElement("prospectionStatus")]
    public string ProspectionStatus { get; set; }

    // TODO: add decoupled link to middle office supporting request
}

