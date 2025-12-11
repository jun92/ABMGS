using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Authentication.GooglePlay;
using SyncnetPlatform.Authentication.SyncnetAuthProvider;
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
    private readonly ISyncnetJwtAuthenticationService _syncnetJwtAuthenticationService;

    public AuthController(
        ILogger<AuthController> logger,
        ISyncnetAuthenticationService authenticationService,
        ISyncnetJwtAuthenticationService syncnetJwtAuthenticationService)
    {
        _logger = logger; 
        _authenticationService = authenticationService;
        _syncnetJwtAuthenticationService = syncnetJwtAuthenticationService;
    }

    [HttpGet("healthy")]
    public IActionResult Heathy()
    {
        return Ok("healthy");
    }

    [HttpGet("auth/token/test/{playerId}")]

    public async Task<IActionResult> TestIssueToken([FromRoute] string playerId)
    {
        return Ok(_syncnetJwtAuthenticationService.IssueNewToken(playerId));
    }

    // Issuing syncnet platform's own JWT token for further usage.
    [HttpPost("auth/token/{platformType}")]
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
