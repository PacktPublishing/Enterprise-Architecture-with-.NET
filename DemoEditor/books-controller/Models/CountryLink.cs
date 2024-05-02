using MongoDB.Bson.Serialization.Attributes;

namespace books_controller.Models;

public class CountryLink : Link
{
    // 3-letter country code, as per ISO3166
    [BsonElement("code")]
    public string ISOCode { get; set; }
}

