using System.IO;

namespace SimpleTransferHost.Instance.Entities
{
    public sealed record FileStreamData(string FileName, Stream DataStream);
}
