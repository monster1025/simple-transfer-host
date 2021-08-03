using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SimpleTransferHost.Instance.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SimpleTransferHost.Instance.Services.WebApi
{
    [Route("")]
    public sealed class RootController : ControllerBase
    {
        internal static readonly Guid _clientKey = new Guid("2CE69CD2-187C-45D3-8CD0-D3FE8BD44751");
        internal static readonly Guid _serverKey = new Guid("D53F2418-10C9-4ABC-9995-14C5C732ABD6");

        private readonly BufferBlock<AsyncOp<FileStreamData>> _buffer;

        public RootController(BufferBlock<AsyncOp<FileStreamData>> buffer)
        {
            _buffer = buffer;
        }

        [HttpGet]
        public async Task<object> Get(Guid clientKey, CancellationToken ct)
        {
            if (clientKey != _clientKey)
            {
                return Unauthorized();
            }

            var op = await _buffer.ReceiveAsync(ct);

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new PushStreamContent(async (stream, _, __) =>
                {
                    await op.Data.DataStream.CopyToAsync(stream, ct);
                    await stream.FlushAsync();
                    stream.Close();

                    op.OnFinish();
                }, new MediaTypeHeaderValue("application/octet-stream"))
            };

            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = op.Data.FileName
            };

            return result;
        }

        [HttpPost]
        public async Task<StatusCodeResult> Post(Guid serverKey, CancellationToken ct)
        {
            if (serverKey != _serverKey)
            {
                return Unauthorized();
            }

            if (!Request.Headers.TryGetValue("XT-FileName", out var values))
            {
                return BadRequest();
            }

            string? filename = values.SingleOrDefault();

            if (string.IsNullOrWhiteSpace(filename) || Path.GetInvalidFileNameChars().Intersect(filename).Any())
            {
                return BadRequest();
            }

            TaskCompletionSource tcs = new();

            bool result = await _buffer.SendAsync(new AsyncOp<FileStreamData>(new FileStreamData(filename, Request.Body), () => tcs.SetResult(), ct), ct);

            if (!result)
            {
                return Conflict();
            }

            await tcs.Task;

            return Ok();
        }
    }
}