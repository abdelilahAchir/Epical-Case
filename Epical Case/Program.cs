using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureAppConfiguration((context, config) =>
	{
		config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables();
	})
	.ConfigureServices(services =>
	{
		services.AddApplicationInsightsTelemetryWorkerService();
		services.ConfigureFunctionsApplicationInsights();
		var connectionString = Environment.GetEnvironmentVariable("BlobStorageConnectionString");
		services.AddSingleton(new BlobServiceClient(connectionString));
	})
	.Build();

host.Run();
