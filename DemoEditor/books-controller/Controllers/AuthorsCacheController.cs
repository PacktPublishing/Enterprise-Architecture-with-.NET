using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using books_controller.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace books_controller.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
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
        await Database.GetCollection<AuthorCache>("authors-bestsofar-cache").ReplaceOneAsync<AuthorCache>(item => item.EntityId == author.EntityId, author, new ReplaceOptions { IsUpsert = true });
        return new OkResult();
    }
}
