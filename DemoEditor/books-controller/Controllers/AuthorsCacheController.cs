using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace books_controller.Controllers;

[ApiController]
[Route("books/[controller]")]
public class AuthorsCacheController : ControllerBase
{
    private readonly string ConnectionString;

    private readonly IMongoDatabase Database;

    private readonly ILogger<AuthorsCacheController> _logger;

    public AuthorsCacheController(IConfiguration config, ILogger<AuthorsCacheController> logger)
    {
        _logger = logger;
        ConnectionString = config.GetValue<string>("BooksConnectionString") ?? "mongodb://db:27017";
        Database = new MongoClient(ConnectionString).GetDatabase("books");
    }

    [HttpPut]
    public async Task<IActionResult> Receive([FromBody] AuthorCache author)
    {
        if (author is null) return BadRequest();
        await Database.GetCollection<AuthorCache>("authors-bestsofar-cache").FindOneAndReplaceAsync<AuthorCache>(item => item.EntityId == author.EntityId, author);
        return new OkResult();
    }
}
