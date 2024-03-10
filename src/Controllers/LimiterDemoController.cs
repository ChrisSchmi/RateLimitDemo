using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimitDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableRateLimiting("fixed-by-ip")]
    public class LimiterDemoController   : ControllerBase
    {
        private readonly ILogger<LimiterDemoController> _logger;

        public LimiterDemoController(ILogger<LimiterDemoController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IActionResult GetValue()
        {
            return Ok("Hello Friends!");
        }
    }
}
