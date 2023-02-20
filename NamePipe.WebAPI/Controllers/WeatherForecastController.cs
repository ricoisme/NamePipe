using Microsoft.AspNetCore.Mvc;
using NamePipe.Helper;
using System.Text.Json;

namespace NamePipe.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IPipeClient _pipeClient;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IPipeClient pipeClient)
        {
            _logger = logger;
            _pipeClient = pipeClient;          
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            await _pipeClient.SendMessageAsync("hi namepipe from webAPI");

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}