namespace DTO;

// TODO: this class also should be put in common, since it is a contract for a generic service
public class UserMessage
{
    public string DestinationUserEntityId { get; set; }

    public string Title { get; set; }

    public string Message { get; set; }

    public bool HasBeenRead { get; set; } = false;
}