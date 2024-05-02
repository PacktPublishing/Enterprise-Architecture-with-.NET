namespace DTO;

public class Book
{
    public string EntityId { get; set; }
    public string? ISBN { get; set; }
    public string? Title { get; set; }
    public int? NumberOfPages { get; set; }
    public DateTime? PublishDate { get; set; }
    public EditingPetal? Editing { get; set; }
    public SalesPetal Sales { get; set; }
}

public class EditingPetal
{
    public int? NumberOfChapters { get; set; }
    public Status? Status { get; set; }
    public AuthorLink? mainAuthor { get; set; }
}

public class AuthorLink : Link
{
    public string UserEmailAddress { get; set; }
    //public Author? FullEntity { get; set; }
    
    public string Id
    {
        get
        {
            int pos = Href.LastIndexOf("/");
            return Href.Substring(pos + 1);
        }
    }
}

public class Status
{
    public string Value { get; set; }
}

public class SalesPetal
{
    public MonetaryAmount? Price { get; set; }
    public decimal? WeightInGrams { get; set; }
}

public class MonetaryAmount
{
    public decimal Value { get; set; }
    public string MonetaryUnit { get; set; }
}
