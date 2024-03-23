using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using System.Net;
using Polly;
using Polly.Extensions.Http;
using Microsoft.AspNetCore.Authentication;
using books_controller;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    options.Authority = "http://localhost:8080/realms/demoeditor/";
    options.Audience = "account";
    options.RequireHttpsMetadata = false;
}).AddCookie();

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>(); // TODO : should normally not be useful anymore once passed to realm-level roles
builder.Services.AddSingleton<IAuthorizationHandler, EditorAuthorizationHandler>();

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("author", policy => policy.RequireClaim("user_roles", "author"));
    o.AddPolicy("editor", policy => policy.Requirements.Add(new EditorRequirement()));
    o.AddPolicy("director", policy => policy.RequireClaim("user_roles", "director"));
});

builder.Services.AddControllers().AddNewtonsoftJson();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("AuthorsWebhook", client => client.BaseAddress = new Uri("http://demoeditor.org/authors/Subscribe"));
builder.Services.AddHttpClient("Authors", client => client.BaseAddress = new Uri("http://demoeditor.org/authors")).AddPolicyHandler(GetRetryPolicy());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(0.1));
}