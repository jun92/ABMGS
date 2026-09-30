using Google.FlatBuffers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace SyncnetPlatform.Tests;

public partial class ABMGS_TestMain : IAsyncLifetime
{
    private static void VerifyPacket(byte[] data, SystemPacket expected)
    {
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(data));
        Assert.Equal(expected, verifyPacket.SystemPacketType);
    }
    private static byte[] BuildPingPacket(int seq = 1)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<PingArgs>(new PingArgs(seq));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.Ping, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    private static byte[] BuildUpdatePlayerNamePacket(string newName)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqUpdatePlayerNameArgs>(new ReqUpdatePlayerNameArgs(newName));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqUpdatePlayerName, verifyPacket.SystemPacketType);
        return dataToSend;
    }
    private static byte[] BuildReqUserInfoPacket()
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqUserInfoArgs>(new ReqUserInfoArgs());
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqUserInfo, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    private static byte[] BuildReqDirectDeliveryDataPacket(Guid toPlayerId, string message, DirectDeliveryDataType dateType)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqDirectDeliveryDataArgs>(
            new ReqDirectDeliveryDataArgs(toPlayerId, message, dateType)
            );
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqDirectDeliveryData, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    private static byte[] BuildReqCreatePlayRoomPacket(
        string playRoomName, 
        bool isPrivate = false, 
        string password ="", 
        int maxCount = 1,
        byte[]? metaData = null)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqCreateRoomArgs>(
            new ReqCreateRoomArgs(playRoomName, isPrivate, password, maxCount)
            );
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqCreateRoom, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    private static byte[] BuildReqLeavelPlayRoomPacket(Guid roomId)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqLeaveRoomArgs>(new ReqLeaveRoomArgs(roomId));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqLeaveRoom, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    private static byte[] BuildReqJoinPlayRoomPacket(Guid roomId)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqJoinRoomArgs>(new ReqJoinRoomArgs(roomId, ""));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqJoinRoom, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    private static byte[] BuildReqPlayerListInRoomPacket(Guid roomId)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqPlayerListInRoomArgs>(new ReqPlayerListInRoomArgs(roomId));
        VerifyPacket(dataToSend, SystemPacket.ReqPlayerListInRoom);
        return dataToSend; 
    }

    private static byte[] BuildReqUserActionForUpdatePlayerCustomData(string actionType, byte[] actionParameters)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqUserActionForUpdatePlayerExtendDataArgs>(
            new ReqUserActionForUpdatePlayerExtendDataArgs(actionType, actionParameters)
            );
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqUserActionForUpdatePlayerExtendData, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    private static byte[] BuildReqPlayerActionToPlayRoom(Guid roomId, string actionType, byte[] actionParameters)
    {
        ReqPlayerActionToPlayRoomArgs reqPlayerActionToPlayRoom = new(roomId, actionType, actionParameters);
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqPlayerActionToPlayRoomArgs>(reqPlayerActionToPlayRoom);
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqPlayerActionToPlayRoom, verifyPacket.SystemPacketType);
        return dataToSend;
    }
    
}
