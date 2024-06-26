using MongoDB.Bson.Serialization.Attributes;

namespace authors_controller.Models;

public class ContactsPetal
{
    [BsonElement("addresses")]
    public List<Address>? Addresses { get; set; }

    [BsonElement("phones")]
    public List<Phone>? Phones { get; set; }
}

public class Address
{
    [BsonElement("streetNumber")]
    public string? StreetNumber { get; set; }

    [BsonElement("streetName")]
    public string? StreetName { get; set; }

    [BsonElement("cityName")]
    public string? CityName { get; set; }

    [BsonElement("zipCode")]
    public string? ZipCode { get; set; }

    [BsonElement("country")]
    public CountryLink? Country { get; set; }
}

public class Phone
{
    // Type of phone, following IANA type defined in RFC 2426
    [BsonElement("ianaType")]
    public string IANAType { get; set; }

    // Phone number, following E.123 standard
    [BsonElement("number")]
    public string Number { get; set; }
}