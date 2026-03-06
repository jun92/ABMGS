using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SyncnetPlatform.Authentication.GooglePlay;
using SyncnetPlatform.Interfaces.Network.Sessions;
using System.Net.WebSockets;
using System.Security.Claims;

namespace SyncnetPlatform.Controllers;

[ApiController]
[Route("/ws")]
public class GameSessionController : ControllerBase
{
    private readonly ILogger<GameSessionController> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IGameSessionService _gameSessionService;
    private readonly ISyncnetAuthenticationService _authenticationService;
    
    public GameSessionController(
        ILogger<GameSessionController> logger, 
        IClusterClient clusterClient,
        IGameSessionService gameSessionService,
        ISyncnetAuthenticationService authenticationService)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _gameSessionService = gameSessionService;
        _authenticationService = authenticationService;
    }

    //[ProducesResponseType(StatusCodes.Status203NonAuthoritative)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[HttpGet("alive")]
    //public IActionResult Authenticate()
    //{
    //    return Ok();
    //}

    // Issuing syncnet platform's own JWT token for further usage.
    [HttpGet("auth/token/{platformType}")]
    public IActionResult IssueToken([FromRoute] string platformType)
    {
        
        if (Enum.TryParse(platformType, out SupportedPlatformType supportedPlatformType))
        {
            switch (supportedPlatformType)
            {
                case SupportedPlatformType.googleplay:

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

    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(policy: "GameSocketPolicy")]
    [HttpGet("gamesession")]
    public async Task GameSession()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userIdClaim);
        if(!Guid.TryParse(userIdClaim, out Guid playerId))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await _gameSessionService.StartGameSession(playerId, webSocket);
        
    }
}


public enum SupportedPlatformType
{
    googleplay,
    apple,
    steam,
    guest
} 