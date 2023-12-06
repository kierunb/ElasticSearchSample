using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace ElasticSearchSample.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DiagnosticsController : ControllerBase
{
    // note - following api is not specific to OpenTelemetry

    private readonly ILogger<DiagnosticsController> _logger;

    private readonly Meter _greeterMeter;
    private readonly Counter<int> _countGreetings;

    private readonly ActivitySource _greeterActivitySource;

    public DiagnosticsController(ILogger<DiagnosticsController> logger)
    {
        _logger = logger;

        _greeterMeter = new("ElasticSearchSample.Example", "1.0.0");
        _countGreetings = _greeterMeter.CreateCounter<int>("greetings.count", description: "Counts the number of greetings");
        
        _greeterActivitySource = new("ElasticSearchSample.Example");
    }


    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Create a new Activity scoped to the method
        using var activity = _greeterActivitySource.StartActivity("GreeterActivity");

        // Log a message
        _logger.LogInformation("Sending greeting");

        // Increment the custom counter
        _countGreetings.Add(1);

        // Add a tag to the Activity
        activity?.SetTag("greeting", "Hello World!");

        return Ok("Hello, World");
    }

}
