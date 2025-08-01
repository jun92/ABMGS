using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using ABMGS.Server.Front.Services;

namespace ABMGS.Server.Front.Controllers;

[ApiController]
[Route("/ws")]
public class WebsocketController : ControllerBase
{
    private readonly ILogger<WebsocketController> _logger;
    private readonly SessionService _sessionService;

    public WebsocketController(ILogger<WebsocketController> logger, SessionService sessionService)
    {
        _logger = logger;
        _sessionService = sessionService;
    }

    public ActionResult Get()
    {
        return Ok();
    }
    [Route("/ws/connect")]
    public async Task ConnectByUserId([FromQuery] Guid userId)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            WebSocket socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            // 반복 시작
            // // 데이타를 읽고
            // // 조합을 하고
            // // 응답 보낼 데이타가 있으면 보내고
            // // 연결 끊겼는지 체크
            // 반복 끝
            _sessionService.AddWebSocket(userId, socket);



        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

}
