using Nito.AsyncEx;
using SimpleTransferHost.Instance.Entities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SimpleTransferHost.Instance.Services
{
    public sealed class ConnectorService
    {
        public sealed record GetResult(string FileName, Func<Stream, Task> Action);

        private sealed record InternalData(TransferData Transfer, Action<Task> NextStep);

        private readonly BufferBlock<GetResult> _buffer = new(new DataflowBlockOptions
        {
            BoundedCapacity = 1,
        });

        private readonly AsyncLock _postLock = new AsyncLock();
        private readonly AsyncLock _getLock = new AsyncLock();

        private InternalData? _internal = null;
        private bool _postAwaits = false;
        private bool _getterAwaits = false;

        public async Task<bool> TryProcessPost(TransferData transferData, CancellationToken ct)
        {
            await _buffer.SendAsync(new GetResult(transferData.FileName, outputStream =>
            {
                return transferData.DataStream.CopyToAsync(outputStream, ct);
            }));

            #region Sync Step

            using (await _postLock.LockAsync(transferData.Token))
            {
                if (_postAwaits)
                {
                    return false;
                }

                _postAwaits = true;
            }

            #endregion

            var dataRead = new TaskCompletionSource<Task>();
            var transferCancelled = new TaskCompletionSource<bool>();

            _internal = new InternalData(transferData, T => dataRead.SetResult(T));

            using (transferData.Token.Register(() => transferCancelled.SetResult(true)))
            {
                await Task.WhenAny(dataRead.Task, transferCancelled.Task);
            }

            _internal = null;
            _postAwaits = false;
            // Next TryProcessPost can now enter the stage.

            if (!dataRead.Task.IsCompleted)
            {
                throw new OperationCanceledException(transferData.Token);
            }

            await await dataRead.Task;

            return true;
        }
        public async Task<GetResult?> TryProcessGet(CancellationToken ct)
        {
            #region Sync Step

            using (await _getLock.LockAsync(ct))
            {
                if (_getterAwaits)
                {
                    return null;
                }

                _getterAwaits = true;
            }

            #endregion

            InternalData? data = null;

            try
            {
                while ((data = _internal) == null)
                {
                    await Task.Delay(200, ct);
                }

                _internal = null;
            }
            finally
            {
                _getterAwaits = false;
            }

            // Next TryProcessGet can now enter the stage.

            return new GetResult(data.Transfer.FileName, outputStream =>
            {
                Task copyTask = Task.Run(async () =>
                {
                    using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct, data.Transfer.Token);

                    await data.Transfer.DataStream.CopyToAsync(outputStream, 81920, cts.Token);
                });

                data.NextStep(copyTask);

                return copyTask;
            });
        }
    }
}
