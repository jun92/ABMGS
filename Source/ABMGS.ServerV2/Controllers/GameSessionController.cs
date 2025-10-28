using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace ABMGS.ServerV2.Controllers;

[ApiController]
[Route("/ws")]
public class GameSessionController : ControllerBase
{
    private readonly ILogger<GameSessionController> _logger;
    public GameSessionController(ILogger<GameSessionController> logger)
    {
        _logger = logger;
    }

    


    [Route("/gamesession")]
    public async Task GameSession()
    {
        if(!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();



        
    }

}
