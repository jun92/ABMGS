using Microsoft.Extensions.Logging;
using SyncnetPlatform.Interfaces.Actors;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SyncnetPlatform.Actors;


public interface IPlayRoomCustomMetadata
{
    public byte[] BuildPlayRoomCustomMetaData();

}
public interface IPlayRoomCustomEventHandler
{
    public Task OnPlayRoomInitializingAsync(PlayRoomState _currentPlayRoomState, byte[]? roomMetaData);
    public Task OnPlayRoomDestroyingAsync();
    public Task OnHandleCustomPacket(byte[] customPacket);
    

}

public class PlayRoomState
{
    [Id(0)]
    public string DisplayName { get; set; } = String.Empty;
    [Id(1)]
    public string PasswordForEntrace { get; set; } = String.Empty;
    [Id(2)]
    public object? PlayRoomMetaData { get; set; } = null;
}

public interface IPlayGameLogic
{
    public Task OnTimer(float delta);
}

public class PlayRoomActor : Grain, IPlayRoomActor
{
    private readonly ILogger<PlayRoomActor> _logger;


    private List<PlayRoomMember> _players = new List<PlayRoomMember>();

    private string _displayName = String.Empty;
    private string _passwordForEntrance = String.Empty;
    private int _maxPlayerCapacity = 4;
    private bool _isPrivate = false;
    private Guid _ownerPlayerId = Guid.Empty;
    private IDisposable? _playRoomTimer;
    private PlayRoomState _playRoomState = new();

    //Customizations
    private readonly IPlayRoomCustomEventHandler? _playRoomCustomEventHandler;
    private readonly IPlayGameLogic? _playGameLogic;
    public PlayRoomActor(
        ILogger<PlayRoomActor> logger,
        IPlayRoomCustomEventHandler? playRoomCustomEventHandler = null,
        IPlayGameLogic? playGameLogic = null)
    {
        _logger = logger;
        _playRoomCustomEventHandler = playRoomCustomEventHandler;
        _playGameLogic = playGameLogic;
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
    /// <summary>
    /// Create new playroom and join the room owner automatically
    /// </summary>
    /// <param name="displayName"></param>
    /// <param name="isPrivate"></param>
    /// <param name="maxCapacity"></param>
    /// <param name="roomPassword"></param>
    /// <param name="roomOwnerPlayerId"></param>
    /// <returns></returns>
    public async Task SetRoomInformation(
        string displayName, 
        bool isPrivate, 
        int maxCapacity, 
        string roomPassword, 
        byte[]? roomMetaData,
        PlayRoomMember owner)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));

        _displayName = displayName;
        _passwordForEntrance = roomPassword;
        _maxPlayerCapacity = maxCapacity;
        _isPrivate = isPrivate;
        _ownerPlayerId = owner.PlayerId;
        _players.Add(owner);

        if( _playRoomCustomEventHandler is not null)
        {
            await _playRoomCustomEventHandler.OnPlayRoomInitializingAsync(_playRoomState, roomMetaData);
        }
    }

    public Task<bool> IsValidRoomToJoin() => Task.FromResult(_ownerPlayerId !=  Guid.Empty);
        

    public async Task<PacketErrorCodes> JoinPlayer(PlayRoomMember joiner)
    {
        if (_ownerPlayerId == Guid.Empty)
        {
            return PacketErrorCodes.RoomNotFound;
        }

        if(_players.Find(f => f.PlayerId == joiner.PlayerId) != null)
        {
            return PacketErrorCodes.AlreadyInRoom;
        }
        if(_players.Count == _maxPlayerCapacity)
        {
            return PacketErrorCodes.RoomFull;
        }

        foreach (var player in _players)
        {
            IPlayerActor p = GrainFactory.GetGrain<IPlayerActor>(player.PlayerId);
            await p.OnUpdateForPlayRoomMembers(joiner, PlayRoomMemberUpdateReason.Join);
        }

        _players.Add(joiner);

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
