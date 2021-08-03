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
            using MemoryStream dataSteam = GenerateStream(50_000);
            using MemoryStream writeStream = new MemoryStream();

            var fakeRequest = A.Fake<HttpRequest>();
            A.CallTo(() => fakeRequest.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues>() { ["XT-FileName"] = "haha.txt" }));
            A.CallTo(() => fakeRequest.Body).Returns(dataSteam);

            var fakeResponse = A.Fake<HttpResponse>();
            A.CallTo(() => fakeResponse.Headers).Returns(new HeaderDictionary());
            A.CallTo(() => fakeResponse.Body).Returns(writeStream);

            var fakePostContext = A.Fake<HttpContext>();
            A.CallTo(() => fakePostContext.Request).Returns(fakeRequest);

            var fakeGetContext = A.Fake<HttpContext>();
            A.CallTo(() => fakeGetContext.Response).Returns(fakeResponse);

            var bb = new BufferBlock<AsyncOp<FileStreamData>>(new DataflowBlockOptions
            {
                BoundedCapacity = 1
            });

            var getRoot = new RootController(bb, NullLogger<RootController>.Instance);
            var postRoot = new RootController(bb, NullLogger<RootController>.Instance);

            Task<object> getTask = getRoot.Get(RootController._clientKey, CancellationToken.None);

            var getContext = new ControllerContext
            {
                HttpContext = fakeGetContext
            };

            var postContext = new ControllerContext
            {
                HttpContext = fakePostContext
            };

            postRoot.ControllerContext = postContext;
            getRoot.ControllerContext = getContext;

            var postTask = postRoot.Post(RootController._serverKey, CancellationToken.None);

            await Task.WhenAll(getTask.ContinueWith(async T =>
            {
                var response = (OkResult)T.Result;

                writeStream.Seek(0, SeekOrigin.Begin);
                byte[] read = writeStream.ToArray();
                read.Length.ShouldBe(50000);
            }), postTask);

            postTask.Result.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        }
    }
}