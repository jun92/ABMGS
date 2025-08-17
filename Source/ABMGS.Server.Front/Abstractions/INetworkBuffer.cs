namespace ABMGS.Server.Front.Abstractions;

public interface INetworkBuffer
{
    public void AddReceiveData(byte[] data);
    public byte[] GetReceiveData();
    public void PushSendData(byte[] data);
    public byte[] PopSendData();
}
