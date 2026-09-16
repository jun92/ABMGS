```mermaid
sequenceDiagram
participant Player1
participant PlayerActor1
participant PlayRoomActor1
participant PlayerActor2

Note over PlayerActor1,PlayRoomActor1: Case: Creating new play room
PlayerActor1->>PlayRoomActor1: ReqCreatePlayRoom
PlayRoomActor1->>PlayerActor1: ResCreatePlayRoom

Note over PlayerActor1,PlayerActor2: Case: PlayerActor2 Joins to a play room and notifying to inner players
PlayerActor2->>PlayRoomActor1: ReqJoinPlayRoom
PlayRoomActor1->>PlayerActor2: ResJoinPlayRoom
PlayRoomActor1->>PlayerActor1: OnPlayerJoinRoom

Note over Player1,PlayerActor2: Case: Player1 invites PlayerActor2
Player1->>PlayerActor1: ReqDirectDeliveryData
PlayerActor1->>Player1: ResDirectDeliveryData
PlayerActor1->>PlayerActor2: OnDirectDeliveryData


%% Note over PlayerActor2,PlayRoomActor1: Case: Getting player's info already in
%% PlayerActor2->>PlayRoomActor1: ReqPlayersInfo
%% PlayRoomActor1->>PlayerActor2: ResPlayersInfo

%% Note over PlayerActor1,PlayerActor2: Case: Leaving PlayerActor2
%% PlayerActor2->>PlayRoomActor1: ReqLeavePlayRoom
%% PlayRoomActor1->>PlayerActor2: ResLeavePlayRoom
%% PlayRoomActor1->>PlayerActor1: ResLeavePlayRoom





