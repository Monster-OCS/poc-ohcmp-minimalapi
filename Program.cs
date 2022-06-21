using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using poc_ohcmp_minimalapi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//for the health checks see
//https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-6.0&viewFallbackFrom=aspnetcore-2.2
//liveness probe - at this point we are always ok
builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());

//IoC registrations
builder.Services.AddSingleton<WeatherComposer>(new WeatherComposer());


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

// API definitions
app.MapGet("/weatherforecast", ([FromServices] WeatherComposer weatherCmp) =>
{
    var forecast = weatherCmp.GetForecast();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();
