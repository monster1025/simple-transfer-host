using Microsoft.AspNetCore.Mvc;
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

namespace SimpleTransferHost.Instance.Services.WebApi
{
    public sealed class RootController : ControllerBase
    {
        internal static readonly Guid _clientKey = new Guid("2CE69CD2-187C-45D3-8CD0-D3FE8BD44751");
        internal static readonly Guid _serverKey = new Guid("D53F2418-10C9-4ABC-9995-14C5C732ABD6");

        private readonly ConnectorService _connector;

        public RootController(ConnectorService connector)
        {
            _connector = connector;
        }

        public async Task<object> Get(Guid clientKey, CancellationToken ct)
        {
            if (clientKey != _clientKey)
            {
                return Unauthorized();
            }

            var data = await _connector.TryProcessGet(ct);

            if (data == null)
            {
                return Conflict();
            }

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new PushStreamContent(async (stream, _, __) =>
                {
                    await data.Action(stream);

                    await stream.FlushAsync();
                    stream.Close();
                }, new MediaTypeHeaderValue("application/octet-stream"))
            };

            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = data.FileName
            };

            return result;
        }
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

            bool result = await _connector.TryProcessPost(new TransferData(filename, Request.Body, ct), ct);

            if (result)
            {
                return Ok();
            }

            return Conflict();
        }
    }
}