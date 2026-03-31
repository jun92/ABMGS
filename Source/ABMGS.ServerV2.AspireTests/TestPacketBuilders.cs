using Google.FlatBuffers;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABMGS.ServerV2.AspireTest;

public partial class ABMGS_TestMain : IAsyncLifetime
{
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

    protected byte[] BuildReqDirectDeliveryData(Guid toPlayerId, string message, DirectDeliveryDataType dateType)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqDirectDeliveryDataArgs>(
            new ReqDirectDeliveryDataArgs(toPlayerId, message, dateType)
            );
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqDirectDeliveryData, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    protected byte[] BuildReqCreatePlayRoom(string playRoomName)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqCreateRoomArgs>(
            new ReqCreateRoomArgs(playRoomName)
            );
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqCreateRoom, verifyPacket.SystemPacketType);
        return dataToSend;
    }

    protected byte[] BuildReqLeavelPlayRoom(Guid roomId)
    {
        byte[] dataToSend = SyncnetPacketBuilder.Build<ReqLeaveRoomArgs>(new ReqLeaveRoomArgs(roomId));
        PacketWrapper verifyPacket = PacketWrapper.GetRootAsPacketWrapper(new ByteBuffer(dataToSend));
        Assert.Equal(SystemPacket.ReqLeaveRoom, verifyPacket.SystemPacketType);
        return dataToSend;
    }
}
