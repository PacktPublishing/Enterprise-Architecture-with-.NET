using System.Net;
using Polly;
using Polly.Extensions.Http;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using DemoEditor.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
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
builder.Services.AddSingleton<IAuthorizationHandler, EditorApiKeyAuthorizationHandler>();
builder.Services.AddAuthorization(o => {
    o.AddPolicy("author", policy => policy.RequireClaim("realm_roles", "author"));
    o.AddPolicy("editor", policy => policy.Requirements.Add(new EditorRequirement()));
    o.AddPolicy("editor-apikey", policy => {
        policy.AddAuthenticationSchemes(new[] { JwtBearerDefaults.AuthenticationScheme });
        policy.Requirements.Add(new EditorApiKeyRequirement()); 
    });
    o.AddPolicy("director", policy => policy.RequireClaim("realm_roles", "director"));
});

builder.Services.AddTransient<IApiKeyValidation, ApiKeyValidation>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers().AddNewtonsoftJson();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("Callbacks").AddPolicyHandler(GetRetryPolicy());

builder.Services.AddMemoryCache();

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

app.UseAuthentication();
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