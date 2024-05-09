using books_controller.Models;
using System.Net.Mail;

namespace books_controller.Business;

public class BooksBehaviours
{
    private Book _book;

    public BooksBehaviours(Book book)
    {
        _book = book;
    }

    public void Execute()
    {
        InviteProspects();
    }

    public void InviteProspects()
    {
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
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("authors-service@demoeditor.org"),
                    Subject = "You have been selected as possible author for a new book",
                    // Add the accept / decline
                    IsBodyHtml = true
                };
                mailMessage.Body = $"<h2>Hello, {prospect.Title}</h2>";
                mailMessage.Body += $"<p>You have been selected as a potential author for DemoEditor's new book {_book.Title}</p>";
                mailMessage.To.Add(prospect.UserEmailAddress);
                smtpClient.Send(mailMessage);

                // In this naive implementation, we do not add any robustness measure: if the code above does not throw
                // any exception, we consider the mail will be received by the prospect author and skip to the next step
                prospect.ProspectionStatus = "waiting-for-prospect-response";
            }
        }
    }
}