using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace SyncnetPlatform.Network.Utils;

public sealed class NetworkBuffer: IDisposable
{
    private readonly Pipe _pipe;
    private readonly int _bufferSize;
    private ReadResult _readResult;
    public NetworkBuffer(int bufferSize)
    {
        _pipe = new Pipe();
        _bufferSize = bufferSize;
    }

    public Memory<byte> GetReceiveBuffer() => _pipe.Writer.GetMemory(_bufferSize);
    public void AddBuffer(int receivedByteCount) => _pipe.Writer.Advance(receivedByteCount);
    public async Task FinishReceived()
    {
        FlushResult result = await _pipe.Writer.FlushAsync();
        await _pipe.Writer.CompleteAsync();
    }

    public async Task<byte[]> Read()
    {
        _readResult = await _pipe.Reader.ReadAsync();
        return _readResult.Buffer.ToArray();

    }
    void IDisposable.Dispose()
    {
        _pipe.Reader.AdvanceTo(_readResult.Buffer.End);
        _pipe.Reader.Complete();
        _pipe.Writer.Complete();
        
    }
}
