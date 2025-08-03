namespace EventsManager.Application.Controllers;

using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using EventsManager.Application.Config;
using EventsManager.Application.ExtensionManager;
using EventsManager.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly IEventAttendeeRepository _repository;
    private readonly UserPoolConfig _userPoolConfig;

    public UsersController(IAmazonCognitoIdentityProvider cognitoClient, IEventAttendeeRepository repository, UserPoolConfig userPoolConfig)
    {
        _cognitoClient = cognitoClient;
        _repository = repository;
        _userPoolConfig = userPoolConfig;
    }

    /// <summary>
    /// GET: api/users: Lists all users in the user pool.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> ListUsers(int limit = 10, string paginationToken = null)
    {
        var request = new ListUsersRequest
        {
            UserPoolId = _userPoolConfig.UserPoolId,
            Limit = limit,
            PaginationToken = paginationToken
        };

        var response = await _cognitoClient.ListUsersAsync(request);
        return Ok(new
        {
            Users = response.Users,
            PaginationToken = response.PaginationToken
        });
    }

    /// <summary>
    ///  GET: api/users/{username}: Retrieves a specific user by username.
    /// </summary>
    [Authorize]
    [HttpGet("{username}")]
    public async Task<IActionResult> GetUser(string username)
    {
        try
        {
            if (!this.CanUserAccessWitProvidedData(username))
            {
                return Unauthorized("You cannot access this data.");
            }

            var request = new AdminGetUserRequest
            {
                UserPoolId = _userPoolConfig.UserPoolId,
                Username = username
            };

            var response = await _cognitoClient.AdminGetUserAsync(request);
            return Ok(response);
        }
        catch (UserNotFoundException)
        {
            return NotFound($"User '{username}' not found.");
        }
    }

    /// <summary>
    /// GET: api/users/{userId}/events: Retrieves events that a specific user attends to.
    /// </summary>
    [Authorize]
    [HttpGet("{username}/events")]
    public async Task<IActionResult> GetEventsUserAttendsTo(string username)
    {
        try
        {
            if (!this.CanUserAccessWitProvidedData(username))
            {
                return Unauthorized("You cannot access this data.");
            }

            var response = await _repository.QueryByUserIdAsync(username);
            return Ok(response);
        }
        catch (UserNotFoundException)
        {
            return NotFound($"User '{username}' not found.");
        }
    }
}