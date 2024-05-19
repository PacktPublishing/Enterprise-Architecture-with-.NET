// using RabbitMQ.Client;

// public class EventsEmitter
// {
//     protected readonly IConfiguration _configuration;

//     protected readonly IHttpClientFactory _clientFactory;

//     public EventsEmitter(IConfiguration configuration, IHttpClientFactory httpClientFactory)
//     {
//         _configuration = configuration;
//         _clientFactory = httpClientFactory;
//     }

//     public Emit(Event evt)
//     {
//         if (evt is AuthorChosenForBookEvent)
//         {
//             string RabbitUser = _configuration.GetValue<string>("RABBITMQ_DEFAULT_USER");
//             string RabbitPassword = _configuration.GetValue<string>("RABBITMQ_DEFAULT_PASS");
//             var factory = new ConnectionFactory() { HostName = "mom", UserName = RabbitUser, Password = RabbitPassword };
//             using (var connection = factory.CreateConnection())
//             using (var channel = connection.CreateModel())
//             {
//                 channel.QueueDeclare(queue: "AuthorChosenForBookEvent", durable: false, exclusive: false, autoDelete: false, arguments: null);
//                 Book book = (evt as AuthorChosenForBookEvent).Book;
//                 var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(book));
//                 channel.BasicPublish(exchange: "", routingKey: "AuthorChosenForBookEvent", basicProperties: null, body: body);
//             }
//         }
//     }

//     private HttpClient GetAuthenticatedClient(string name)
//     {
//         HttpClient client = _clientFactory.CreateClient(name);
//         var accessToken = Request.Headers["Authorization"];
//         string jwt = accessToken.ToString().Replace("Bearer ", "");
//         client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
//         return client;
//     }
// }

// public interface Event()
// {}