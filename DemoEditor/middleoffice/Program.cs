using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json.Serialization;
using MiddleOffice.Models;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using DemoEditor.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication().AddJwtBearer(options => {
    options.RequireHttpsMetadata = false;
    options.MetadataAddress = "http://iam:8080/realms/demoeditor/.well-known/openid-configuration";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = false, // do not forget to refresh authentication when changing this
        ValidateIssuer = false,
    };
});
builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
builder.Services.AddSingleton<IAuthorizationHandler, EditorAuthorizationHandler>();
builder.Services.AddAuthorization(o => {
    o.AddPolicy("author", policy => policy.RequireClaim("realm_roles", "author"));
    o.AddPolicy("editor", policy => policy.Requirements.Add(new EditorRequirement()));
    o.AddPolicy("director", policy => policy.RequireClaim("realm_roles", "director"));
});

builder.Services.AddHttpClient("Actions").AddPolicyHandler(GetRetryPolicy());

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Generally, the first command that will be called in Middle Office is the creation of a template of request,
// where a functional administrator can define what can be proposed, with which inputs and which possible decisions.
// These include a description but also the actions that will be run when a decision is taken on a request based on this template.
app.MapPut("/templates/{idTemplate}", async (string idTemplate, [FromBody] Template content) => {
    content.EntityId = idTemplate;
    IMongoDatabase Database = new MongoClient("mongodb://db:27017").GetDatabase("middleoffice");
    await Database.GetCollection<Template>("templates").DeleteManyAsync(Builders<Template>.Filter.Eq(t => t.EntityId, content.EntityId));
    await Database.GetCollection<Template>("templates").InsertOneAsync(content);
    return Results.NoContent();
})
.RequireAuthorization("director");

// Then, anybody can look at existing templates to decide which one is best to create requests.
app.MapGet("/templates", () =>
{
    IMongoDatabase Database = new MongoClient("mongodb://db:27017").GetDatabase("middleoffice");
    var query = Database.GetCollection<Template>("templates").Find(t => t.Status == "active");
    var result = query.ToList();
    return Results.Json(result);
});

// One however needs to have an intermediate authorization level to create such a request based on a template.
// When created, a request cannot be modified, and its possible decisions are necessarily a copy from the template.
// By the way, a copy of the template could also be created inside the link to it in order to keep the current version,
// but we consider it is enough that only functional administrators can modify the template and that "deletion" actually
// does not erase the data but puts the template definition in an archived state, making it only non visible for users.
app.MapPost("/templates/{idTemplate}/request", async (string idTemplate, [FromBody] List<InputValue> InputsValues) => {

    // First, we need the base template, as it will be used for most of the content of the request
    IMongoDatabase Database = new MongoClient("mongodb://db:27017").GetDatabase("middleoffice");
    var result = await Database.GetCollection<Template>("templates").FindAsync(t => t.EntityId == idTemplate && t.Status == "active");
    var template = result.FirstOrDefault();
    if (template == null) return Results.NotFound("Template indicated could not be found or is not active anymore");

    // Then we create the request itself, linking it to the template
    Request request = new Request();
    request.EntityId = Guid.NewGuid().ToString("N");
    request.Template = new TemplateLink() {
        Rel = "base-template",
        Href = "http://middleoffice:83/templates/" + idTemplate,
        Title = template.Title.FirstOrDefault()?.Value ?? "N/A"
    };
    request.InputsValues = InputsValues;

    // If required inputs are not send, then we need to stop there
    List<string> missingFieldsIds = new List<string>();
    foreach (Field field in template.Fields.Where(f => f.Required))
        if (!InputsValues.Any(iv => iv.FieldId == field.Id))
            missingFieldsIds.Add(field.Id);
    if (missingFieldsIds.Count() > 0)
        return Results.BadRequest("These required fields have no corresponding inputs: " + string.Join(", ", missingFieldsIds));

    // Once saved, its GUID can be sent back to the user creating the request, who will deal with where to send it
    await Database.GetCollection<Request>("requests").InsertOneAsync(request);
    return TypedResults.Created($"http://middleoffice:83/requests/{request.EntityId}", request);
})
.RequireAuthorization("editor");

// When the request is created, a GUID is generated and the full URL is sent back in the Location header,
// as edicted in the HTTP standard, together with a 201 result. This is then used by the person target of the request
// to read its content and decide on the decision that will be sent afterwards. This is typically sent into a web form.
app.MapGet("/request/{guidRequest}", async (string guidRequest) => {

    // Before all, we need to get the request back from persistence
    IMongoDatabase Database = new MongoClient("mongodb://db:27017").GetDatabase("middleoffice");
    var resultRequest = await Database.GetCollection<Request>("requests").FindAsync(r => r.EntityId == guidRequest);
    var request = resultRequest.FirstOrDefault();
    if (request == null) return Results.NotFound("Request could not be found");
    if (!string.IsNullOrEmpty(request.IdDecision)) return Results.BadRequest("This request is closed");

    // When someone calls this API, they need to see the content of the template as well
    string idTemplate = request.Template.Href.Substring(request.Template.Href.LastIndexOf("/") + 1);
    var resultTemplate = await Database.GetCollection<Template>("templates").FindAsync(t => t.EntityId == idTemplate);
    var template = resultTemplate.FirstOrDefault();
    request.Template.FullEntity = template;

    // In this simple approach, there is no content negotiation, and we send an HTML form for response
    string form = $"""<h2>{template.Title.FirstOrDefault().Value ?? "A request has been addressed to you"}</h2>""";
    foreach (Field field in template.Fields.Where(f => f.Visible))
    {
        string Value = request.InputsValues.FirstOrDefault(iv => iv.FieldId == field.Id)?.Value;
        form += $"""<p>{field.Name.FirstOrDefault().Value ?? "N/A"}: {Value}</p>""";
    }
    foreach (Decision possibleDecision in template.PossibleDecisions)
    {
        form += $"<form action='http://middleoffice:83/request/{guidRequest}/decision/{possibleDecision.Id}' method='POST'>";
        form += $"""  <input type='submit' value='{possibleDecision.Title.FirstOrDefault().Value ?? ("Decision code:" + possibleDecision.Id)}'/>""";
        form += "</form>";
    }
    return Results.Content(form, "text/html");
});

// The only modifications that will come afterwards will be when someone adds a decision on the request.
// This can be done by anyone, since we consider the knowing of the GUID of the request is enough of a security here.
// Setting this decision on the request automatically runs the actions associated to the decision in the template.
app.MapPost("/request/{guidRequest}/decision/{idDecision}", async (IHttpClientFactory httpClientFactory, string guidRequest, string idDecision) => {

    // When receiving a decision, we must first ensure the request exists and is not closed
    IMongoDatabase Database = new MongoClient("mongodb://db:27017").GetDatabase("middleoffice");
    var resultRequest = await Database.GetCollection<Request>("requests").FindAsync(r => r.EntityId == guidRequest);
    var request = resultRequest.FirstOrDefault();
    if (request == null) return Results.NotFound("Request could not be found");
    if (!string.IsNullOrEmpty(request.IdDecision)) return Results.BadRequest("This request is closed");
    
    // We also need to verify that the decision value sent is a valid one, based on the template
    string idTemplate = request.Template.Href.Substring(request.Template.Href.LastIndexOf("/") + 1);
    var resultTemplate = await Database.GetCollection<Template>("templates").FindAsync(t => t.EntityId == idTemplate);
    var template = resultTemplate.FirstOrDefault();
    request.Template.FullEntity = template;
    Decision decision = template.PossibleDecisions.FirstOrDefault(d => d.Id == idDecision);
    if (decision is null)
        return Results.BadRequest($"Decision id {idDecision} is not compatible with possible values");

    // Then we record the decision, which will close the request. Note that the content of the template is recorded with it
    // for historical purpose, even if there are securities in order to avoid a complete deletion of the template content.
    request.IdDecision = idDecision;
    await Database.GetCollection<Request>("requests").ReplaceOneAsync<Request>(r => r.EntityId == guidRequest, request);

    // Once this is done, we emit the associated actions
    foreach (var action in decision.Actions)
    {
        // Content of the action must be customized taking into account the field values
        string urlToCall = action.Href;
        foreach (InputValue iv in request.InputsValues)
            urlToCall = urlToCall.Replace("${" + iv.FieldId + "}", iv.Value);
        Console.WriteLine("Action called: " + urlToCall); // TODO: switch to proper logging

        // For now, calling the URL is done in a simple way, without complex delivery robustness
        try
        {
            HttpClient client = httpClientFactory.CreateClient("Actions");
            if (action.Verb.ToUpper() == "PUT")
                await client.PutAsync(urlToCall, null);
            else if (action.Verb.ToUpper() == "POST")
                await client.PostAsync(urlToCall, null);
            else
                Results.BadRequest($"Verb {action.Verb} is not supported");
        }
        catch
        {
            return Results.BadRequest("Decision has been recorded, but subsequent actions could not be taken. Please contact administrator and provide the following request id: " + guidRequest);
        }
    }
    return Results.NoContent();
});

// Once the request has received a decision, it is automatically archived, but this operation is manual on a template,
// since it is supposed to have a much longer lifecycle. Versions of templates could be managed, but it is easier
// to simply create a new one with the name including a version than developping a dedicated mechanism.
app.MapPut("/templates/{idTemplate}/status/archived", async (string idTemplate) => {
    IMongoDatabase Database = new MongoClient("mongodb://db:27017").GetDatabase("middleoffice");
    var result = await Database.GetCollection<Template>("templates").FindAsync(t => t.EntityId == idTemplate && t.Status == "active");
    var template = result.FirstOrDefault();
    if (template == null) return Results.NotFound("Template indicated could not be found or is not active anymore");
    template.Status = "archived";
    await Database.GetCollection<Template>("templates").ReplaceOneAsync<Template>(t => t.EntityId == idTemplate, template);
    return Results.NoContent();
})
.RequireAuthorization("director");

var objectSerializer = new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith("MiddleOffice.Models."));
BsonSerializer.RegisterSerializer(objectSerializer);

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(0.1));
}