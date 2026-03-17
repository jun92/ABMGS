using Google.FlatBuffers;
using SyncnetPlatform.Extensions;
using SyncnetPlatform.Protocols.Generated;

namespace SyncnetPlatform.Network.Utils;


internal class ReqDirectDeliveryDataPacketBuilder : PacketBABuilder<ReqDirectDeliveryDataArgs>
{
    public override byte[] Build(ReqDirectDeliveryDataArgs args)
    {
        var builder = CreateBuilder();

        var m = builder.CreateString(args.Message);

        ReqDirectDeliveryData.StartReqDirectDeliveryData(builder);
        ReqDirectDeliveryData.AddToPlayerId(builder, args.ToPlayerId.ToGuidType(builder));
        ReqDirectDeliveryData.AddData(builder, m);
        ReqDirectDeliveryData.AddDataType(builder, args.DateType);

        return Wrap(
            builder, 
            SystemPacket.ReqDirectDeliveryData, 
            ReqDirectDeliveryData.EndReqDirectDeliveryData(builder).Value);
    }
}

internal class ResDirectDeliveryDataPacketBuilder : PacketBABuilder<ResDirectDeliveryDataArgs>
{
    public override byte[] Build(ResDirectDeliveryDataArgs args)
    {
        var builder = CreateBuilder();
        var data = ResDirectDeliveryData.CreateResDirectDeliveryData(
            builder, 
            args.ErrorCode, 
            builder.CreateString(args.Message));
        return Wrap(builder, SystemPacket.ResDirectDeliveryData, data.Value);

    }
}
internal class OnDirectDeliveryDataPacketBuilder : PacketBABuilder<OnDirectDeliveryDataArgs>
{
    public override byte[] Build(OnDirectDeliveryDataArgs args)
    {
        var builder = CreateBuilder();
        OnDirectDeliveryData.StartOnDirectDeliveryData(builder);
        OnDirectDeliveryData.AddFromPlayerId(builder, args.FromPlayerId.ToGuidType(builder));
        OnDirectDeliveryData.AddData(builder, builder.CreateString(args.Message));
        OnDirectDeliveryData.AddDataType(builder, args.DataType);

        return Wrap(
            builder, 
            SystemPacket.OnDirectDeliveryData, 
            OnDirectDeliveryData.EndOnDirectDeliveryData(builder).Value);
    }
}