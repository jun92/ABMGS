
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyncnetPlatform.Actors;

public interface IPlayerCustomBehavior
{
    Task<bool> OnLoginAsync(PlayerState playerData, CancellationToken? cancellationToken = null);
    Task<bool> OnLogoutAsync(CancellationToken? cancellationToken = null);

    void UpdatePlayerExtendDataByUserAction(string actionType, byte[] actionParameters, PlayerState playerState);

    void OnCreatePlayRoom(PlayerState playerState, Guid playRoomId, byte[]? roomState);
    void OnJoinPlayRoom(PlayerState playerState, Guid playRoomId, bool isOwner, byte[]? roomState);
    
    byte[] Serialize(IReadOnlyDictionary<string, object?> playerState);
    
    IPlayerDataExtend GetPlayerCustomState();
}

public interface IPlayerDataExtend
{
    void Initialize(IReadOnlyDictionary<string, object?> state);
    byte[] Serialize(IReadOnlyDictionary<string, object?> playerState);
    Dictionary<string, object?> ToDictionary(byte[] data);
    Dictionary<string, object?> ToDictionary();
}


