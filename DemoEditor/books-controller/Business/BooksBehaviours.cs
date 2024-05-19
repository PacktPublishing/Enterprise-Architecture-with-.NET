using System.ComponentModel.DataAnnotations;
using books_controller.Models;
using System.Net.Mail;
using System.Net.Http.Headers;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace books_controller.Business;

public class BooksBehaviours
{
    private Book _book;

    private Book _previousState;

    private HttpClient _clientMiddleOffice;

    private HttpClient _clientMiddleOffice2; // TODO: check if this debug is useful and how it could be changed if so

    private HttpClient _clientNotification;

    private IConfiguration _configuration { get; set; }

    public BooksBehaviours(Book book, Book previousState, HttpClient clientMiddleOffice, HttpClient clientMiddleOffice2, HttpClient clientNotification, IConfiguration configuration)
    {
        _book = book;
        _previousState = previousState;
        _clientMiddleOffice = clientMiddleOffice;
        _clientMiddleOffice2 = clientMiddleOffice2;
        _clientNotification = clientNotification;
        _configuration = configuration;
    }

    public async Task<bool> Execute()
    {
        bool result = true;
        if (ProspectsHaveBeenAdded())
            result = result && await InviteProspects();
        if (AuthorDataNeedsRefresh())
            result = result && await RefreshAuthorData();
        if (AuthorHasBeenChosen())
            result = result && await InformListenersAuthorChosenForBook();
        return result;
    }

    private bool AuthorDataNeedsRefresh()
    {
        return _book?.Editing?.mainAuthor?.UserEmailAddress is not null && _book?.Editing?.mainAuthor?.UserEmailAddress == "Actual email address (like title) will be updated by the server, using the href value as reference";
    }

    private async Task<bool> RefreshAuthorData()
    {
        // TODO: to be implemented by extracting the code that already exists to do so in the BooksController
        return true;
    }

    private bool AuthorHasBeenChosen()
    {
        return (_previousState?.Editing?.mainAuthor is null && _book?.Editing?.mainAuthor is not null);
    }

    private async Task<bool> InformListenersAuthorChosenForBook()
    {
        string RabbitUser = _configuration.GetValue<string>("RABBITMQ_DEFAULT_USER");
        string RabbitPassword = _configuration.GetValue<string>("RABBITMQ_DEFAULT_PASS");

        var factory = new ConnectionFactory() { HostName = "mom", UserName = RabbitUser, Password = RabbitPassword };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "AuthorChosenForBookEvent", durable: false, exclusive: false, autoDelete: false, arguments: null);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_book));
            channel.BasicPublish(exchange: "", routingKey: "AuthorChosenForBookEvent", basicProperties: null, body: body);
        }
        return true;
    }

    private bool ProspectsHaveBeenAdded()
    {
        return (_previousState?.Editing?.ProspectAuthors is null || _previousState?.Editing?.ProspectAuthors.Count() == 0)
            && (_book?.Editing?.ProspectAuthors is not null && _book.Editing.ProspectAuthors.Count() > 0);
    }

    public async Task<bool> InviteProspects()
    {
        foreach (ProspectAuthorLink prospect in _book.Editing.ProspectAuthors)
        {
            if (prospect.ProspectionStatus == "selected-by-editors-as-prospect")
            {
                // Inviting a prospect means creating a request in the middle office for them to be able to respond
                // to sollicitation. Note that the template for such middle office request should be customized in advance.
                // Necessary files are provided in the Postman collection associated to this example.
                List<InputValue> inputsValues = new List<InputValue>();
                inputsValues.Add(new InputValue { FieldId = "BookId", Value = _book.EntityId });
                inputsValues.Add(new InputValue { FieldId = "BookTitle", Value = _book.Title });
                inputsValues.Add(new InputValue { FieldId = "AuthorId", Value = prospect.Href.Substring(prospect.Href.LastIndexOf('/') + 1) });
                var response = await _clientMiddleOffice.PostAsJsonAsync<List<InputValue>>("http://mo:8080/templates/AuthorNewBookProposal/request", inputsValues);
                string urlRequest = response.Headers.GetValues("Location").FirstOrDefault();

                // TODO: remove this instruction, made necessary that we do not operate yet under a true domain name with HTTPS
                // and all the bells and whistles. When everything will be behind a Traefik / Caddy Docker-enabled reverse-proxy,
                // we will not have to differentiate between the names of the services and the accesses from outside of Docker Compose.
                // But for now, we need to force the equivalence between external access to the service and internal, on port 8080.
                urlRequest = urlRequest.Replace("middleoffice:83", "mo:8080");

                // We could put the link inside the message, but it is even better to put first the content of the request form.
                string requestForm = await _clientMiddleOffice2.GetStringAsync(urlRequest);
                string messageContent = $"<h2>Hello, {prospect.Title}</h2>";
                messageContent += "<hr/>" + requestForm + "<hr/>";
                messageContent += $"<p>If this message does not display correctly, please click on <a href='{urlRequest}'>this link</a> for the online version</p>";

                // Once the request is ready in the middle office, we can send a message to the prospect author for them to respond.
                StringContent content = new StringContent(messageContent, System.Text.UnicodeEncoding.UTF8, "text/html");
                content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                _clientNotification.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                string from = "urn:org:demoeditor:contact:emailaddress:authors-service@demoeditor.org";
                string to = $"urn:org:demoeditor:contact:emailaddress:{prospect.UserEmailAddress}";
                string title = $"DemoEditor has selected you as a possible author for the new book called {_book.Title}";
                await _clientNotification.PostAsync($"http://users:8080/Notify?to={to}&title={title}&from={from}&contentType=html", content);

                // In this naive implementation, we do not add any robustness measure: if the code above does not throw
                // any exception, we consider the mail will be received by the prospect author and skip to the next step.
                // Note that persistence is not activated here since the calling method will be recording the entity in the end.
                prospect.ProspectionStatus = "waiting-for-prospect-response";
            }
        }

        return true;
    }
}

// TODO: externalize all mechanism associated to middle office consumption in a dedicated class, and ideally make this an injectable service
public class InputValue
{
    [Required(ErrorMessage = "Field id is mandatory")]
    public string FieldId { get; set; }

    public string Value { get; set; }
}