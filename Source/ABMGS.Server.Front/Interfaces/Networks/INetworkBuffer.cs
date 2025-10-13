namespace ABMGS.Server.Front.Interfaces.Networks;

public interface INetworkBuffer
{
    public void AddReceiveData(byte[] data);
    public byte[] GetReceiveData();
    public void PushSendData(byte[] data);
    public byte[] PopSendData();
}
