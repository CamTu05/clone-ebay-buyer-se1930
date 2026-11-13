using Microsoft.AspNetCore.Mvc;

namespace Buyer.API.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<StatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<StatusResponse> GetStatus()
    {
        var configuredInstance = configuration["ApiInstance"];
        var instance = configuredInstance ?? "single";
        if (configuredInstance is not null) Response.Headers["X-Api-Instance"] = instance;
        return Ok(new StatusResponse("eBay Clone - Buyer API", "Healthy", instance));
    }
}

public sealed record StatusResponse(string Application, string Status, string Instance);
