public class Book
{
    public string BusinessId { get; set; }
    public string? ISBN { get; set; }
    public int? NumberOfPages { get; set; }
    public DateOnly? PublishDate { get; set; }
    public EditingPetal? Editing { get; set; }
    public SalesPetal Sales { get; set; }
}