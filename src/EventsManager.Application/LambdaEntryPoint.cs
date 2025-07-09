using EventsManager.Application.ExtensionManager;

namespace EventsManager.Application;

public class LambdaEntryPoint : Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        var region = Environment.GetEnvironmentVariable("AWS_REGION");
        builder
            .ConfigureAppConfiguration(((_, configurationBuilder) =>
            {
                configurationBuilder.AddAmazonSecretsManager(region, "test/MyApiCredentials");
            }))
            .UseStartup<Startup>();
    }

    protected override void Init(IHostBuilder builder)
    {
    }
}