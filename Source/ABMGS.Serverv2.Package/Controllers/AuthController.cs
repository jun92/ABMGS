using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Authentication.GooglePlay;
using System;
using System.Collections.Generic;
using System.Text;

namespace SyncnetPlatform.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly ISyncnetAuthenticationService _authenticationService;


    public AuthController(
        ILogger<AuthController> logger,
        ISyncnetAuthenticationService authenticationService)
    {
        _logger = logger; 
        _authenticationService = authenticationService;
    }

    [HttpGet("healthy")]
    public IActionResult Heathy()
    {
        return Ok("healthy");
    }


    // Issuing syncnet platform's own JWT token for further usage.
    [HttpGet("auth/token/{platformType}")]
    public async Task<IActionResult> IssueToken([FromRoute] string platformType)
    {

        if (Enum.TryParse(platformType, out SupportedPlatformType supportedPlatformType))
        {
            switch (supportedPlatformType)
            {
                case SupportedPlatformType.googleplay:
                    await _authenticationService.GetPlayerIdByGooglePlayAuth("testservercode");
                    break;
                case SupportedPlatformType.apple:
                    break;
                case SupportedPlatformType.steam:
                    break;
            }
            return Ok();
        }
        else
        {
            return BadRequest($"unsupported platform type - {platformType} ");
        }

    }

}
