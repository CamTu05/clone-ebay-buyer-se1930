using Microsoft.AspNetCore.Mvc;

namespace Buyer.API.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<StatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<StatusResponse> GetStatus()
    {
        return Ok(new StatusResponse("eBay Clone - Buyer API", "Healthy"));
    }
}

public sealed record StatusResponse(string Application, string Status);
