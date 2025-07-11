using Microsoft.AspNetCore.Mvc;
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
}
