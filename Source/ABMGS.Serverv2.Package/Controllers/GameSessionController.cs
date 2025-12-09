using SyncnetPlatform.Interfaces.Network.Sessions;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Authentication;

namespace SyncnetPlatform.Controllers;

[ApiController]
[Route("/ws")]
public class GameSessionController : ControllerBase
{
    private readonly ILogger<GameSessionController> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IGameSessionService _gameSessionService;
    private readonly IAuthenticationService _authenticationService;
    
    public GameSessionController(
        ILogger<GameSessionController> logger, 
        IClusterClient clusterClient,
        IGameSessionService gameSessionService,
        IAuthenticationService authenticationService)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _gameSessionService = gameSessionService;
        _authenticationService = authenticationService;
    }

    [ProducesResponseType(StatusCodes.Status203NonAuthoritative)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpGet("alive")]
    public IActionResult Authenticate()
    {
        return Ok();
    }

    // Issuing syncnet platform's own JWT token for further usage.
    [HttpGet("issue/{platformType}")]
    public IActionResult IssueToken([FromRoute] string platformType)
    {
        
        if (Enum.TryParse<SupportedPlatformType>(platformType, out SupportedPlatformType supportedPlatformType))
        {
            switch (supportedPlatformType)
            {
                case SupportedPlatformType.GooglePlay:
                    break;
                case SupportedPlatformType.Apple:
                    break;
                case SupportedPlatformType.Steam:
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
    [HttpGet("gamesession")]
    public async Task GameSession()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        
        WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await _gameSessionService.StartGameSession(Guid.NewGuid(), webSocket);
        
    }

}


public enum SupportedPlatformType
{
    GooglePlay,
    Apple,
    Steam,
}