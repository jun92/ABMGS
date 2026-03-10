using Google.Apis.Auth;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SyncnetPlatform.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SyncnetPlatform.Authentication.GooglePlay;

public class GoogleAuthenticationConst
{
    public static readonly string TokenRequestUrl = "https://oauth2.googleapis.com/token";
    public static readonly string[] Issuers = { "accounts.google.com", "https://accounts.google.com" };
    public static readonly string GrantType = "authorization_code";
    public static readonly string RedirectUri = "postmessage";

}

public interface ISyncnetAuthenticationService
{
    Task<string> GetPlayerIdByGooglePlayAuth(string serverAuthCode);
    Task<Guid> GetPlayerIdByGuest(string identifier);
}

public interface IGooglePlayAuthenticationService
{
    Task<string> Auth(string serverAuthCode);
}

public interface IGuestAuthenticationService
{
    Task<Guid> Auth(string identifier);
}
public class PlayerAuthenticationService : ISyncnetAuthenticationService
{
    private readonly IGooglePlayAuthenticationService _googleAuthService;
    private readonly IGuestAuthenticationService _guestAuthService;

    public PlayerAuthenticationService(
        IGooglePlayAuthenticationService googleAuthService,
        IGuestAuthenticationService guestAuthService)
    {
        _googleAuthService = googleAuthService;
        _guestAuthService = guestAuthService;
    }

    public async Task<string> GetPlayerIdByGooglePlayAuth(string serverAuthCode)
    {
        return await _googleAuthService.Auth(serverAuthCode);
    }
    public async Task<Guid> GetPlayerIdByGuest(string identifier)
    {
        return await _guestAuthService.Auth(identifier);
    }
}

public class GuestAuthenticationService : IGuestAuthenticationService
{
    private readonly ILogger<GuestAuthenticationService> _logger;
    private readonly IExternalIdentityRepository _externalIdentityRepository;
    public GuestAuthenticationService(
        ILogger<GuestAuthenticationService> logger,
        IExternalIdentityRepository externalIdentityRepository
        )
    {
        _logger = logger;
        _externalIdentityRepository = externalIdentityRepository;
    }
    public async Task<Guid> Auth(string identifier)
    {
        return await _externalIdentityRepository.GetOrCreate(Databases.IdProviderType.Guest, identifier);
    }
}

public class GooglePlayAuthenticationService : IGooglePlayAuthenticationService
{
    private readonly ILogger<GooglePlayAuthenticationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleAuthenticationConfiguration _googleAuthenticationOptions;
    public GooglePlayAuthenticationService(
        ILogger<GooglePlayAuthenticationService> logger,
        IHttpClientFactory httpClientFactory,
        
        IOptions<GoogleAuthenticationConfiguration> options
        ) 
    { 
        _logger = logger; 
        _httpClientFactory = httpClientFactory;
        _googleAuthenticationOptions = options.Value;
    }

    public async Task<string> Auth(string serverAuthCode)
    {
        return await HandleSeverAuthCode(serverAuthCode);
    }

    public async Task<string> HandleSeverAuthCode(string serverAuthCode )
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(serverAuthCode);

        using var http = _httpClientFactory.CreateClient();

        var response = await http.PostAsJsonAsync(
            GoogleAuthenticationConst.TokenRequestUrl,
            new GoogleTokenRequest
            {
               client_id = _googleAuthenticationOptions.ClientId,
               client_secret = _googleAuthenticationOptions.ClientSecret,
               code = serverAuthCode,
               grant_type = GoogleAuthenticationConst.GrantType,
               redirect_uri = GoogleAuthenticationConst.RedirectUri
            }
            );
        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.Deserialize<GoogleTokenResponse>(await response.Content.ReadAsStringAsync());
        return await ValidateGoogleJwt(result?.id_token ?? "");
    }

    public async Task<string> ValidateGoogleJwt(string idToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(idToken);
        GoogleAuthenticationConfiguration configuration = new GoogleAuthenticationConfiguration();
        var settings = new GoogleJsonWebSignature.ValidationSettings()
        {
            Audience = new[] { configuration.ClientId },
        };
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        return payload.Subject;
    }
}

public class GoogleAuthenticationConfiguration
{
    [Required] public string ClientId { get; set; } = String.Empty;
    [Required] public string ClientSecret { get; set; } = String.Empty;

}


public class GoogleTokenRequest
{
    [Required] public string code { get; set; } = String.Empty;
    [Required] public string client_id { get; set; } = String.Empty;
    [Required] public string client_secret { get; set; } = String.Empty;
    [Required] public string redirect_uri { get; set; } = String.Empty;
    [Required] public string grant_type { get; set; } = String.Empty;
}

public class GoogleTokenResponse
{
    [Required] public string access_token { get; set; } = String.Empty;
    [Required] public string id_token { get; set; } = String.Empty;
    [Required] public string refresh_token { get; set; } = String.Empty;
    [Required] public int expires_in { get; set; } 
}
