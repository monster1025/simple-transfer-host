using System;
using System.Threading;

namespace SimpleTransferHost.Instance.Entities
{
    public sealed record AsyncOp<T>(T Data, Action OnFinish, CancellationToken CancellationToken);
}
