using System;
using System.Collections.Generic;
using System.Text;

namespace SyncnetPlatform.Extensions;

public static class CancellationTokenExtensions
{
    public static Task AsTask(this CancellationToken token)
    {
        var tcs = new TaskCompletionSource<bool>();

        if (token.IsCancellationRequested)
        {
            tcs.TrySetCanceled(token);
            return tcs.Task;
        }

        token.Register(() => tcs.TrySetCanceled(token));

        return tcs.Task;
    }
}
