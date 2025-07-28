using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace ABMGS.Server.Front.Controllers;

[ApiController]
[Route("/ws")]

public class WebsocketController : ControllerBase
{
    private readonly ILogger<WebsocketController> _logger;

    public WebsocketController(ILogger<WebsocketController> logger)
    {
        _logger = logger;
    }

    public ActionResult Get()
    {
        return Ok();
    }
    [Route("/ws/connect")]
    public async Task ConnectByUserId([FromQuery] string userId)
    {
        if(HttpContext.WebSockets.IsWebSocketRequest)
        {
            WebSocket socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

}
