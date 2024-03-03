var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var statuses = new[]
{
    "Idea", "AuthorChosen", "ContentDefined", "Writing", "Editing", "ReadyToPrint", "Available", "RetiredFromSales", "Archived"
};

var books =  Enumerable.Range(1, 3).Select(index =>
    new Book
    (
        index.ToString(),
        "978-2-409-03806-" + index.ToString(),
        Random.Shared.Next(400, 850),
        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
        new EditingPetal
        (
            Random.Shared.Next(5, 30),
            statuses[Random.Shared.Next(statuses.Length)]
        ),
        new SalesPetal
        (
            new decimal(Random.Shared.Next(1000, 10000) / 100),
            new decimal(Random.Shared.Next(200, 3500))
        )
    ))
    .ToArray();

app.MapGet("/books", () =>
{
    return books;
})
.WithName("GetBooks")
.WithOpenApi();

app.MapGet("/books/{id}", (string id) => {
    return books.FirstOrDefault(book => book!.BusinessId == id, null);
})
.WithName("GetBook")
.WithOpenApi();

app.Run();

record Book(string BusinessId, string? ISBN, int? NumberOfPages, DateOnly? PublishDate, EditingPetal? EditingProps, SalesPetal SalesProps)
{}

record EditingPetal(int? NumberOfChapters, string? Status)
{}

record SalesPetal(decimal? price, decimal? weight)
{}