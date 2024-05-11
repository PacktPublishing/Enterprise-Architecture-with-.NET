namespace users_api.Models;

// TODO: put in common, as this is used also in a petal from the authors data referential service, 
// but take into account the differences, in particular the addition of the type of address 
// and the use of multiple mails, where the mail is outside the contacts for the authors

public class Contacts
{
    public List<Address> Addresses { get; set; } = new List<Address>();

    public List<Phone> Phones { get; set; } = new List<Phone>();

    public List<Email> Emails { get; set; } = new List<Email>();

    public GUINotifInfo Portal { get; set; } = new GUINotifInfo();
}

public class Address
{
    public string AddressType { get; set; }

    public string? StreetNumber { get; set; }

    public string? StreetName { get; set; }

    public string? CityName { get; set; }

    public string? ZipCode { get; set; }

    public CountryLink? Country { get; set; }
}

public class Phone
{
    // Type of phone, following IANA type defined in RFC 2426
    public string IANAType { get; set; }

    // Phone number, following E.123 standard
    public string Number { get; set; }
}

public class Email
{
    // Type of mail, using the same IANA type defined in RFC 2426 as for the phones
    public string IANAType { get; set; }

    public string EmailAddress { get; set; }
}

public class GUINotifInfo
{
    public string UserEntityId { get; set; }

    public string DisplayMode { get; set; } = "menu"; // For now, we only support displaying notifications in GUI throught the dedicated menu entry
}

public class CountryLink : Link
{
    // 3-letter country code, as per ISO3166
    public string ISOCode { get; set; }
}

public class Link
{
    public string Rel { get; set; }

    public string Href { get; set; }

    public string Title { get; set; }
}
