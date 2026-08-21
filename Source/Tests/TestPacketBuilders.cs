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
    protected void VerifyPacket(byte[] data, SystemPacket expected)
    {
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(data));
        Assert.Equal(expected, verifyPacket.SystemPacketType);
    }
    protected byte[] BuildPingPacket(int seq = 1)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<PingArgs>(new PingArgs(seq));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.Ping, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    protected byte[] BuildUpdatePlayerNamePacket(string newName)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqUpdatePlayerNameArgs>(new ReqUpdatePlayerNameArgs(newName));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqUpdatePlayerName, verifyPacket.SystemPacketType);
        return dataToSend;
    }
    protected byte[] BuildReqUserInfoPacket()
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqUserInfoArgs>(new ReqUserInfoArgs());
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqUserInfo, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    protected byte[] BuildReqDirectDeliveryDataPacket(Guid toPlayerId, string message, DirectDeliveryDataType dateType)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqDirectDeliveryDataArgs>(
            new ReqDirectDeliveryDataArgs(toPlayerId, message, dateType)
            );
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqDirectDeliveryData, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    protected byte[] BuildReqCreatePlayRoomPacket(
        string playRoomName, 
        bool IsPrivate = false, 
        string password ="", 
        int maxCount = 1,
        byte[]? metaData = null)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqCreateRoomArgs>(
            new ReqCreateRoomArgs(playRoomName, IsPrivate, password, maxCount)
            );
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqCreateRoom, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    protected byte[] BuildReqLeavelPlayRoomPacket(Guid roomId)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqLeaveRoomArgs>(new ReqLeaveRoomArgs(roomId));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqLeaveRoom, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    protected byte[] BuildReqJoinPlayRoomPacket(Guid roomId)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqJoinRoomArgs>(new ReqJoinRoomArgs(roomId, ""));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqJoinRoom, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    protected byte[] BuildReqPlayerListInRoomPacket(Guid roomId)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqPlayerListInRoomArgs>(new ReqPlayerListInRoomArgs(roomId));
        VerifyPacket(dataToSend, SystemPacket.ReqPlayerListInRoom);
        return dataToSend; 
    }

    protected byte[] BuildReqUserActionForUpdatePlayerCustomData(string actionType, byte[] actionParameters)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqUserActionForUpdatePlayerExtendDataArgs>(
            new ReqUserActionForUpdatePlayerExtendDataArgs(actionType, actionParameters)
            );
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqUserActionForUpdatePlayerExtendData, verifyPacket.SystemPacketType);
        return dataToSend;
    }
    
}
