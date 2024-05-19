public class Book
{
    public string EntityId { get; set; }

    public string? ISBN { get; set; }

    public string? Title { get; set; }

    public EditingPetal? Editing { get; set; }
}

public class EditingPetal
{
    public int? NumberOfChapters { get; set; }

    public AuthorLink? mainAuthor { get; set; }
}

public class AuthorLink : Link
{
    public string UserEmailAddress { get; set; }
}

public class Link
{
    public string Href { get; set; }

    public string Title { get; set; }
}
