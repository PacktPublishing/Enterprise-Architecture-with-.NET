using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

#region Debug scenario for receiving a message
// Book book = new Book {
//     EntityId = "00024",
//     Title = "Performance in .NET",
//     Editing = new EditingPetal {
//         NumberOfChapters = 12,
//         mainAuthor = new AuthorLink {
//             Href = "http://authors:82/Authors/jpgou",
//             Title = "JP Gouigoux",
//             UserEmailAddress = "jp.gouigoux@free.fr"
//         }
//     }
// };

// byte[] pdf = PDFContract.Generate(book);

// string authorURL = book.Editing.mainAuthor.Href;
// int posLastSlash = authorURL.LastIndexOf('/');
// string authorId = authorURL.Substring(posLastSlash + 1);

// Dictionary<string, string> metadata = new();
// metadata.Add("de:authorId", authorId);
// metadata.Add("de:bookId", book.EntityId);
// metadata.Add("de:bookTitle", book.Title);
// metadata.Add("de:contractId", DateTime.Now.Year.ToString() + "-" + book.EntityId);
// metadata.Add("de:contractType", "NewEdition");
// await EDMClient.SendDocument(
//     "Contract-" + book.EntityId,
//     "AuthorContract",
//     metadata,
//     "Contract-" + book.EntityId + ".pdf",
//     pdf);

// Console.ReadLine();
#endregion

#region Debug scenario for emitting a return message

// Book book = new Book {
//     EntityId = "00024",
//     Title = "(contracting) Performance in .NET",
//     Editing = new EditingPetal {
//         NumberOfChapters = 12,
//         mainAuthor = new AuthorLink {
//             Href = "http://authors:82/Authors/jpgou",
//             Title = "JP Gouigoux (waiting for full identity)",
//             UserEmailAddress = "jp.gouigoux@free.fr"
//         }
//     }
// };

// var factory = new ConnectionFactory() { HostName = "localhost", Port = 5673, UserName = "rapido", Password = "k5rXH6wmBhE2bukfXFsz" };
// Console.WriteLine("Sending message");
// using (var connection = factory.CreateConnection())
// using (var channel = connection.CreateModel())
// {
//     channel.QueueDeclare(queue: "ContractGenerated", durable: false, exclusive: false, autoDelete: false, arguments: null);
//     channel.BasicPublish(exchange: "", routingKey: "ContractGenerated", basicProperties: null, body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(book)));
// }
// Console.WriteLine("Message sent");

// Console.ReadLine();

#endregion

IConfiguration _configuration = new ConfigurationBuilder()
  .AddEnvironmentVariables()
  .Build();

string RabbitUser = _configuration["RABBITMQ_DEFAULT_USER"] ?? throw new ArgumentException("Missing RABBITMQ_DEFAULT_USER environment variable");
string RabbitPassword = _configuration["RABBITMQ_DEFAULT_PASS"] ?? throw new ArgumentException("Missing RABBITMQ_DEFAULT_PASS environment variable");

var factory = new ConnectionFactory() { HostName = "mom", UserName = RabbitUser, Password = RabbitPassword };
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare(queue: "AuthorChosenForBookEvent", durable: false, exclusive: false, autoDelete: false, arguments: null);
    var consumer = new EventingBasicConsumer(channel);
    consumer.Received += async (model, ea) =>
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Book? book = JsonSerializer.Deserialize<Book>(message);
            if (book is not null)
            {
                byte[] pdf = PDFContract.Generate(book);

                string authorURL = book.Editing.mainAuthor.Href;
                int posLastSlash = authorURL.LastIndexOf('/');
                string authorId = authorURL.Substring(posLastSlash + 1);

                Dictionary<string, string> metadata = new();
                metadata.Add("de:authorId", authorId);
                metadata.Add("de:bookId", book.EntityId);
                metadata.Add("de:bookTitle", book.Title);
                metadata.Add("de:contractId", DateTime.Now.Year.ToString() + "-" + book.EntityId);
                metadata.Add("de:contractType", "NewEdition");
                await EDMClient.SendDocument(
                    "Contract-" + book.EntityId,
                    "AuthorContract",
                    metadata,
                    "Contract-" + book.EntityId + ".pdf",
                    pdf);

                using (var connection2 = factory.CreateConnection())
                using (var channel2 = connection2.CreateModel())
                {
                    channel2.QueueDeclare(queue: "ContractGenerated", durable: false, exclusive: false, autoDelete: false, arguments: null);
                    channel2.BasicPublish(exchange: "", routingKey: "ContractGenerated", basicProperties: null, body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(book)));
                }
            }
        }
        finally
        {
            // In order to simplify the initial version, we always acknowledge treatment of the message
            // Any error is handled by the fact that there has been no message stating the contract is ready
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }
    };
    channel.BasicConsume(queue: "AuthorChosenForBookEvent", autoAck: false, consumer: consumer);

    Console.WriteLine("Press RETURN to stop the program");
    Console.ReadLine();
}