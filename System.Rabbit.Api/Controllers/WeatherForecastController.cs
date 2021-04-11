using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Rabbit.Api.Controllers
{
    [ApiController]
    [Route("weather")]
    public class WeatherForecastController : Controller
    {
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("forecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpPost]
        [Route("forecast")]
        public IActionResult ExecuteRabbit([FromBody] WeatherForecast weatherForecast)
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
            var rabbitConnection = config.GetConnectionString("RabbitMQConnection");

            var factoryConnection = new ConnectionFactory();
            factoryConnection.HostName = rabbitConnection;

            using (var connection = factoryConnection.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "TesteRabbit", durable: false, exclusive: false, autoDelete: false, arguments: null);

                    var json = JsonConvert.SerializeObject(weatherForecast);
                    var body = Encoding.UTF8.GetBytes(json);

                    channel.BasicPublish(exchange: "", routingKey: "TesteRabbit", basicProperties: null, body: body);

                    _logger.LogInformation("Mensagem enviada!");
                }
            }

            return Ok();
        }

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
    }
}
