namespace DTO;

public class CountryLink : Link
{
    public string? ISOCode { get; set; }
}

public class Link
{
    public string Rel { get; set; }
    public string Href { get; set; }
    public string Title { get; set; }
}
