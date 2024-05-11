namespace users_api.Models;

// Author class here is only used as a support for contact and does not deserialize the rest of the information
public class Author
{
    public string? UserEmailAddress { get; set; }

    // Restriction might be used some day for notification filtering purpose
    public string? Restriction { get; set; }

    public Contacts? Contacts { get; set; }
}