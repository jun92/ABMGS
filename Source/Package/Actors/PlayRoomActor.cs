using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SyncnetPlatform.Actors;

public interface IPlayGameLogic
{
    Task OnTimer(float delta);
}

public class PlayRoomActor : Grain, IPlayRoomActor
{
    private readonly ILogger<PlayRoomActor> _logger;

    private List<PlayRoomMember> _players = new List<PlayRoomMember>();

    private int _maxPlayerCapacity = 4;
    private bool _isPrivate = false;
    private Guid _ownerPlayerId = Guid.Empty;
    private IDisposable? _playRoomTimer;
    private PlayRoomState _playRoomState = new();

    //Customizations
    private readonly IPlayRoomCustomEventHandler<IPlayRoomMetaData>? _playRoomCustomEventHandler;
    private readonly IPlayGameLogic? _playGameLogic;
    private readonly IPlayRoomMetaData? _playRoomMetaData;
    public PlayRoomActor(
        ILogger<PlayRoomActor> logger,
        IPlayRoomCustomEventHandler<IPlayRoomMetaData>? playRoomCustomEventHandler = null,
        IPlayGameLogic? playGameLogic = null,
        IPlayRoomMetaData? playRoomMetaData = null)
    {
        _logger = logger;
        _playRoomCustomEventHandler = playRoomCustomEventHandler;
        _playGameLogic = playGameLogic;
        _playRoomMetaData = playRoomMetaData;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (_playGameLogic is not null)
        {
            _playRoomTimer = this.RegisterGrainTimer(
                callback: _playGameLogic.OnTimer,
                state: 0.0f,
                dueTime: TimeSpan.Zero,
                period: TimeSpan.FromSeconds(1)
                );
        }
        else
        {
            _playRoomTimer = null;
        }

        if(_playRoomCustomEventHandler is not null)
        {
            _playRoomState.PlayRoomMetaData = _playRoomCustomEventHandler.InitializePlayRoomMetaData();
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
    
    public async Task<IPlayRoomMetaData?> SetRoomInformation(
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
            _playRoomState.PlayRoomMetaData = await _playRoomCustomEventHandler.OnPlayRoomInitializingAsync();
        }

        return _playRoomState.PlayRoomMetaData;
    }

    public Task<bool> IsValidRoomToJoin() => Task.FromResult(_ownerPlayerId !=  Guid.Empty);


    public async Task<PacketErrorCodes> JoinPlayer(PlayRoomMember joiner)
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
            return match.Value;
        }
        #endregion

        foreach (var player in _players)
        {
            IPlayerActor p = GrainFactory.GetGrain<IPlayerActor>(player.PlayerId);
            await p.OnUpdateForPlayRoomMembers(joiner, PlayRoomMemberUpdateReason.Join);
        }

        _players.Add(joiner);
        _playRoomCustomEventHandler.AddPlayerToPlayRoom(joiner.PlayerId, )

        return PacketErrorCodes.Success;
    }

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

    public async Task HandleCustomPacket(byte[] customPacket)
    {
        if(_playRoomCustomEventHandler is not null)
        {
            await _playRoomCustomEventHandler.OnHandleCustomPacket(customPacket);
        }
    }
}
