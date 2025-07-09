using EventsManager.Application.ExtensionManager;
using Serilog;

namespace EventsManager.Application;

public class LocalEntryPoint
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args)
            .Build()
            .Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
             .ConfigureAppConfiguration((_, configurationBuilder) =>
             {
                 var region = Environment.GetEnvironmentVariable("AWS_REGION");
                 configurationBuilder.AddAmazonSecretsManager(region, "test/MyApiCredentials");
             })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}