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
    using (var httpClientHandler = new HttpClientHandler())
    {
        httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        //GET internal-services.lexus.monster.com/seeker/api/seeker/ping
        using (var client = new HttpClient(httpClientHandler))
        {
            HttpResponseMessage response = await client.GetAsync("https://internal-services.lexus.monster.com/Seeker/api/v1/ping");
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
    }

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

record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}