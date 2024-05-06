namespace DTO;

public class Book
{
    public string EntityId { get; set; }
    public string? ISBN { get; set; }
    public string? Title { get; set; }
    public int? NumberOfPages { get; set; }
    public DateTime? PublishDate { get; set; }
    public EditingPetal? Editing { get; set; }
    public SalesPetal Sales { get; set; } = new SalesPetal();

    public string MainTechnoTag
    {
        get { return Editing?.MainTechnoTag ?? string.Empty; }
        set
        {
            if (Editing is null) Editing = new EditingPetal();
            Editing.MainTechnoTag = value;
        }
    }

    public int? NumberOfChapters
    {
        get { return Editing?.NumberOfChapters; }
        set
        {
            if (Editing is null) Editing = new EditingPetal();
            Editing.NumberOfChapters = value;
        }
    }

    public DateTime? PlannedPublishDate
    {
        // This is where we would switch to history reading of the modification of the book
        // attribute if we wanted to display together the planned and actual publish date.
        // For now, we simply consider that the history is enough and that it does not matter
        // if the form shows the actual publish date replacing the planned date, since we are
        // at a different state of the book lifecycle, and this data is disabled or even invisible.
        get { return PublishDate; }
        set { PublishDate = value; }
    }

    public List<AuthorLink> ProposedAuthors
    {
        get { return Editing?.proposedAuthors; }
        set
        {
            if (Editing is null) Editing = new EditingPetal();
            Editing.proposedAuthors = value;
        }
    }

    public string MainAuthorId
    {
        get { return Editing?.mainAuthor?.Id; }
        set
        {
            if (Editing is null) Editing = new EditingPetal();
            if (Editing.mainAuthor is null) Editing.mainAuthor = new AuthorLink();
            Editing.mainAuthor.Id = value;
        }
    }
}

public class EditingPetal
{
    public int? NumberOfChapters { get; set; }
    public string? MainTechnoTag { get; set; }
    public Status? Status { get; set; }
    public AuthorLink? mainAuthor { get; set; }
    public List<AuthorLink> proposedAuthors { get; set; } = new List<AuthorLink>();
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
        set
        {
            int pos = Href.LastIndexOf("/");
            Href = Href.Substring(0, pos + 1) + value;
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
    public int? NumberOfBooksSold { get; set; }
}

public class MonetaryAmount
{
    public decimal Value { get; set; }
    public string MonetaryUnit { get; set; }
}
