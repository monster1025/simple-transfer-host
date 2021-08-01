using Microsoft.VisualStudio.TestTools.UnitTesting;
using STH.Controllers;
using STH.Services;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace STH.Tests
{
    [TestClass]
    public class BehaviourTests
    {
        private static readonly Random _r = new Random();


        private MemoryStream GenerateStream(long size)
        {
            byte[] arr = new byte[size];
            _r.NextBytes(arr);
            return new MemoryStream(arr);
        }

        [TestMethod]
        public async Task SimpleTransfer()
        {
            var conn = new ConnectorService();

            var root1 = new RootController(conn);
            var root2 = new RootController(conn);

            Task<object> getTask = root1.Get(RootController._clientKey, CancellationToken.None);

            using (var rm = new HttpRequestMessage())
            using (MemoryStream inputStream = GenerateStream(50_000))
            using (StreamContent content = new StreamContent(inputStream))
            {
                rm.Headers.Add("XT-FileName", "haha.txt");
                rm.Content = content;
                root2.Request = rm;

                var postTask = root2.Post(RootController._serverKey, CancellationToken.None);

                await Task.WhenAll(getTask.ContinueWith(async T =>
                {
                    HttpResponseMessage response = (HttpResponseMessage)T.Result;

                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);

                    byte[] read = await response.Content.ReadAsByteArrayAsync();
                    Assert.AreEqual(read.Length, 50000);
                }), postTask);

                Assert.AreEqual(postTask.Result.StatusCode, HttpStatusCode.OK);
            }
        }
    }
}
