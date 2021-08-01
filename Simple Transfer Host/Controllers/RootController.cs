using STH.Entities;
using STH.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace STH.Controllers
{
    [Route]
    public sealed class RootController : ApiController
    {
        internal static readonly Guid _clientKey = new Guid("2CE69CD2-187C-45D3-8CD0-D3FE8BD44751");
        internal static readonly Guid _serverKey = new Guid("D53F2418-10C9-4ABC-9995-14C5C732ABD6");

        private readonly ConnectorService _connector = new ConnectorService();

        public RootController(ConnectorService connector)
        {
            _connector = connector;
        }

        public async Task<object> Get(Guid clientKey, CancellationToken token)
        {
            if (clientKey != _clientKey)
            {
                return StatusCode(HttpStatusCode.Unauthorized);
            }

            var data = await _connector.TryProcessGet(token);

            if (data == null)
            {
                return StatusCode(HttpStatusCode.Conflict);
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
        public async Task<StatusCodeResult> Post(Guid serverKey, CancellationToken token)
        {
            if (serverKey != _serverKey)
            {
                return StatusCode(HttpStatusCode.Unauthorized);
            }

            if (!Request.Headers.TryGetValues("XT-FileName", out var values))
            {
                return StatusCode(HttpStatusCode.BadRequest);
            }

            string filename = values.SingleOrDefault();

            if (string.IsNullOrWhiteSpace(filename) || Path.GetInvalidFileNameChars().Intersect(filename).Any())
            {
                return StatusCode(HttpStatusCode.BadRequest);
            }

            Stream stream = await Request.Content.ReadAsStreamAsync();

            bool result = await _connector.TryProcessPost(new TransferData(filename, stream, token));

            return StatusCode(result ? HttpStatusCode.OK : HttpStatusCode.Conflict);
        }
    }
}
