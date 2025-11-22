using SyncnetPlatform.Extensions;
using System.Collections.Concurrent;

namespace SyncnetPlatform.Utils;

public class QueueWithTCS<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly object _lock = new();
    private TaskCompletionSource<bool>? _waiter = null;

    public void Enqueue(T item)
    {
        TaskCompletionSource<bool>? waiterToRelease = null;
        lock (_lock)
        {
            bool wasEmpty = _queue.IsEmpty;

            _queue.Enqueue(item);

            if(wasEmpty && _waiter != null )
            {
                waiterToRelease = _waiter;
                _waiter = null;
            }
        }
        waiterToRelease?.TrySetResult(true);
    }
    public async Task<T> DequeueAsync(CancellationToken ct = default)
    {
        T? item;
        if (_queue.TryDequeue(out item))
        {
            return item;
        }

        TaskCompletionSource<bool> waiter;
        lock (_lock)
        {
            if (_queue.TryDequeue(out item))
            {
                return item;
            }
            _waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            waiter = _waiter;
        }
        if(ct.CanBeCanceled)
        {
            var completed = await Task.WhenAny(waiter.Task, ct.AsTask());
            if (completed == ct.AsTask())
                throw new OperationCanceledException(ct);
        }
        else
        {
            await waiter.Task;
        }
        _queue.TryDequeue(out item);
        return item!;
    }
   

}

