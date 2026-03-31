using SyncnetPlatform.Extensions;
using SyncnetPlatform.Protocols.Generated;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABMGS.ServerV2.AspireTest;

public partial class ABMGS_TestMain : IAsyncLifetime
{
    [Fact]
    public async Task PlayroomCreationAndDestructionTest()
    {
        var wsClient = await CreateAuthoredWebSocket();
        // Enter
        byte[] dataToSend = BuildReqCreatePlayRoom("CreateRoomTestTitle");
        await SendDataAsync(wsClient, dataToSend);
        var (receiveBuffer, result) = await ReceiveAsync(wsClient);
        PacketWrapper packetWrapper = AsPacketWrapper(receiveBuffer, result.Count);
        Assert.Equal(SystemPacket.ResCreateRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResCreateRoom().Result);
        
        Guid roomId = new Guid();
        roomId.FromGuidType(packetWrapper.SystemPacketAsResCreateRoom().RoomId);

        _output.WriteLine(roomId.ToString());

        // Leave
        dataToSend = BuildReqLeavelPlayRoom(roomId);
        await SendDataAsync(wsClient, dataToSend);
        (receiveBuffer, result) = await ReceiveAsync(wsClient);
        packetWrapper = AsPacketWrapper(receiveBuffer, result.Count);
        Assert.Equal(SystemPacket.ResLeaveRoom, packetWrapper.SystemPacketType);
        Assert.Equal(PacketErrorCodes.Success, packetWrapper.SystemPacketAsResLeaveRoom().Result);
    }

}
