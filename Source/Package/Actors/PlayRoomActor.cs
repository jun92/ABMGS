using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Actors;

public class PlayRoomActor : Grain, IPlayRoomActor
{
    private readonly ILogger<PlayRoomActor> _logger;

    private readonly List<PlayRoomMember> _players = new List<PlayRoomMember>();

    private int _maxPlayerCapacity = 4;
    private bool _isPrivate = false;
    private Guid _ownerPlayerId = Guid.Empty;
    private IDisposable? _playRoomTimer;
    private readonly PlayRoomState _playRoomState = new();

    //Customizations
    private readonly IPlayRoomCustomEventHandler? _playRoomCustomEventHandler = null;
    public PlayRoomActor(
        ILogger<PlayRoomActor> logger,
        IPlayRoomCustomEventHandler? playRoomCustomEventHandler = null
        )
    {
        _logger = logger;
        if( playRoomCustomEventHandler is not null)
        {
            _playRoomCustomEventHandler = playRoomCustomEventHandler;
        }
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (_playRoomCustomEventHandler is not null)
        {
            // Activate Timer for custom handler
            _playRoomTimer = this.RegisterGrainTimer(
                callback: _playRoomCustomEventHandler.OnTimer,
                state: 0.0f,
                dueTime: TimeSpan.Zero,
                period: TimeSpan.FromSeconds(1)
                );
        }
        else
        {
            _playRoomTimer = null;
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if(_playRoomCustomEventHandler is not null)
        {
            await _playRoomCustomEventHandler.OnPlayRoomDestroyingAsync();
        }
        _playRoomTimer?.Dispose();
    }
    public Guid RoomId => GrainContext.GrainId.GetGuidKey();
    
    public async Task<IPlayRoomCustomState?> SetRoomInformation(
        string displayName, 
        bool isPrivate, 
        int maxCapacity, 
        string roomPassword, 
        PlayRoomMember owner)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));
        _playRoomState.DisplayName = displayName;
        _playRoomState.PasswordForEntrace = roomPassword;
        _maxPlayerCapacity = maxCapacity;
        _isPrivate = isPrivate;
        _ownerPlayerId = owner.PlayerId;

        _players.Add(owner);

        if( _playRoomCustomEventHandler is not null)
        {
            _playRoomState.PlayRoomCustomState = await _playRoomCustomEventHandler.OnPlayRoomInitializingAsync();
        }

        return _playRoomState.PlayRoomCustomState;
    }

    public Task<bool> IsValidRoomToJoin() => Task.FromResult(_ownerPlayerId !=  Guid.Empty);


    public async Task<(PacketErrorCodes, byte[])> JoinPlayer(PlayRoomMember joiner)
    {
        #region Early exit check
        Dictionary<Predicate<PlayRoomActor>, PacketErrorCodes> earlyExitCheck = new Dictionary<Predicate<PlayRoomActor>, PacketErrorCodes>
        {
            { r => r._ownerPlayerId == Guid.Empty, PacketErrorCodes.RoomNotFound },
            { r => r._players.Exists(p => p.PlayerId == joiner.PlayerId), PacketErrorCodes.AlreadyInRoom },
            { r => r._players.Count == _maxPlayerCapacity, PacketErrorCodes.RoomFull }
        };
        if( earlyExitCheck.FirstOrDefault(f => f.Key(this)) is {  Key: not null } match)
        {
            return (match.Value, Array.Empty<byte>());
        }
        #endregion

        foreach (PlayRoomMember player in _players)
        {
            IPlayerActor p = GrainFactory.GetGrain<IPlayerActor>(player.PlayerId);
            await p.OnUpdateForPlayRoomMembers(joiner, PlayRoomMemberUpdateReason.Join);
        }
        if( _playRoomCustomEventHandler is not null)
        {
            await _playRoomCustomEventHandler.AddPlayerToPlayRoom(joiner.PlayerId, joiner.PlayerExtendData ?? []);
        }

        _players.Add(joiner);

        return (PacketErrorCodes.Success, SerializePlayRoomCustomState());
    }

    protected byte[] SerializePlayRoomCustomState() =>
        _playRoomState.PlayRoomCustomState is not null ? _playRoomState.PlayRoomCustomState.Serialize() : [];

    public Task<List<PlayRoomMember>> GetPlayersInPlayRoom()
    {
        return Task.FromResult(_players);
    }

    public async Task<PacketErrorCodes> LeavePlayer(PlayRoomMember leaver)
    {
        if(_ownerPlayerId == Guid.Empty)
        {
            return PacketErrorCodes.RoomNotFound;
        }

        _players.Remove(leaver);
        if(_players.Count == 0 )
        {
            _ownerPlayerId = Guid.Empty;
            base.DeactivateOnIdle();
            return PacketErrorCodes.Success;
        }

        foreach(var player in _players)
        {
            IPlayerActor p = GrainFactory.GetGrain<IPlayerActor>(player.PlayerId);
            await p.OnUpdateForPlayRoomMembers(leaver, PlayRoomMemberUpdateReason.Leave);
        }

        if(_ownerPlayerId == leaver.PlayerId)
        {
            _ownerPlayerId = _players.First().PlayerId;
        }

        return PacketErrorCodes.Success;
    }

    protected void Init()
    {
        _players.Clear();
    }

    public async Task OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter)
    {
        if(_playRoomCustomEventHandler is not null)
        {
            (Dictionary<Guid,byte[]> updatedPlayerExtendData, byte[]? updatedPlayRoomCustomState) = await _playRoomCustomEventHandler.OnPlayerActionToPlayRoom(playerId, actionType, actionParameter);
            if( updatedPlayRoomCustomState is not null)
            {
                //Boardcast the updated state to all players in the room.
            }
            foreach(KeyValuePair<Guid, byte[]> playerExtendData in updatedPlayerExtendData)
            {
                PlayRoomMember? who = _players.Find(f => f.PlayerId == playerExtendData.Key);
                if (who != null)
                {
                    who.PlayerExtendData = playerExtendData.Value;
                    IPlayerActor p = GrainFactory.GetGrain<IPlayerActor>(who.PlayerId);
                    await p.OnUpdatePlayerExtendData(playerExtendData.Value);
                }
                // Broadcast players extend data to all players in the room as well.
                // Update the data structure in the PlayRoomActor first and sync them later.
            }
        }
    }
}
