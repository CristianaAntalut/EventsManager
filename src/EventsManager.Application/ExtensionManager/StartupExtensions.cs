using EventsManager.Application.Config;

namespace EventsManager.Application.ExtensionManager;

public  static class StartupExtensions
{
    public static void AddAmazonSecretsManager(this IConfigurationBuilder configurationBuilder,
                    string region,
                    string secretName)
    {
        var configurationSource =
                new AmazonSecretsManagerConfigurationSource(region, secretName);

        configurationBuilder.Add(configurationSource);
    }
}
