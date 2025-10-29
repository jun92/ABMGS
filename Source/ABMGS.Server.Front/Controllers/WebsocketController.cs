using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using ABMGS.Server.Front.Services;

namespace ABMGS.Server.Front.Controllers;

[ApiController]
[Route("/ws")]
public class WebsocketController : ControllerBase
{
    private readonly ILogger<WebsocketController> _logger;
    private readonly SessionService _sessionService;
    private readonly PlayerLoopService _loopService;

    public WebsocketController(
        ILogger<WebsocketController> logger,
        SessionService sessionService,
        PlayerLoopService playerLoopService
        )
    {
        _logger = logger;
        _sessionService = sessionService;
        _loopService = playerLoopService;
    }

    public ActionResult Get()
    {
        return Ok();
    }


    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Route("/platform/{platform}/provider/{provider}")]
    public async Task ConnectByUserId([FromQuery] Guid userId)
    {

        // Authenticate the user here 


        // check websocket connection. -> start game loop
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            WebSocket socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _loopService.StartSessionLoop(socket, userId);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

}
