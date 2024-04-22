using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using System.Net;
using Polly;
using Polly.Extensions.Http;
using Microsoft.AspNetCore.Authentication;
using books_controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddAuthentication().AddJwtBearer(options => {
//     options.RequireHttpsMetadata = false;
//     options.MetadataAddress = "http://iam:8080/realms/demoeditor/.well-known/openid-configuration";
//     options.TokenValidationParameters = new TokenValidationParameters
//     {
//         RoleClaimType = "groups",
//         ValidateAudience = false, // do not forget to refresh authentication when changing this
//         // https://stackoverflow.com/questions/60306175/bearer-error-invalid-token-error-description-the-issuer-is-invalid
//         ValidateIssuer = false,
//     };
// });
// builder.Services.AddAuthorization();

//builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>(); // TODO : should normally not be useful anymore once passed to realm-level roles
//builder.Services.AddSingleton<IAuthorizationHandler, EditorAuthorizationHandler>();

// builder.Services.AddAuthorization(o =>
// {
//     o.AddPolicy("author", policy => policy.RequireClaim("user_roles", "author"));
//     o.AddPolicy("editor", policy => policy.Requirements.Add(new EditorRequirement()));
//     o.AddPolicy("director", policy => policy.RequireClaim("user_roles", "director"));
// });

builder.Services.AddControllers().AddNewtonsoftJson();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("AuthorsWebhook", client => client.BaseAddress = new Uri("http://authors:8080/Authors/Subscribe"));
builder.Services.AddHttpClient("Authors", client => client.BaseAddress = new Uri("http://authors:8080/Authors")).AddPolicyHandler(GetRetryPolicy());

builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//if (app.Environment.IsDevelopment())
app.UseCors(options => options.WithOrigins("http://portal").AllowAnyMethod().AllowAnyHeader());

app.UseHttpsRedirection();

// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(0.1));
}