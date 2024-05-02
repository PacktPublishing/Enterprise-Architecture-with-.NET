using System.Net;
using Polly;
using Polly.Extensions.Http;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("Callbacks").AddPolicyHandler(GetRetryPolicy());

builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(options => options.WithOrigins("http://portal", "http://portal:88").AllowAnyMethod().AllowAnyHeader().SetPreflightMaxAge(TimeSpan.FromMinutes(10)));

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var objectSerializer = new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith("authors_controller.Models") || type.FullName.StartsWith("Newtonsoft."));
BsonSerializer.RegisterSerializer(objectSerializer);

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(0.1 * retryAttempt));
}