
using Orleans.Runtime;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(static siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("urls");
});

var app = builder.Build();

app.MapGet("/", () => "Welcome to the URL shortener, powered by Orleans!");
app.MapGet("/shorten", static async (IGrainFactory grains, HttpRequest request, string url) =>
{
    var host = $"{request.Scheme}://{request.Host.Value}";

    if(string.IsNullOrWhiteSpace(url) ||
        Uri.IsWellFormedUriString(url, UriKind.Absolute) is false)
    {
        return Results.BadRequest($"""
                The URL query string is required and needs to be well formed.
                Consider, ${host}/shorten?url=https://www.microsoft.com.
                """);
    }

    var shortenedRouteSegment = Guid.NewGuid().GetHashCode().ToString("X");

    var shortenerGrain = grains.GetGrain<IUrlShorteningGrain>(shortenedRouteSegment);

    await shortenerGrain.SetUrl(url);

    var resultBuilder = new UriBuilder(host)
    {
        Path = $"/go/{shortenedRouteSegment}"
    };
    return Results.Ok(resultBuilder);
});

app.MapGet("/go/{shortenedRouteSegment:required}", static async (IGrainFactory grains, string shortenedRouteSegment) => 
{
    var shortenerGrain = grains.GetGrain<IUrlShorteningGrain>(shortenedRouteSegment);
    var url = await shortenerGrain.GetUrl();
    var redirectBuilder = new UriBuilder(url);
    return Results.Redirect(redirectBuilder.Uri.ToString());
});



app.Run();


public interface IUrlShorteningGrain : IGrainWithStringKey
{
    Task SetUrl(string fullUrl);
    Task<string> GetUrl();
}

[GenerateSerializer, Alias(nameof(UrlDetails))]
public sealed record UrlDetails
{
    [Id(0)]
    public string FullUrl { get; init; } = string.Empty;
    [Id(1)]
    public string ShortenedRouteSegment { get; init; } = string.Empty;
}

public sealed class UrlShortenerGrain([PersistentState(stateName:"url", storageName:"urls")] IPersistentState<UrlDetails> state) : Grain, IUrlShorteningGrain
{
    public async Task SetUrl(string fullUrl)
    {
        state.State = new()
        {
            ShortenedRouteSegment = this.GetPrimaryKeyString(),
            FullUrl = fullUrl
        };
        await state.WriteStateAsync();
    }
    public Task<string> GetUrl() => Task.FromResult(state.State.FullUrl);
}

    

