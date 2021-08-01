using System;
using System.IO;
using System.Threading;

namespace STH.Entities
{
    public sealed class TransferData
    {
        public string FileName { get; }
        public Stream InputStream { get; }
        public CancellationToken Token { get; }

        public TransferData(string fileName, Stream dataStream, CancellationToken token)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            InputStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
            Token = token;
        }
    }
}
