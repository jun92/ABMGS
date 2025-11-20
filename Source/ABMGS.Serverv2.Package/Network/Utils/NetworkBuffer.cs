using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace SyncnetPlatform.Network.Utils;

public sealed class NetworkBuffer: IDisposable
{
    private readonly Pipe _pipe;
    private readonly int _bufferSize;
    public NetworkBuffer(int bufferSize)
    {
        _pipe = new Pipe();
        _bufferSize = bufferSize;
    }

    public Memory<byte> GetReceiveBuffer() => _pipe.Writer.GetMemory(_bufferSize);
    public void AddBuffer(int receivedByteCount) => _pipe.Writer.Advance(receivedByteCount);
    public async Task FinishReceived()
    {
        _ = await _pipe.Writer.FlushAsync();
        await _pipe.Writer.CompleteAsync();
    }

    public async Task<byte[]> Read()
    {
        ReadResult readResult = await _pipe.Reader.ReadAsync();
        var ReturnData = readResult.Buffer.ToArray();
        _pipe.Reader.AdvanceTo(readResult.Buffer.End);
        return ReturnData;

    }
    void IDisposable.Dispose()
    {
        _pipe.Reader.Complete();
        _pipe.Writer.Complete();
    }
}
