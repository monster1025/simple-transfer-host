using Nito.AsyncEx;
using STH.Entities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace STH.Services
{
    public sealed class ConnectorService
    {
        #region GetResult

        public sealed class GetResult
        {
            public string FileName { get; }
            public Func<Stream, Task> Action { get; }

            public GetResult(string fileName, Func<Stream, Task> action)
            {
                Action = action;
                FileName = fileName;
            }
        }

        #endregion
        #region InternalData

        private sealed class InternalData
        {
            public TransferData Transfer { get; set; }
            public Action<Task> NextStep { get; set; }
        }

        #endregion

        private readonly AsyncLock _postLock = new AsyncLock();
        private readonly AsyncLock _getLock = new AsyncLock();

        private InternalData _internal = null;
        private bool _postAwaits = false;
        private bool _getterAwaits = false;

        public async Task<bool> TryProcessPost(TransferData transferData)
        {
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

            _internal = new InternalData
            {
                Transfer = transferData,
                NextStep = T => dataRead.SetResult(T)
            };

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
        public async Task<GetResult> TryProcessGet(CancellationToken token)
        {
            #region Sync Step

            using (await _getLock.LockAsync(token))
            {
                if (_getterAwaits)
                {
                    return null;
                }

                _getterAwaits = true;
            }

            #endregion

            InternalData data = null;

            try
            {
                while ((data = _internal) == null)
                {
                    await Task.Delay(200, token);
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
                    using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token, data.Transfer.Token))
                    {
                        await data.Transfer.InputStream.CopyToAsync(outputStream, 81920, cts.Token);
                    }
                });

                data.NextStep(copyTask);

                return copyTask;
            });
        }
    }
}
