using SyncnetPlatform.Interfaces.Network.Sessions;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace SyncnetPlatform.Controllers;

[ApiController]
[Route("/ws")]
public class GameSessionController : ControllerBase
{
    private readonly ILogger<GameSessionController> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IGameSessionService _gameSessionService;
    
    public GameSessionController(
        ILogger<GameSessionController> logger, 
        IClusterClient clusterClient,
        IGameSessionService gameSessionService)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _gameSessionService = gameSessionService;
    }

    [ProducesResponseType(StatusCodes.Status203NonAuthoritative)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Route("/healthcheck")]
    public IActionResult Authenticate()
    {
        return Ok();
    }



    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Route("gamesession")]
    public async Task GameSession()
    {

        //Authenticate the user here

        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await _gameSessionService.StartGameSession(Guid.NewGuid(), webSocket, CancellationToken.None);
    }

}
