using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading;

Guid ServerKey = new Guid("D53F2418-10C9-4ABC-9995-14C5C732ABD6");
Uri TargetUri = new Uri($"http://sibur.yandex5.ru/simple-transfer-host?serverKey={ServerKey}");

if (args.Length != 1)
{
    Console.WriteLine("Parameter?");
    return 1;
}

string file = args.Single();

using var fs = File.OpenRead(file);

Console.WriteLine("File opened. Connecting...");

using var handler = new SocketsHttpHandler();
using var httpProgressHandler = new ProgressMessageHandler(handler);
using var client = new HttpClient(httpProgressHandler);
using var content = new StreamContent(fs);
using var request = new HttpRequestMessage(HttpMethod.Post, TargetUri)
{
    Content = content,
};

client.DefaultRequestHeaders.ExpectContinue = true;

httpProgressHandler.HttpSendProgress += (o, e) =>
{
    Console.Write($"Uploading... {Math.Round((double)e.BytesTransferred / e.TotalBytes.Value * 100, 1)}%");

    int fillLength = Console.BufferWidth - Console.CursorLeft - 1;

    if (fillLength > 0)
    {
        Console.Write(new string(' ', fillLength));
    }

    Console.CursorLeft = 0;
};

string fileNameRaw = Path.GetFileName(file);
request.Headers.Add("XT-FileName", fileNameRaw);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (o, e) =>
{
    if (!cts.IsCancellationRequested)
    {
        Console.WriteLine("A cancellation has been invoked");
        e.Cancel = true;
        cts.Cancel();
    }
};

HttpResponseMessage response;

Console.CursorVisible = false;

try
{
    response = await client.SendAsync(request, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Uploading has been cancelled");
    return 0;
}

Console.CursorVisible = true;
Console.WriteLine();

using (response)
{
    switch (response.StatusCode)
    {
        case HttpStatusCode.Conflict:
            Console.WriteLine("Server is busy with another upload request.");
            return (int)response.StatusCode;

        case HttpStatusCode.Unauthorized:
            Console.WriteLine("Client key is invalid.");
            return (int)response.StatusCode;

        case HttpStatusCode.BadRequest:
            Console.WriteLine("Filename is invalid.");
            return (int)response.StatusCode;

        case HttpStatusCode.OK:
            Console.WriteLine("File sent.");
            return 0;

        default:
            Console.WriteLine($"Unexpected HTTP result: {response.StatusCode}.");
            return (int)response.StatusCode;
    }
}