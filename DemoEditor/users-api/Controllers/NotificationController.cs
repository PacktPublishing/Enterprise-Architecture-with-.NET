using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net.Mail;
using System.Security.Claims;
using users_api.Models;

namespace users_api.Controllers;

[Authorize]
[ApiController]
public class NotificationController : ControllerBase
{
    private readonly ILogger<NotificationController> _logger;

    private readonly IMongoDatabase Database;

    private IHttpClientFactory _clientFactory;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public NotificationController(IConfiguration config, ILogger<NotificationController> logger, IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        _httpContextAccessor = httpContextAccessor;

        // It is better to fix the database in the constructor, in order to avoid accidental coupling
        // due to the fact that the databases are in the same engine. Keeping a separate client for each
        // base ensures we are not dependent on the fact that databases are - for pure simplicity of
        // implementation - in the same database engine (and all of them in MongoDB)
        string ConnectionString = config.GetValue<string>("BooksConnectionString") ?? "mongodb://db:27017";
        Database = new MongoClient(ConnectionString).GetDatabase("users");
    }

    [HttpGet]
    [Route("MyNewMessages")]
    public async Task<IActionResult> GetConnectedUserNotifs([FromQuery] bool includeAlreadyReadMessages = false)
    {
        // No need to provision: if messages have been adressed to this user account, it must have been connected once already
        var httpContext = _httpContextAccessor.HttpContext;
        string connectedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

        // Then we retrieve the new messages (or all of them, in some cases, but this should be discusses, as volumes can grow quickly)
        var builder = Builders<UserMessage>.Filter;
        FilterDefinition<UserMessage> queryFilter = builder.Eq(item => item.DestinationUserEntityId, connectedUserId);
        if (!includeAlreadyReadMessages)
            queryFilter &= builder.Eq(item => item.HasBeenRead, false);
        var query = await Database.GetCollection<UserMessage>("user-messages").FindAsync(queryFilter);
        var result = query.ToList();

        // In order to simplify the mechanism, we consider messages as read once they are extracted from the database
        var update = Builders<UserMessage>.Update.Set(item => item.HasBeenRead, true);
        await Database.GetCollection<UserMessage>("user-messages").UpdateManyAsync(queryFilter, update);
        return new JsonResult(result);
    }

    [HttpPost]
    [Route("Notify")]
    public async Task<IActionResult> Notify(
        [FromQuery] string? from,
        [FromQuery] string to,
        [FromQuery] string? title,
        [FromQuery] string? priority = "normal",
        [FromQuery] string? contentType = "text")
    {
        var contentHeader = Request.Headers["Content-Type"];
        bool isBodyHtml = contentHeader.ToString().ToLower() == "text/html";
        string message = string.Empty;
        using (var reader = new System.IO.StreamReader(Request.Body, System.Text.Encoding.UTF8))
        {  
            message = await reader.ReadToEndAsync();
        }

        // Instead of fighting with different clients and complex retrieval of login success, as well as reflecting on how
        // to send it only once, we simply provision the user everytime a call that needs it is realized, as it is in this operation
        if (!(await ProvisionCurrentUser()))
            return BadRequest("Impossible to provision user");
        Contacts? origin = await GetContactsFromURN(from);
        Contacts destination = await GetContactsFromURN(to);
        if (destination is null)
            return BadRequest("Notification failed because destination contact is unknown");

        List<string> channels = await GetChannelsToUse(origin, destination, priority);
        if (channels.Count() == 0)
            return BadRequest("No compatible channels could be found");

        if (channels.Contains("email"))
        {
            // TODO: get this from configuration instead of relying on the MailHog included in Docker Compose
            var smtpClient = new SmtpClient("mail")
            {
                Port = 1025,
                //Credentials = new NetworkCredential("username", "password"),
                EnableSsl = false,
            };
            var mailMessage = new MailMessage();
            if (origin is not null) mailMessage.From = new MailAddress(origin.Emails.First().EmailAddress);
            mailMessage.Subject = title;
            mailMessage.Body = message;
            mailMessage.IsBodyHtml = isBodyHtml; // TODO: Content request header indicates what format is the full message in the body
            mailMessage.To.Add(destination.Emails.First().EmailAddress);
            smtpClient.Send(mailMessage);
        }

        if (channels.Contains("portal"))
        {
            UserMessage userMsg = new UserMessage() {
                DestinationUserEntityId = destination.Portal.UserEntityId,
                Title = title,
                Message = message
            };
            await Database.GetCollection<BsonDocument>("user-messages").InsertOneAsync(userMsg.ToBsonDocument());
        }

        return Ok();
    }

    // urn can be in the following forms:
    // - urn:org:demoeditor:contact:emailaddress:no-reply@demoeditor.org
    // - urn:org:demoeditor:contact:author:jpgou
    // - urn:org:demoeditor:contact:actor:9b516e0614194adc992137b26570f3af (TODO: customers and suppliers are not implemented yet)
    // - urn:org:demoeditor:contact:user:166433fc973e4543b570b900537a4f83
    // - urn:org:demoeditor:contact:user:francesca@demoeditor.org
    private async Task<Contacts> GetContactsFromURN(string urn)
    {
        if (string.IsNullOrEmpty(urn)) return null;

        Contacts result = new Contacts();
        string identifier = urn.Substring(urn.LastIndexOf(":") + 1);

        if (urn.StartsWith("urn:org:demoeditor:contact:emailaddress:"))
        {
            result.Emails.Add(new Email() {
                IANAType = "work",
                EmailAddress = identifier
            });
        }

        if (urn.StartsWith("urn:org:demoeditor:contact:author:"))
        {
            HttpClient client = GetAuthenticatedClient("Authors");
            try
            {
                Author author = await client.GetFromJsonAsync<Author>("/Authors/" + identifier);
                result = author.Contacts;
                result.Emails.Add(new Email() {
                    IANAType = "work",
                    EmailAddress = author.UserEmailAddress
                });
            }
            catch { return null; }
        }

        if (urn.StartsWith("urn:org:demoeditor:contact:actor:"))
        {
            throw new NotImplementedException("Actors are not supported yet");
        }

        if (urn.StartsWith("urn:org:demoeditor:contact:user:"))
        {
            // Access to user is direct (and not through an API) because we can consider
            // notification service to be in the same business domain as users management
            var query = await Database.GetCollection<UserProvision>("users-provision").FindAsync(item => item.IDPIdentifier == identifier || item.EmailAddress == identifier);
            UserProvision user = query.FirstOrDefault();
            if (user is null) return null;
            result.Emails.Add(new Email() {
                IANAType = "work",
                EmailAddress = user.EmailAddress
            });
            result.Phones.Add(new Phone() {
                IANAType = "cell",
                Number = user.MobilePhone
            });
            result.Portal.UserEntityId = user.IDPIdentifier;
        }

        // TODO: take into account preferences of the users about their notification channel.
        // For now, we obey a very simple rule that snail mail is not supported, and that other
        // contacts are filtered in order to leave only the work email and the mobile phone.
        result.Addresses.Clear();
        result.Phones.RemoveAll(p => p.IANAType != "cell");
        result.Emails.RemoveAll(em => em.IANAType != "work");

        return result;
    }

    private async Task<List<string>> GetChannelsToUse(Contacts origin, Contacts destination, string priority)
    {
        // TODO: this is typically where we could use a BRMS like OPA if the rules for notification start to get complicated,
        // and they can because we already have several possible channels (snail mail, phone calls, SMS, list of messages
        // or toast popups in the portal GUI) and the channels (they can be multiple) depend on the contacts of the emitter
        // and of the receiver. In addition, we should take into account priority. Add to this the possible feature of taking
        // into account preferences from each diffent destination depending on workdays or weekends / time of the day / etc.,
        // and you get an idea of why we should use a dedicated engine. But for now, we are going to be very simple in the choice.
        List<string> channels = new();
        if (destination.Emails.Count() > 0 && origin is not null && origin.Emails.Count() > 0) channels.Add("email");
        if (priority == "high" && !string.IsNullOrEmpty(destination.Portal.UserEntityId)) channels.Add("portal");
        return channels;

        // PUT http://localhost:8181/v1/policies/app/abac policy.rego
        // PUT http://localhost:8181/v1/data data.json
        // POST http://localhost:8181/v1/data/app/abac input.json
        // '.result | .allow'
        
    }

    private HttpClient GetAuthenticatedClient(string name)
    {
        HttpClient client = _clientFactory.CreateClient(name);
        var accessToken = Request.Headers["Authorization"];
        string jwt = accessToken.ToString().Replace("Bearer ", "");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }

    private async Task<bool> ProvisionCurrentUser()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            UserProvision userProv = new UserProvision {
                IDPIdentifier = httpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value,
                EmailAddress = httpContext.User.FindFirst(ClaimTypes.Email).Value,
                FirstName = httpContext.User.FindFirst(ClaimTypes.GivenName).Value,
                LastName = httpContext.User.FindFirst(ClaimTypes.Surname).Value,
                MobilePhone = httpContext.User.FindFirst(ClaimTypes.MobilePhone)?.Value
            };
            await Database.GetCollection<BsonDocument>("users-provision").InsertOneAsync(userProv.ToBsonDocument());
            return true;
        }
        catch { return false; }
    }
}
