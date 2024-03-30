namespace DTO;

public class Author
{
    public string EntityId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? UserEmailAddress { get; set; }
    public string? Restriction { get; set; }
    public ContactsPetal? Contacts { get; set; }
}

public class ContactsPetal
{
    public List<Address>? Addresses { get; set; }
    public List<Phone>? Phones { get; set; }
}

public class Address
{
    public string? StreetNumber { get; set; }
    public string? StreetName { get; set; }
    public string? CityName { get; set; }
    public string? ZipCode { get; set; }
    public CountryLink? Country { get; set; }
}

public class Phone
{
    public string IANAType { get; set; }
    public string Number { get; set; }
}
