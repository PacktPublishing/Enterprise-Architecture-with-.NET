namespace DTO;

public class Author
{
    public string EntityId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? UserEmailAddress { get; set; }
    public string? Restriction { get; set; }
    public ContactsPetal? Contacts { get; set; }

    public Address MainAddress
    {
        get
        {
            if (Contacts == null) Contacts = new ContactsPetal();
            if (Contacts.Addresses == null) Contacts.Addresses = new List<Address>();
            if (Contacts.Addresses.Count() == 0) Contacts.Addresses.Add(new Address() { Country = new() });
            if (Contacts.Addresses[0].Country == null) Contacts.Addresses[0].Country = new();
            return Contacts.Addresses.First();
        }
    }

    public List<Phone> SortedPhones
    {
        get 
        {
            if (Contacts?.Phones is null) return new List<Phone>();
            Contacts.Phones.Sort((p1, p2) => ConvertIANATypeToPriority(p1.IANAType) - ConvertIANATypeToPriority(p2.IANAType));
            return Contacts.Phones;
        }
    }

    public string? FirstPhoneNumber
    {
        get
        {
            // if (Contacts == null) Contacts = new ContactsPetal();
            // if (Contacts.Phones == null) Contacts.Phones = new List<Phone>();
            // if (Contacts.Phones.Count() == 0) Contacts.Phones.Add(new Phone() { IANAType = "home" });
            // return Contacts.Phones.First().Number;

            Phone home = SortedPhones.FirstOrDefault();
            if (home != null) return home.Number;
            return "N/A";
        }
        set
        {
            Phone home = SortedPhones.FirstOrDefault();
            if (home != null) home.Number = value;
            else
            {
                if (Contacts == null) Contacts = new ContactsPetal();
                if (Contacts.Phones == null) Contacts.Phones = new List<Phone>();
                Contacts.Phones.Add(new Phone() { IANAType = "home", Number = value });
            }
        }
    }

    public string? SecondPhoneNumber
    {
        get
        {
            if (SortedPhones.Count() < 2) return "N/A";
            return SortedPhones[1].Number;
        }
        set
        {
            if (Contacts == null) Contacts = new ContactsPetal();
            if (Contacts.Phones == null) Contacts.Phones = new List<Phone>();
            if (Contacts.Phones.Count() == 0)
                Contacts.Phones.Add(new Phone() { IANAType = "home", Number = "N/A" });
            if (Contacts.Phones.Count() == 1)
                Contacts.Phones.Add(new Phone() { IANAType = "cell", Number = value });
            SortedPhones[1].Number = value;
        }
    }

    public string Title
    {
        get { return FirstName + " " + LastName.ToUpper(); }
    }

    private static int ConvertIANATypeToPriority(string IANAType)
    {
        // Gives the order in which the types of phones will be displayed
        switch (IANAType)
        {
            case "home": return 0;
            case "cell": return 1;
            default: return 2;
        }
    }

    // public string? HomePhone
    // {
    //     get
    //     {
    //         Phone home = Contacts?.Phones?.FirstOrDefault(p => p.IANAType == "home");
    //         Console.WriteLine("HomePhone est lu");
    //         if (home != null) return home.Number;
    //         return "N/A";
    //     }
    //     set
    //     {
    //         Console.WriteLine("HomePhone a changé");
    //         Phone home = Contacts?.Phones?.FirstOrDefault(p => p.IANAType == "home");
    //         if (home != null) home.Number = value;
    //         else
    //         {
    //             if (Contacts == null) Contacts = new ContactsPetal();
    //             if (Contacts.Phones == null) Contacts.Phones = new List<Phone>();
    //             Contacts.Phones.Add(new Phone() { IANAType = "home", Number = value });
    //         }
    //     }
    // }

    // public string? MobilePhone
    // {
    //     get
    //     {
    //         Phone cell = Contacts?.Phones?.FirstOrDefault(p => p.IANAType == "cell");
    //         if (cell != null) return cell.Number;
    //         return "N/A";
    //     }
    //     set
    //     {
    //         Phone cell = Contacts?.Phones?.FirstOrDefault(p => p.IANAType == "cell");
    //         if (cell != null) cell.Number = value;
    //         else
    //         {
    //             if (Contacts == null) Contacts = new ContactsPetal();
    //             if (Contacts.Phones == null) Contacts.Phones = new List<Phone>();
    //             Contacts.Phones.Add(new Phone() { IANAType = "cell", Number = value });
    //         }
    //     }
    // }

    public bool IsBlocked
    {
        get { return Restriction != "none"; }
        set { if (value) Restriction = "blocked"; else Restriction = "none"; }
    }
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
