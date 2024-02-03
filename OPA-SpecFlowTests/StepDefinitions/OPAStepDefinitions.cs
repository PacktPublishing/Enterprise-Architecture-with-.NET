using static System.Reflection.Metadata.BlobBuilder;
using TechTalk.SpecFlow.CommonModels;
using Newtonsoft.Json;
using System.Text;
using System.Net;

namespace OPA_SpecFlowTests.StepDefinitions
{
    public class Author
    {
        public string Id { get; set; }
        public string Login { get; set; }
        public string Restriction { get; set; }
    }

    public class Book
    {
        public string Number { get; set; }
        public string Status { get; set; }
        public string AuthorId { get; set; }
    }

    public class User
    {
        public string Login { get; set; }
        public string Group { get; set; }
    }

    [Binding]
    public sealed class OPAStepDefinitions
    {
        private static HttpClient _client;
        private static List<Author> _authors;
        private static List<Book> _books;
        private static List<User> _users;
        private static string _orgChart;
        private static string _result;

        [BeforeFeature]
        public static void Initialize()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:8181/v1/");
        }

        [BeforeScenario]
        public static void InitializeScenario()
        {
            _authors = new List<Author>();
            _books = new List<Book>();
            _users = new List<User>();
        }

        [Given("book number (.*) with author id (.*) is in (.*) status")]
        public void AddBookWithStatus(string number, string authorId, string status)
        {
            _books.Add(new Book() { Number = number, AuthorId = authorId, Status = status });
        }

        [Given("user (.*) belongs to group (.*)")]
        public void AddUserWithGroup(string login, string group)
        {
            _users.Add(new User() { Login = login, Group = group });
        }

        [Given("user (.*) is associated to author (.*) who has level of restriction (.*)")]
        public void AddAuthor(string login, string authorId, string restrictionLevel)
        {
            _authors.Add(new Author() { Login = login, Id = authorId, Restriction = restrictionLevel });
        }

        [Given("organizational chart is (.*)")]
        public void SetOrganizationChart(string orgChart)
        {
            _orgChart = orgChart;
        }

        [When("user (.*) request (.*) access to the (.*) petal of the book number (.*)")]
        public void ExecuteRequest(string login, string access, string perimeter, string bookNumber)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("    \"roles\": {");
            sb.AppendLine("        \"book-direction\": { \"access\": []},");
            sb.AppendLine("        \"book-sales\": { \"parent\": \"book-direction\", \"access\": [{ \"operation\": \"all\", \"type\": \"books.sales\" }]},");
            sb.AppendLine("        \"book-edition\": { \"parent\": \"book-direction\", \"access\": [{ \"operation\": \"all\", \"type\": \"books.editing\" }]},");
            sb.AppendLine("        \"book-writer\": { \"parent\": \"book-edition\", \"access\": [{ \"operation\": \"read\", \"type\": \"books.editing\" }, { \"operation\": \"all\", \"type\": \"books.content\" }]}");
            sb.AppendLine("    },");
            sb.AppendLine("    \"books\": {");
            for (int i=0; i<_books.Count; i++)
            {
                Book b = _books[i];
                sb.Append("        \"" + b.Number + "\": { \"id\": \"" + b.Number + "\", \"title\": \"***NORMALLY NO IMPACT ON RULES***\", \"editing\": { \"author\": \"" + b.AuthorId + "\", \"status\": \"" + b.Status + "\" }}");
                if (i < _books.Count - 1) sb.AppendLine(","); else sb.AppendLine();
            }
            sb.AppendLine("    },");
            sb.AppendLine("    \"authors\": {");
            for (int i = 0; i < _authors.Count; i++)
            {
                Author a = _authors[i];
                sb.AppendLine("        \"" + a.Id + "\": { \"id\": \"" + a.Id + "\", \"firstName\": \"***NORMALLY NO IMPACT ON RULES***\", \"lastName\": \"***NORMALLY NO IMPACT ON RULES***\", \"user\": \"" + a.Login + "\", \"restriction\": \"" + a.Restriction + "\" }");
                if (i < _authors.Count - 1) sb.AppendLine(","); else sb.AppendLine();
            }
            sb.AppendLine("    },");
            sb.AppendLine("    \"org_chart\": " + _orgChart + ",");
            sb.AppendLine("    \"directory\": {");
            for (int i = 0; i < _users.Count; i++)
            {
                User u = _users[i];
                sb.AppendLine("        \"" + u.Login + "\": {\"groups\": [\"" + u.Group + "\"]}");
                if (i < _users.Count - 1) sb.AppendLine(","); else sb.AppendLine();
            }
            sb.AppendLine("    },");
            sb.AppendLine("    \"group_mappings\": {");
            sb.AppendLine("        \"board\": { \"roles\": [\"book-direction\"] },");
            sb.AppendLine("        \"commerce\": { \"roles\": [\"book-sales\"] },");
            sb.AppendLine("        \"editors\": { \"roles\": [\"book-edition\"] },");
            sb.AppendLine("        \"authors\": { \"roles\": [\"book-writer\"] }");
            sb.AppendLine("    },");
            sb.AppendLine("    \"user_mappings\": {");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var response = _client.PutAsync("data", new StringContent(sb.ToString(), Encoding.UTF8, "application/json")).Result;

            string input = "{ \"input\": { \"user\": \"" + login + "\","
                + " \"operation\": \"" + access + "\","
                + " \"resource\": \"" + perimeter + "\","
                + " \"book\": \"" + bookNumber + "\" } }";

            response = _client.PostAsync("data/app/abac", new StringContent(input, Encoding.UTF8, "application/json")).Result;
            if (response != null)
            {
                _result = response.Content.ReadAsStringAsync().Result;
            }
        }

        [Then("access should be (.*)")]
        public void ValidateExpectedResult(string expectedResult)
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(_result));
            reader.Read(); // Get first element
            reader.Read(); // Read result attribute
            reader.Read(); // Get element for result
            reader.Read(); // Read allow attribute
            bool? actual = reader.ReadAsBoolean(); // Get boolean value for allow attribute
            if (actual is null)
                throw new ApplicationException("Unable to find result");

            bool? expected = null;
            if (expectedResult == "refused") expected = false;
            if (expectedResult == "accepted") expected = true;
            if (expected is null)
                throw new ApplicationException("Unable to find expected value");

            Assert.Equal(expected, actual);
        }
    }
}
