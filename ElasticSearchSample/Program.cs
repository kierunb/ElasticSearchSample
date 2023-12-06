
using Elastic.Channels;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
//using Serilog.Sinks.Elasticsearch;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown-environment";
string assemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
string indexPattern = $"{assemblyName.ToLower().Replace(".", "-")}-{environment?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}";

// Serilog.Sinks.Elasticsearch
//Log.Logger = new LoggerConfiguration()
//	.WriteTo.Console()
//	.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
//	{
//		AutoRegisterTemplate = true,
//		AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
//		IndexFormat = indexPattern
//	})
//	.Enrich.WithMachineName()
//	.Enrich.WithEnvironmentName()
//	.CreateLogger();

// Elastic.Serilog.Sinks
Log.Logger = new LoggerConfiguration()
	.WriteTo.Console()
	.WriteTo.Elasticsearch(new[] { new Uri("http://localhost:9200") }, opts =>
	{
		opts.DataStream = new DataStreamName("logs", "ElasticSearchSample", "webapi");
		opts.BootstrapMethod = BootstrapMethod.Failure;
	})
	.Enrich.WithMachineName()
	.Enrich.WithEnvironmentName()
	.CreateLogger();

try
{
	Log.Information("Starting web application");
	var builder = WebApplication.CreateBuilder(args);

	builder.Host.UseSerilog();

	builder.Services.AddControllers();
	
	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen();

	// otel configuration

	var otel = builder.Services.AddOpenTelemetry();

	// Configure OpenTelemetry Resources with the application name
	otel.ConfigureResource(resource => resource
		.AddService(serviceName: builder.Environment.ApplicationName));

	// Custom metrics for the application
	string greeterMeterName = "ElasticSearchSample.Example";

	// Add Metrics for ASP.NET Core and our custom metrics and export to Prometheus
	otel.WithMetrics(metrics => metrics
		// Metrics provider from OpenTelemetry
		.AddAspNetCoreInstrumentation()
		.AddRuntimeInstrumentation()
		.AddMeter(greeterMeterName)
		// Metrics provides by ASP.NET Core in .NET 8
		.AddMeter("Microsoft.AspNetCore.Hosting")
		.AddMeter("Microsoft.AspNetCore.Server.Kestrel")
		.AddPrometheusExporter());


	var tracingOtlpEndpoint = new Uri("http://localhost:4317");
	string greeterActivitySourceName = "ElasticSearchSample.Example";

	otel.WithTracing(tracing =>
	{
		tracing.AddAspNetCoreInstrumentation();
		tracing.AddHttpClientInstrumentation();
		tracing.AddSource(greeterActivitySourceName);
		if (tracingOtlpEndpoint != null)
		{
			tracing.AddOtlpExporter(otlpOptions =>
			{
				otlpOptions.Endpoint = tracingOtlpEndpoint;
			});
		}
		else
		{
			tracing.AddConsoleExporter();
		}
	});


	var app = builder.Build();

	// Configure the HTTP request pipeline.
	if (app.Environment.IsDevelopment())
	{
		app.UseSwagger();
		app.UseSwaggerUI();
	}

	//app.UseHttpsRedirection();

	app.UseAuthorization();

	// Configure the Prometheus scraping endpoint
	app.MapPrometheusScrapingEndpoint();

	app.MapControllers();

	app.Run();

}
catch (Exception ex)
{
	Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
	Log.CloseAndFlush();
}


