using System.ComponentModel.DataAnnotations;
using books_controller.Models;
using System.Net.Mail;

namespace books_controller.Business;

public class BooksBehaviours
{
    private Book _book;

    private HttpClient _clientMiddleOffice;

    private HttpClient _clientMiddleOffice2;

    public BooksBehaviours(Book book, HttpClient clientMiddleOffice, HttpClient clientMiddleOffice2)
    {
        _book = book;
        _clientMiddleOffice = clientMiddleOffice;
        _clientMiddleOffice2 = clientMiddleOffice2;
    }

    public async Task<bool> Execute()
    {
        return await InviteProspects();
    }

    public async Task<bool> InviteProspects()
    {
        // TODO: extract the mail sending in a notification service, and rearrange this class so it can be read by product owners
        var smtpClient = new SmtpClient("mail")
        {
            Port = 1025,
            //Credentials = new NetworkCredential("username", "password"),
            EnableSsl = false,
        };

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
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("authors-service@demoeditor.org"),
                    Subject = $"DemoEditor has selected you as a possible author for the new book called {_book.Title}",
                    Body = messageContent,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(prospect.UserEmailAddress);
                smtpClient.Send(mailMessage);

                // In this naive implementation, we do not add any robustness measure: if the code above does not throw
                // any exception, we consider the mail will be received by the prospect author and skip to the next step
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