using ABMGS.ServerV2.Grains;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace ABMGS.ServerV2.Controllers;

[ApiController]
[Route("/ws")]
public class GameSessionController : ControllerBase
{
    private readonly ILogger<GameSessionController> _logger;
    private readonly IClusterClient _clusterClient;
    public GameSessionController(ILogger<GameSessionController> logger, IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }

    [ProducesResponseType(StatusCodes.Status203NonAuthoritative)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Route("authenticate")]
    public IActionResult Authenticate()
    {
        return Ok();
    }

    


    [Route("/gamesession")]
    public async Task GameSession()
    {

        //Authenticate the user here

        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        IPlayerActor PlayerActor = _clusterClient.GetGrain<IPlayerActor>(Guid.NewGuid());



        
    }

}
