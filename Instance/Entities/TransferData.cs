using System.IO;
using System.Threading;

namespace SimpleTransferHost.Instance.Entities
{
    public sealed record TransferData(string FileName, Stream DataStream, CancellationToken Token);
}
