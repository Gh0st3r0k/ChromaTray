using System.Drawing;
using System.Net;
using System.Text;
using System.Text.Json;
using ChromaTray.Services;
using Xunit;

namespace ChromaTray.Core.Tests;

public class ColormindClientTests
{
    [Fact]
    public async Task SeedIsSentWithFourEmptyPositionsAndPreservedExactly()
    {
        var seed = Color.FromArgb(152, 222, 217);
        var handler = new Handler(async request =>
        {
            Assert.Equal("https", request.RequestUri!.Scheme);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            var input = json.RootElement.GetProperty("input");
            Assert.Equal("ui", json.RootElement.GetProperty("model").GetString());
            Assert.Equal(new[] { 152, 222, 217 }, input[0].EnumerateArray().Select(c => c.GetInt32()));
            Assert.All(input.EnumerateArray().Skip(1), value => Assert.Equal("N", value.GetString()));
            return Reply("{\"result\":[[150,220,215],[1,2,3],[4,5,6],[7,8,9],[10,11,12]]}");
        });
        using var http = new HttpClient(handler);
        var result = await new ColormindClient(http).GenerateAsync([seed, null, null, null, null], default);
        Assert.Equal(seed, result[0]);
        Assert.Equal(Color.FromArgb(1, 2, 3), result[1]);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"result\":[[1,2,3]]}")]
    [InlineData("{\"result\":[[256,0,0],[1,2,3],[1,2,3],[1,2,3],[1,2,3]]}")]
    [InlineData("{\"result\":[null,[1,2,3],[1,2,3],[1,2,3],[1,2,3]]}")]
    public async Task MalformedResponseIsRejected(string json)
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(Reply(json))));
        await Assert.ThrowsAsync<InvalidDataException>(() => new ColormindClient(http).GenerateAsync(new Color?[5], default));
    }

    [Fact]
    public async Task AllLockedDoesNotCallService()
    {
        using var http = new HttpClient(new Handler(_ => throw new Exception("Network must not be called")));
        var input = Enumerable.Repeat((Color?)Color.Red, 5).ToArray();
        Assert.All(await new ColormindClient(http).GenerateAsync(input, default), color => Assert.Equal(Color.Red, color));
    }

    [Fact]
    public async Task ServiceFailureAndCancellationArePropagated()
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));
        await Assert.ThrowsAsync<HttpRequestException>(() => new ColormindClient(http).GenerateAsync(new Color?[5], default));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ColormindClient(http).GenerateAsync(new Color?[5], cancellation.Token));
    }

    private static HttpResponseMessage Reply(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return respond(request);
        }
    }
}
