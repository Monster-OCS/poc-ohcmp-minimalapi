using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//for the health checks see
//https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-6.0&viewFallbackFrom=aspnetcore-2.2
//liveness probe - at this point we are always ok
builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//http redirection should not be enabled for API
//https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-6.0&tabs=visual-studio
//app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/minimalapi/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateTime.Now.AddDays(index),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray(); 
    return forecast;
})
.WithName("GetWeatherForecast");

//test endpoint calling onprem
app.MapGet("/minimalapi/ping", async ([FromServices]IHttpClientFactory httpClientFactory) =>
{
    //this is a quick sample, not prod code template, need revision
    //GET internal-services.lexus.monster.com/seeker/api/seeker/ping
    var client = GetClient(httpClientFactory);
    HttpResponseMessage response = await client.GetAsync("https://internal-services.lexus.monster.com/Seeker/api/v1/ping");
    var result = await response.Content.ReadAsStringAsync();
    return result;
})
.WithName("GetPing");

//registration of liveness (self check)
app.MapHealthChecks("/health/liveness", new HealthCheckOptions
{
    Predicate = r => r.Name.Contains("self")
}).RequireHost("*:9090");

//registration of readiness
app.MapHealthChecks("/health/readiness", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("services")
}).RequireHost("*:9090");

app.Run();

//there may be some better way with named clients or something else and we should check around using statement ...
//https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-6.0
HttpClient GetClient(IHttpClientFactory httpClientFactory)
{
    if (app.Environment.IsDevelopment())
    {
        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return new HttpClient(httpClientHandler);
    }

    return httpClientFactory.CreateClient();
}

record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}