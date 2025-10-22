using Microsoft.AspNetCore.Mvc;

namespace ABMGS.ServerV2.Controllers;

public class GameSessionController : ControllerBase
{
    private readonly ILogger<GameSessionController> _logger;
    public GameSessionController(ILogger<GameSessionController> logger)
    {
        _logger = logger;
    }


    [Route("gamesession")]
    public async Task GameSession()
    {

    }

}
