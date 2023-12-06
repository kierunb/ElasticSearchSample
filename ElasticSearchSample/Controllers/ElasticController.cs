using ElasticSearchSample.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nest;
using System;

namespace ElasticSearchSample.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ElasticSearchController : ControllerBase
{
    ElasticClient _elasticClient;
    private readonly ILogger<ElasticSearchController> _logger;

    public ElasticSearchController(ILogger<ElasticSearchController> logger)
    {
        //var connectionPool = new SniffingConnectionPool(uris);
        var settings = new ConnectionSettings(new Uri("http://example.com:9200"))
            .DefaultIndex("people");

        _elasticClient = new ElasticClient(settings);
        
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var person = new Person
        {
            Id = Faker.RandomNumber.Next(1, 100000),
            FirstName = Faker.Name.First(),
            LastName = Faker.Name.Last()
        };

        //await _elasticClient.IndexDocumentAsync(person);

        _logger.LogInformation("Indexing of person {Id} invoked", person.Id);

        return Ok(person);
    }

    [HttpGet]
    public async Task<IActionResult> Get(string firstName)
    {
        // query to: /people/person/_search -> index 'people', type 'person'

        var searchResponse = await _elasticClient.SearchAsync<Person>(s => s
            .AllIndices() // search all indices '/_all/person/_search'
            .From(0)
            .Size(10)
            .Query(q => q
                 .Match(m => m
                    .Field(f => f.FirstName)
                    .Query(firstName)
                 )
            )
        );

        _logger.LogInformation("Found: {docCount} documents.", searchResponse.Documents.Count);

        return Ok(searchResponse.Documents);
    }
}
