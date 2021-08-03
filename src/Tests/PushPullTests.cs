using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using Shouldly;
using SimpleTransferHost.Instance.Entities;
using SimpleTransferHost.Instance.Services.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Tests
{
    public class Tests
    {
        private static readonly Random _r = new Random();

        private MemoryStream GenerateStream(long size)
        {
            byte[] arr = new byte[size];
            _r.NextBytes(arr);
            return new MemoryStream(arr);
        }

        [Test]
        public async Task SimpleTransfer()
        {
            var fakeRequest = A.Fake<HttpRequest>();
            A.CallTo(() => fakeRequest.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues>() { ["XT-FileName"] = "haha.txt" }));

            using MemoryStream dataSteam = GenerateStream(50_000);
            A.CallTo(() => fakeRequest.Body).Returns(dataSteam);

            var fakeContext = A.Fake<HttpContext>();
            A.CallTo(() => fakeContext.Request).Returns(fakeRequest);

            var bb = new BufferBlock<AsyncOp<FileStreamData>>(new DataflowBlockOptions
            {
                BoundedCapacity = 1
            });

            var getRoot = new RootController(bb, NullLogger<RootController>.Instance);
            var postRoot = new RootController(bb, NullLogger<RootController>.Instance);

            Task<object> getTask = getRoot.Get(RootController._clientKey, CancellationToken.None);

            var ctx = new ControllerContext
            {
                HttpContext = fakeContext
            };

            postRoot.ControllerContext = ctx;

            var postTask = postRoot.Post(RootController._serverKey, CancellationToken.None);

            await Task.WhenAll(getTask.ContinueWith(async T =>
            {
                HttpResponseMessage response = (HttpResponseMessage)T.Result;

                response.StatusCode.ShouldBe(HttpStatusCode.OK);

                byte[] read = await response.Content.ReadAsByteArrayAsync();
                read.Length.ShouldBe(50000);
            }), postTask);

            postTask.Result.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        }
    }
}