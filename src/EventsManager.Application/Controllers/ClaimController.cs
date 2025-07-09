using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventsManager.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClaimController : Controller
{
    [Authorize(Roles = "Admin,User")]
    [HttpGet]
    public async Task<IActionResult> ListEvents()
    {
        var response = User.Claims.Select(item => new KeyValuePair<string, string>(item.Type, item.Value)).ToList();
        return Ok(response);
    }
}

