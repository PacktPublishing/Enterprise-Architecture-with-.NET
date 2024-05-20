using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Serialization;
using books_controller.Business;
using books_controller.Models;
using books_controller.Tools;
using Microsoft.AspNetCore.Authorization;
using FastExcel;
using System.Net.Mail;

namespace books_controller.Controllers;

[ApiController]
[Route("[controller]")]
public class BooksController : ControllerBase
{
    private readonly string ConnectionString;

    private readonly IConfiguration _configuration;

    private readonly IMongoDatabase Database;

    private readonly ILogger<BooksController> _logger;

    private IHttpClientFactory _clientFactory;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public BooksController(IConfiguration config, ILogger<BooksController> logger, IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = config;
        _logger = logger;
        _clientFactory = clientFactory;
        _httpContextAccessor = httpContextAccessor;
        ConnectionString = config.GetValue<string>("BooksConnectionString") ?? "mongodb://db:27017";
        Database = new MongoClient(ConnectionString).GetDatabase("books");
    }

    [Authorize(Policy = "director")]
    [HttpPut]
    [Route("Import")]
    public async Task<IActionResult> ImportBooksCatalog([FromQuery] string? localFileAddress = "/tmp/DemoEditor-BooksCatalog.xlsx")
    {
        if (!System.IO.File.Exists(localFileAddress))
            return new NotFoundObjectResult(string.Format("Could not find books catalog at {0}", localFileAddress));

        // When in migration period, we simply remove everything from the database before each call
        await Database.GetCollection<ChangeUnit>("books-changes").DeleteManyAsync(item => true);
        await Database.GetCollection<ObjectState<Book>>("books-states").DeleteManyAsync(item => true);
        await Database.GetCollection<Book>("books-bestsofar").DeleteManyAsync(item => true);

        using (FastExcel.FastExcel excelReader = new FastExcel.FastExcel(new FileInfo(localFileAddress), true))
        {
            Worksheet worksheet = excelReader.Read("Books");
            var rows = worksheet.Rows.ToArray();
            for (int i = 1; i < rows.Length; i++)
            {
                var cells = rows[i].Cells.ToArray();
                Book b = new Book() {
                    EntityId = i.ToString().PadLeft(4, '0'),
                    ISBN = (string)cells[0].Value,
                    Title = (string)cells[1].Value,
                    Editing = new EditingPetal() {
                        Status = new Status { Value = "edit" }
                    },
                    Sales = new SalesPetal() {
                        NumberOfCopiesSold = int.Parse((string)cells[5].Value) // Beware of FastExcel bug that still lurks (https://github.com/rap2hpoutre/fast-excel/issues/32) and use value for each edition in order to avoid index-skipping of empty cell
                    }
                };

                try
                {
                    if (((string)cells[6].Value).StartsWith("### FINISHED")) b.Editing.Status.Value = "sell";
                } 
                catch {}

                HttpClient client = GetAuthenticatedClient("Authors");
                try
                {
                    // We try to retrieve authors by their full name; if it does not work, business people will tell if it is better to improve the algorithm with a "close enough" match or if the volume does not justify it and they will simply correct the mismatched author names in the initial books catalog
                    string titleFromImport = (string)cells[2].Value;
                    int posSpace = titleFromImport.IndexOf(" ");
                    string filter = posSpace == -1
                        ? ("lastName eq '" + titleFromImport + "'")
                        : ("firstName eq '" + titleFromImport.Substring(0, posSpace) + "' and lastName eq '" + titleFromImport.Substring(posSpace + 1) + "'");

                    Author[] authors = await client.GetFromJsonAsync<Author[]>("/Authors/?$filter=" + filter);
                    if (authors.Length > 0) 
                    {
                        // URLs are hardcoded in order to better explain what is done by the code
                        b.Editing.mainAuthor = new AuthorLink() {
                            Rel = "dc.creator",
                            Href = "http://authors:82/Authors/" + authors[0].EntityId,
                            Title = authors[0].FirstName + " " + authors[0].LastName.ToUpper(),
                        };
                    }
                }
                catch
                {
                    _logger.LogInformation("Could not find author {title} using the full name", (string)cells[2].Value);
                }

                // It has been decided by the business owners that the data migrated from the old books catalog should appear in the new referential as if it had been created at the beginning of 2024
                DateTimeOffset valueDateForMigratedData = new DateTimeOffset(2024, 1, 1, 0, 0, 0, new TimeSpan(1, 0, 0));
                await this.Create(b, valueDateForMigratedData);
            }
            return new OkObjectResult(string.Format("{0} lines have been imported", rows.Length));
        }
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
    [Route("{bookId}/AuthorsAcceptingWritingProposal/{authorId}")]
    public async Task<IActionResult> ReceiveAcceptedProposal(string bookId, string authorId)
    {
        var smtpClient = new SmtpClient("mail")
        {
            Port = 1025,
            //Credentials = new NetworkCredential("username", "password"),
            EnableSsl = false,
        };
        var mailMessage = new MailMessage
        {
            From = new MailAddress("editors@demoeditor.org"),
            Subject = "A prospect author has accepted writing proposal",
            // TODO: expose HTML pages for the links below or adjust IAM so that new tabs of portal gets authenticated, if possible since it is a Single Page Application and not a web site
            Body = $"""
                <p>Book project: {bookId}</p>
                <p>Author accepting: <a href='http://portal:88/author/{authorId}'>{authorId}</a></p>
                """,
            IsBodyHtml = true
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
        [FromQuery(Name = "$top")] int top = 20)
    {
        // The Find method of the MongoDB driver supports ordering, and takes into account archived books are not seen by default
        var query = Database.GetCollection<Book>("books-bestsofar").Find(r => r.Editing == null || r.Editing.Status == null || r.Editing.Status.Value != "archived");
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

    [HttpGet]
    [Route("$count")]
    public long GetBooksCount()
    {
        // The count is of course done on the best so far collection, otherwise it would add up historical states
        return Database.GetCollection<Book>("books-bestsofar").CountDocuments(r => r.Editing == null || r.Editing.Status == null || r.Editing.Status.Value != "archived");
    }

    [HttpGet]
    [Route("{entityId}")]
    public async Task<IActionResult> GetUnique(string entityId, [FromQuery] DateTimeOffset? providedValueDate = null)
    {
        if (providedValueDate is null)
        {
            // As long as no parameter is added, the behaviour is simply to find an retrieve the best known state
            var result = Database.GetCollection<Book>("books-bestsofar").Find(item => item.EntityId == entityId);
            if (result.CountDocuments() == 0)
                return new NotFoundResult();
            else
            {
                Book book = result.First();
                string? expand = HttpContext.Request.Query["$expand"];
                if (book.Editing != null && book.Editing.mainAuthor != null 
                    && expand != null && expand.Contains("mainAuthor"))
                {
                    HttpClient client = GetAuthenticatedClient("Authors");
                    try
                    {
                        int pos = book.Editing.mainAuthor.Href.LastIndexOf("/");
                        string authorEntityId = book.Editing.mainAuthor.Href.Substring(pos + 1);

                        Author? mainAuthor = await client.GetFromJsonAsync<Author>("/Authors/" + authorEntityId);
                        if (mainAuthor != null)
                        {
                            // As a second level of security, the books referential immediately deletes confidential data in case the authors referential has a security problem
                            mainAuthor.Contacts = null;

                            // Then, the author is used in the expand but also updated in cache                            
                            book.Editing.mainAuthor.FullEntity = mainAuthor;
                            await Database.GetCollection<AuthorCache>("authors-bestsofar-cache").ReplaceOneAsync<AuthorCache>(item => item.EntityId == mainAuthor.EntityId, new AuthorCache(mainAuthor), new UpdateOptions { IsUpsert = true });
                        }
                    }
                    catch
                    {
                        // Naive retrieval of the entityId from the href link
                        int indexLastSlash = book.Editing.mainAuthor.Href.LastIndexOf("/");
                        string authorEntityId = book.Editing.mainAuthor.Href.Substring(indexLastSlash + 1);

                        // If, for whatever reason, fresh author data cannot be retrieved, we fail over on the cache
                        var res = Database.GetCollection<AuthorCache>("authors-bestsofar-cache").Find<AuthorCache>(item => item.EntityId == authorEntityId);
                        AuthorCache authorCache = res.FirstOrDefault();
                        if (authorCache != null)
                        {
                            book.Editing.mainAuthor.FullEntity = authorCache.ConvertToAuthor();
                        }
                        else
                        {
                            // The choice of implementation in case of failing to expand author is to raise an error
                            return new NotFoundObjectResult("Impossible to expand author information");
                        }
                    }
                }
                return new JsonResult(book);
            }
        }
        else
        {
            // Naive implementation at first, to be evolved to a single query only when actual performance is at stake
            var states = Database.GetCollection<ObjectState<Book>>("books-states").Find(item => item.EntityId == entityId);
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
        [FromBody] Book book, 
        [FromQuery] DateTimeOffset? providedValueDate = null)
    {
        // If no entity identifier is provided, a business rule states that a GUID should be attributed
        if (string.IsNullOrEmpty(book.EntityId))
            book.EntityId = Guid.NewGuid().ToString("N");

        // Creating an entity is in fact nothing else than patching an empty state
        JsonPatchDocument equivPatches = JSONPatchHelper.CreatePatch(
            new Book() { EntityId = book.EntityId }, 
            book,
            new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() }
        );

        // Registering to author changes, but if it does not work, it is not such a problem owing to eventual consistency
        try
        {
            HttpClient client = GetAuthenticatedClient("Authors");
            if (book.Editing != null && book.Editing.mainAuthor != null)
            {
                int pos = book.Editing.mainAuthor.Href.LastIndexOf("/");
                string authorEntityId = book.Editing.mainAuthor.Href.Substring(pos + 1);
                await client.PutAsync("/Authors/Subscribe?callbackURL=http://books:8080/AuthorsCache&$filter=EntityId eq '" + authorEntityId + "'", null);
            }
        }
        catch
        {
            _logger.LogError("Could not subscribe to webhook for author changes when creating book {BookId}", book.EntityId);
        }

        // After creating the equivalent patch, we thus pass this over to the PATCH operation
        return await Patch(book.EntityId, equivPatches, providedValueDate);
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
        var collectionActions = Database.GetCollection<BsonDocument>("books-changes");
        await collectionActions.InsertOneAsync(patchAction.ToBsonDocument());

        // Retrieving the potential existing best so far state of the business entity
        var result = Database.GetCollection<Book>("books-bestsofar").Find(item => item.EntityId == entityId);
        var book = result.FirstOrDefault();

        // If it does not exist already, we create it and query it again
        if (book == null)
        {
            book = new Book() { EntityId = entityId };
            var bestknown = Database.GetCollection<BsonDocument>("books-bestsofar");
            await bestknown.InsertOneAsync(book.ToBsonDocument());
            result = Database.GetCollection<Book>("books-bestsofar").Find(item => item.EntityId == entityId);
            book = result.First();
        }

        // Applying the patch of change to the state, in order to determine the new state, while keeping a clone
        // that will help us define the change business rules below
        Book before = (Book)book.Clone();
        patch.ApplyTo(book);

        // It is only after the saving of the patch that we rebuild the author link
        if (book?.Editing?.mainAuthor is not null)
        {
            int pos = book.Editing.mainAuthor.Href.LastIndexOf("/");
            string authorEntityId = book.Editing.mainAuthor.Href.Substring(pos + 1);

            // Since we consider speed is more important than absolute consistency for the author data duplicated in the link, we start with the cache
            var authorCache = Database.GetCollection<AuthorCache>("authors-bestsofar-cache").Find<AuthorCache>(item => item.EntityId == authorEntityId).FirstOrDefault();
            if (authorCache is null)
            {
                // But if we do not get the information from cache, we try to get it from the service and, of course, we refresh the cache if it works
                try
                {
                    HttpClient client = GetAuthenticatedClient("Authors");
                    var author = await client.GetFromJsonAsync<Author>("/Authors/" + authorEntityId);
                    if (author != null)
                    {
                        authorCache = new AuthorCache(author);
                        await Database.GetCollection<AuthorCache>("authors-bestsofar-cache").ReplaceOneAsync<AuthorCache>(item => item.EntityId == authorEntityId, authorCache, new UpdateOptions { IsUpsert = true });
                    }
                }
                catch {}
            }

            if (authorCache is not null)
            {
                book.Editing.mainAuthor.Rel = "dc:creator";
                book.Editing.mainAuthor.Title = authorCache.FirstName + " " + authorCache.LastName.ToUpper();
                book.Editing.mainAuthor.UserEmailAddress = authorCache.UserEmailAddress;
            }
        }

        // This is where we apply business rules on the book.
        // TODO: find a more generic way to inject the potentially necessary services.
        // TODO: generate webhook callbacks on the important changes (modification of status, etc.)
        BooksBehaviours behaviours = new BooksBehaviours(book, before, GetAuthenticatedClient("MiddleOffice"), GetAuthenticatedClient("MiddleOffice"), GetAuthenticatedClient("Users"), _configuration);
        bool correctExecution = await behaviours.Execute();
        if (!correctExecution)
        {
            // TODO: work on a more sophisticated error management, and possibly suppress patch just saved
            // for immediate consistency, or delay its recording after the business behaviours operations
            return BadRequest();
        }

        // This new state is added to the list of ongoing states of the entity
        var collectionEtats = Database.GetCollection<BsonDocument>("books-states");
        ObjectState<Book> nouvelEtat = new ObjectState<Book>()
        {
            EntityId = entityId,
            ValueDate = valueDate,
            State = book
        };
        await collectionEtats.InsertOneAsync(nouvelEtat.ToBsonDocument());

        // It is also used as a replacement of the best so far
        var collectionBestKnown = Database.GetCollection<Book>("books-bestsofar");
        await collectionBestKnown.FindOneAndReplaceAsync<Book>(
            p => p.EntityId == entityId,
            book
        );

        // Finally, the object is returned in order to show potential changes due to business rules
        return new ObjectResult(book);
    }

    [Authorize(Policy = "editor")]
    [HttpDelete]
    [Route("{entityId}")]
    public async Task<IActionResult> Delete(
        string entityId,
        [FromQuery] DateTimeOffset? providedValueDate = null,
        [FromQuery] bool fullDeleteIncludingHistory = false)
    {
        if (fullDeleteIncludingHistory)
        {
            // If this flag is activated (and it could be linked to a special authorization), the object is indeed deleted
            await Database.GetCollection<ChangeUnit>("books-changes").DeleteManyAsync(Builders<ChangeUnit>.Filter.Eq(item => item.EntityId, entityId));
            await Database.GetCollection<ObjectState<Book>>("books-states").DeleteManyAsync(Builders<ObjectState<Book>>.Filter.Eq(item => item.EntityId, entityId));
            await Database.GetCollection<Book>("books-bestsofar").DeleteManyAsync(Builders<Book>.Filter.Eq(item => item.EntityId, entityId));
            return new OkResult();
        }
        else
        {
            // In most case, we retrieve the object in order to apply it a modification, setting the status to archived
            var result = Database.GetCollection<Book>("books-bestsofar").Find(item => item.EntityId == entityId);
            if (result.CountDocuments() == 0)
                return new NotFoundResult();
            Book book = result.First();
            
            // A second version of the entity is created in order to apply the modification to it
            Book modified = result.First();
            if (modified.Editing is null) modified.Editing = new EditingPetal();
            if (modified.Editing.Status is null) modified.Editing.Status = new Status();
            modified.Editing.Status.Value = "archived";

            // A JSONPatch is then generated to carry the status modification
            JsonPatchDocument equivPatches = JSONPatchHelper.CreatePatch(
                book, 
                modified,
                new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() }
            );

            // After creating the equivalent patch, we thus pass this over to the PATCH operation
            DateTimeOffset valueDate = providedValueDate.GetValueOrDefault(DateTimeOffset.UtcNow);
            return await Patch(book.EntityId, equivPatches, valueDate);
        }
    }

    [Authorize(Policy = "director")]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] bool confirmFullDeleteOfAllBooksIncludingHistory = false)
    {
        if (confirmFullDeleteOfAllBooksIncludingHistory)
        {
            // If this flag is activated (and it could be linked to a special authorization), the object is indeed deleted
            await Database.GetCollection<ChangeUnit>("books-changes").DeleteManyAsync(item => true);
            await Database.GetCollection<ObjectState<Book>>("books-states").DeleteManyAsync(item => true);
            await Database.GetCollection<Book>("books-bestsofar").DeleteManyAsync(item => true);
        }
        return new OkResult();
    }

    private HttpClient GetAuthenticatedClient(string name)
    {
        HttpClient client = _clientFactory.CreateClient(name);
        var accessToken = Request.Headers["Authorization"];
        string jwt = accessToken.ToString().Replace("Bearer ", "");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }
}
