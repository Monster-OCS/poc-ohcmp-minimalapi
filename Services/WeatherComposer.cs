using poc_ohcmp_minimalapi.Dtos;

namespace poc_ohcmp_minimalapi.Services
{
    public class WeatherComposer
    {
        private static readonly string[] summaries = new[] {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public WeatherForecast[] GetForecast()
        {
            return Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateTime.Now.AddDays(index),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

            
        }
    }
}
