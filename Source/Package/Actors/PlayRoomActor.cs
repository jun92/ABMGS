using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Network.Buffers;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly IPlayRoomSendBuffer _playRoomSendBuffer;

    //Customizations
    private readonly IPlayRoomCustomEventHandler? _playRoomCustomEventHandler = null;
    public PlayRoomActor(
        ILogger<PlayRoomActor> logger,
        IPlayRoomSendBuffer playRoomSendBuffer,
        IPlayRoomCustomEventHandler? playRoomCustomEventHandler = null
        )
    {
        _logger = logger;
        _playRoomSendBuffer = playRoomSendBuffer;
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
    
    public async Task<(PacketErrorCodes, byte[]?)> SetRoomInformation(string displayName,
        bool isPrivate,
        int maxCapacity,
        string roomPassword,
        PlayRoomMember owner)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));
        _playRoomState.DisplayName = displayName;
        _playRoomState.PasswordForEntrance = roomPassword;
        _maxPlayerCapacity = maxCapacity;
        _isPrivate = isPrivate;
        _ownerPlayerId = owner.PlayerId;

        _players.Add(owner);

        if( _playRoomCustomEventHandler is not null)
        {
            _playRoomState.PlayRoomCustomState = await _playRoomCustomEventHandler.OnPlayRoomInitializingAsync();
        }
        return (PacketErrorCodes.Success, SerializePlayRoomCustomState());
    }

    public ValueTask<bool> IsValidRoomToJoin() => ValueTask.FromResult<bool>(_ownerPlayerId != Guid.Empty);

    public async Task<(PacketErrorCodes, byte[])> JoinPlayer(PlayRoomMember joiner)
    {
        #region Early exit check
        if(_ownerPlayerId == Guid.Empty) return (PacketErrorCodes.RoomNotFound, []);
        if(_players.Exists(p => p.PlayerId == joiner.PlayerId)) return (PacketErrorCodes.AlreadyInRoom, []);
        if (_players.Count == _maxPlayerCapacity) return (PacketErrorCodes.RoomFull, []);
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

        _players.RemoveAll(r => r.PlayerId == leaver.PlayerId);
        if(_players.Count == 0 )
        {
            _ownerPlayerId = Guid.Empty;
            _logger.LogInformation("All players are left");
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

    public async Task<PacketErrorCodes> OnPlayerActionToPlayRoom(Guid playerId, string actionType, byte[] actionParameter)
    {
        if(_playRoomCustomEventHandler is not null)
        {
            // Custom processing 
            (Dictionary<Guid,byte[]> updatedPlayerExtendData, byte[]? updatedPlayRoomCustomState) = 
                await _playRoomCustomEventHandler.OnPlayerActionToPlayRoom(playerId, actionType, actionParameter, _playRoomSendBuffer);
            
            
            if( updatedPlayRoomCustomState is not null)
            {
                // Broadcasting to all players due to playroom state changed.
                foreach (PlayRoomMember member in _players)
                {
                    IPlayerActor p = GrainFactory.GetGrain<IPlayerActor>(member.PlayerId);
                    await p.OnUpdatePlayRoomCustomState(RoomId, updatedPlayRoomCustomState);
                }
            }
            
            foreach(KeyValuePair<Guid, byte[]> playerExtendData in updatedPlayerExtendData)
            {
                PlayRoomMember? updatedMember = _players.Find(p => p.PlayerId == playerExtendData.Key);
                if (updatedMember is not null)
                {
                    IPlayerActor p = GrainFactory.GetGrain<IPlayerActor>(updatedMember.PlayerId);
                    await p.OnUpdatePlayerExtendData(playerExtendData.Value);
                }
            }
        }
        return PacketErrorCodes.Success;
    }
}
