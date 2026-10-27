using DialDen.RLS.API;
using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.DependencyInjection;
using DialDen.RLS.Infrastructure.DependencyInjection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddEnvironmentVariables(prefix: "RLS_API_")
    .AddCommandLine(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<PostingStatus>()));
builder.Services
    .AddRLSCore()
    .AddRLSInfrastructure(builder.Configuration);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod();
    // The UI may be served from any local port (e.g. the test host), so loopback origins are allowed in development only
    if (builder.Environment.IsDevelopment())
    {
        policy.SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback);
    }
}));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "DialDen RLS API v1"));
}
app.UseHttpsRedirection();
app.UseCors();
app.MapRLSEndpoints();
app.MapHealthChecks("/health");
app.Run();