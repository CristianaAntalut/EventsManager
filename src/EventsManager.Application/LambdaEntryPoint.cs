using EventsManager.Application.ExtensionManager;
using Serilog;

namespace EventsManager.Application;

public class LambdaEntryPoint : Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        var region = Environment.GetEnvironmentVariable("AWS_REGION");
        builder
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
            })
            .ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddAmazonSecretsManager(region, "test/MyApiCredentials");
            })
            .UseStartup<Startup>();
    }

    protected override void Init(IHostBuilder builder)
    {
        builder
            .UseSerilog((context, services, configuration) =>
            {
                configuration
                            .Enrich.FromLogContext()
                            .WriteTo.Console();
            });
    }
}