using Microsoft.AspNetCore.Mvc;

namespace EventsManager.Application.ExtensionManager;
public static class ControllerExtensions
{
    public static bool CanUserAccessWitProvidedData(this ControllerBase controller, string username)
    {
        var response = controller.User.Claims.Select(item => new KeyValuePair<string, string>(item.Type, item.Value)).ToList();
        if (IsUsernameInClaims(username, response) || IsUserAdmin(response))
        {
            return true;
        }

        return false;


        static bool IsUsernameInClaims(string username, List<KeyValuePair<string, string>> response) =>
            response.Any(item => item.Key == "username" && item.Value == username);


        static bool IsUserAdmin(List<KeyValuePair<string, string>> response) =>
            response.Any(item => item.Key == "cognito:groups" && item.Value == "Admin");
    }
}
