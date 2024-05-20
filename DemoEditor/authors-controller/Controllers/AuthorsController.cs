using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using authors_controller.Models;
using authors_controller.Tools;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Mail;

namespace authors_controller.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly string ConnectionString;

    private readonly IMongoDatabase Database;

    private readonly ILogger<AuthorsController> _logger;

    private IHttpClientFactory _clientFactory;

    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IMemoryCache _memoryCache;

    public AuthorsController(IConfiguration config, ILogger<AuthorsController> logger, IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        _httpContextAccessor = httpContextAccessor;
        _memoryCache = memoryCache;
        ConnectionString = config.GetValue<string>("AuthorsConnectionString") ?? "mongodb://db:27017";
        Database = new MongoClient(ConnectionString).GetDatabase("authors");
    }

    [Authorize(Policy = "editor")]
    [HttpPut]
    [Route("Subscribe")]
    public IActionResult Subscribe([FromQuery] string callbackURL, [FromQuery(Name = "$filter")] string filter)
    {
        // Very naive implementation, without persistance
        if (!filter.StartsWith("EntityId eq '")) throw new ArgumentException("Filter only supports EntityId");
        string authorEntityId = filter.Substring(13).TrimEnd('\'');
        if (!_memoryCache.TryGetValue("OnChangeCallbacks", out Dictionary<string, Uri> OnChangeCallbacks))
            OnChangeCallbacks = new();
        if (!OnChangeCallbacks.ContainsKey(authorEntityId))
            OnChangeCallbacks.Add(authorEntityId, new Uri(callbackURL));
        _memoryCache.Set("OnChangeCallbacks", OnChangeCallbacks);
        return Ok();
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("Status")]
    public ActionResult Status()
    {
        // If the ping to the database works, the controller is considered as working fine
        if (Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000))
            return Ok();
        else
            return StatusCode(500);
    }

    [AllowAnonymous] // This is typically where we could use a One-Time Token for security without identification
    [HttpPost]
    [Route("{authorId}/RequestForContact")]
    public async Task<IActionResult> RequestForContact(string authorId)
    {
        var smtpClient = new SmtpClient("mail")
        {
            Port = 1025,
            //Credentials = new NetworkCredential("username", "password"),
            EnableSsl = false,
        };
        var mailMessage = new MailMessage
        {
            From = new MailAddress("authors-service@demoeditor.org"),
            Subject = $"Someone please contact author {authorId}",
        };
        mailMessage.To.Add("editors@demoeditor.org");
        smtpClient.Send(mailMessage);
        return Ok();
    }

    [Authorize(Policy = "editor-apikey")]
    [HttpGet]
    public IActionResult Get(
        [FromQuery(Name = "$orderby")] string orderby = "",
        [FromQuery(Name = "$skip")] int skip = 0,
        [FromQuery(Name = "$top")] int top = 20,
        [FromQuery(Name = "$filter")] string filter = "")
    {
        // Starting with the filtering content
        var builder = Builders<Author>.Filter;
        FilterDefinition<Author> queryFilter = null;
        if (!string.IsNullOrEmpty(filter))
        {
            int posFirstName = filter.IndexOf("firstName eq '");
            if (posFirstName > -1)
            {
                int posEndFirstName = filter.IndexOf("'", posFirstName + 14);
                string firstName = filter.Substring(posFirstName + 14, posEndFirstName - posFirstName - 14);
                if (queryFilter is null)
                    queryFilter = builder.Eq(x => x.FirstName, firstName);
                else 
                    queryFilter &= builder.Eq(x => x.FirstName, firstName);
            }

            int posLastName = filter.IndexOf("lastName eq '");
            if (posLastName > -1)
            {
                int posEndLastName = filter.IndexOf("'", posLastName + 13);
                string lastName = filter.Substring(posLastName + 13, posEndLastName - posLastName - 13);
                if (queryFilter is null)
                    queryFilter = builder.Eq(x => x.LastName, lastName);
                else
                    queryFilter &= builder.Eq(x => x.LastName, lastName);
            }
        }

        // The Find method of the MongoDB driver supports ordering
        var query = queryFilter is null
            ? Database.GetCollection<Author>("authors-bestsofar").Find(x => true)
            : Database.GetCollection<Author>("authors-bestsofar").Find(queryFilter);
        if (!string.IsNullOrEmpty(orderby))
        {
            string jsonSort = string.Empty;
            foreach (string item in orderby.Split(','))
            {
                if (item.ToLower().EndsWith(" desc"))
                    jsonSort += "," + item.Split(' ')[0] + ":-1";
                else if (item.ToLower().EndsWith(" asc"))
                    jsonSort += "," + item.Split(' ')[0] + ":1";
                else if (item.Length > 0)
                    jsonSort += "," + item + ":1";
            }
            query.Sort("{" + jsonSort.Substring(1) + "}");
        }

        // It also supports active pagination
        var result = query.Skip(skip).Limit(top).ToList();
        return new JsonResult(result);
    }

    [Authorize(Policy = "editor")]
    [HttpGet]
    [Route("$count")]
    public long GetAuthorsCount()
    {
        // The count is of course done on the best so far collection, otherwise it would add up historical states
        return Database.GetCollection<Author>("authors-bestsofar").CountDocuments(r => true);
    }

    [Authorize(Policy = "editor")]
    [HttpGet]
    [Route("{entityId}")]
    public IActionResult GetUnique(string entityId, [FromQuery] DateTimeOffset? providedValueDate = null)
    {
        if (providedValueDate is null)
        {
            // As long as no parameter is added, the behaviour is simply to find an retrieve the best known state
            var result = Database.GetCollection<Author>("authors-bestsofar").Find(item => item.EntityId == entityId);
            if (result.CountDocuments() == 0)
                return new NotFoundResult();
            else
            {
                Author author = result.First();
                return new JsonResult(author);
            }
        }
        else
        {
            // Naive implementation at first, to be evolved to a single query only when actual performance is at stake
            var states = Database.GetCollection<ObjectState<Author>>("authors-states").Find(item => item.EntityId == entityId);
            if (states.CountDocuments() == 0)
                return new NotFoundResult();
            else
            {
                var results = states.SortBy(item => item.ValueDate).ToList();
                if (providedValueDate < results[0].ValueDate)
                    return new NotFoundObjectResult("Provided value date is prior to entity creation");
                int index = 0;
                do
                {
                    index++;
                }
                while (results[index].ValueDate < providedValueDate);
                return new JsonResult(results[index].State);
            }
        }
    }

    [Authorize(Policy = "editor")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Author author, 
        [FromQuery] DateTimeOffset? providedValueDate = null)
    {
        // If no entity identifier is provided, a business rule states that a GUID should be attributed
        if (string.IsNullOrEmpty(author.EntityId))
            author.EntityId = Guid.NewGuid().ToString("N");

        // Creating an entity is in fact nothing else than patching an empty state
        JsonPatchDocument equivPatches = JSONPatchHelper.CreatePatch(
            new Author() { EntityId = author.EntityId }, 
            author,
            new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() }
        );

        // After creating the equivalent patch, we thus pass this over to the PATCH operation
        return await Patch(author.EntityId, equivPatches, providedValueDate);
    }
    
    [Authorize(Policy = "editor")]
    [HttpPatch]
    [Route("{entityId}")]
    public async Task<IActionResult> Patch(
        string entityId,
        [FromBody] JsonPatchDocument patch,
        [FromQuery] DateTimeOffset? providedValueDate = null)
    {
        // If the patch could not be deserialized, better stopping right away
        if (patch is null) return BadRequest();

        // Creating a complete change unit and inserting in the actions collection
        DateTimeOffset valueDate = providedValueDate.GetValueOrDefault(DateTimeOffset.UtcNow);
        ChangeUnit patchAction = new ChangeUnit()
        {
            EntityId = entityId,
            ValueDate = valueDate,
            PatchContent = new BsonArray(patch.Operations.ConvertAll<BsonDocument>(item => item.ToBsonDocument()))
        };
        var collectionActions = Database.GetCollection<BsonDocument>("authors-changes");
        await collectionActions.InsertOneAsync(patchAction.ToBsonDocument());

        // Retrieving the potential existing best so far state of the business entity
        var result = Database.GetCollection<Author>("authors-bestsofar").Find(item => item.EntityId == entityId);
        var author = result.FirstOrDefault();

        // If it does not exist already, we create it and query it again
        if (author == null)
        {
            author = new Author() { EntityId = entityId };
            var bestknown = Database.GetCollection<BsonDocument>("authors-bestsofar");
            await bestknown.InsertOneAsync(author.ToBsonDocument());
            result = Database.GetCollection<Author>("authors-bestsofar").Find(item => item.EntityId == entityId);
            author = result.First();
        }

        // Applying the patch of change to the state, in order to determine the new state
        patch.ApplyTo(author);
        ObjectState<Author> nouvelEtat = new ObjectState<Author>()
        {
            EntityId = entityId,
            ValueDate = valueDate,
            State = author
        };

        // This new state is added to the list of ongoing states of the entity
        var collectionEtats = Database.GetCollection<BsonDocument>("authors-states");
        await collectionEtats.InsertOneAsync(nouvelEtat.ToBsonDocument());

        // It is also used as a replacement of the best so far
        var collectionBestKnown = Database.GetCollection<Author>("authors-bestsofar");
        await collectionBestKnown.FindOneAndReplaceAsync<Author>(
            p => p.EntityId == entityId,
            author
        );

        // If all went well, we send the registered callbacks associated with the webhook for change events
        // No sophistication here, we simply send as a sequence, without error management, because eventual consistency is ensured some other way
        HttpClient? client = null;
        if (!_memoryCache.TryGetValue("OnChangeCallbacks", out Dictionary<string, Uri> OnChangeCallbacks))
            OnChangeCallbacks = new();
        if (OnChangeCallbacks.ContainsKey(entityId))
        {
            // When sending author information to services that have registered on the webhook, a business rule states that no address should be communicated
            Author strippedAuthor = (Author)author.Clone();
            strippedAuthor.Contacts = null;

            // Client is only created if we have indeed a callback to execute, in order to spare resources
            // Authentication is reused from the current API execution context; there may be better ways than copying the JWT
            if (client is null)
            {
                client = _clientFactory.CreateClient("Callbacks");
                var accessToken = Request.Headers["Authorization"];
                string jwt = accessToken.ToString().Replace("Bearer ", "");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            }

            // In order to be exactly compatible on the serialization, we use the dedicated model class (TODO: group all model classes in a library shared between the two APIs and even the client)
            await client.PutAsJsonAsync<Author>(OnChangeCallbacks[entityId], strippedAuthor);
        }

        // Finally, the object is returned in order to show potential changes due to business rules
        return new ObjectResult(author);
    }
}
