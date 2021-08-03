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
using System.Net.Mime;
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
        private readonly ILogger<RootController> _logger;

        public RootController(BufferBlock<AsyncOp<FileStreamData>> buffer, ILogger<RootController> logger)
        {
            _buffer = buffer;
            _logger = logger;
        }

        [HttpGet]
        public async Task Get(Guid clientKey, CancellationToken ct)
        {
            if (clientKey != _clientKey)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }

            _logger.LogInformation("Received GET request");

            var op = await _buffer.ReceiveAsync(ct);

            ContentDisposition cd = new ContentDisposition
            {
                FileName = op.Data.FileName,
                Inline = true  // false = prompt the user for downloading;  true = browser to try to show the file inline
            };

            Response.Headers.Add("Content-Disposition", cd.ToString());

            _logger.LogInformation("Consumed awaiting Stream, pushing POST stream into the GET stream");

            Response.StatusCode = (int)HttpStatusCode.OK;

            await op.Data.DataStream.CopyToAsync(Response.Body, ct);

            op.OnFinish();
        }

        [HttpPost]
        [DisableRequestSizeLimit]
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

            _logger.LogInformation("Received POST push, trying to find buffer reader");

            bool result = await _buffer.SendAsync(new AsyncOp<FileStreamData>(new FileStreamData(filename, Request.Body), () => tcs.SetResult(), ct), ct);

            _logger.LogInformation("Reader consumed our stream message, waiting for end of read");

            if (!result)
            {
                return Conflict();
            }

            await tcs.Task;

            _logger.LogInformation("The end of read has been reached, all OK.");

            return Ok();
        }
    }
}