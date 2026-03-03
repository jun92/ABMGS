using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SyncnetPlatform.Actors;


interface IPlayRoomActor : IGrainWithGuidKey
{

}

public class PlayRoomActor : Grain, IPlayRoomActor
{
    private readonly ILogger<PlayRoomActor> _logger;
    private List<Guid> players = new List<Guid>();

    private string _displayName = String.Empty;
    private string _passwordForEntrance = String.Empty;
    public PlayRoomActor(ILogger<PlayRoomActor> logger)
    {
        _logger = logger;
    }

    public Guid RoomId {
        private set { }
        get { return GrainContext.GrainId.GetGuidKey();} 
    }

    public async Task SetRoomInformation(string displayName, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));

    }

    public async Task OnPlayerJoin(Guid playerId)
    {

    }

    public async Task OnPlayerLeave(Guid playerId)
    {
        players.Remove(playerId);
        if(players.Count == 0)
        {
            base.DeactivateOnIdle();
        }
    }

    public async Task OnReqDestoryRoom(Guid roomId)
    {
        if( roomId.Equals(RoomId))
        {
            Init();
            base.DeactivateOnIdle();
        }
    }


    protected void Init()
    {
        players.Clear();
    }
}
