using Google.Apis.Auth;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
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

public interface IAuthenticationService
{

}
public class PlayerAuthenticationService : IAuthenticationService
{

}

public class GooglePlayAuthenticationService
{
    private readonly ILogger<GooglePlayAuthenticationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    public GooglePlayAuthenticationService(
        ILogger<GooglePlayAuthenticationService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configure
        ) 
    { 
        _logger = logger; 
        _httpClientFactory = httpClientFactory;
        _configuration = configure;
    }

    public async Task HandleSeverAuthCode(string serverAuthCode )
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(serverAuthCode);

        using var http = _httpClientFactory.CreateClient();
        GoogleAuthenticationConfiguration configuration = new GoogleAuthenticationConfiguration();

        var response = await http.PostAsJsonAsync(
            GoogleAuthenticationConst.TokenRequestUrl,
            new GoogleTokenRequest
            {
               client_id = configuration.ClientId,
               client_secret = configuration.ClientSecret,
               code = serverAuthCode,
               grant_type = GoogleAuthenticationConst.GrantType,
               redirect_uri = GoogleAuthenticationConst.RedirectUri
            }
            );
        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.Deserialize<GoogleTokenResponse>(await response.Content.ReadAsStringAsync());

    }

    public async Task ValidateGoogleJwt(string idToken)
    {
        GoogleAuthenticationConfiguration configuration = new GoogleAuthenticationConfiguration();

        var settings = new GoogleJsonWebSignature.ValidationSettings()
        {
            Audience = new[] { configuration.ClientId },
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
    }

}

public class GoogleAuthenticationConfiguration
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }

}


public class GoogleTokenRequest
{
    public string code { get; set; }
    public string client_id { get; set; }
    public string client_secret { get; set; }
    public string redirect_uri { get; set; }
    public string grant_type { get; set; }

}

public class GoogleTokenResponse
{
    public string access_token { get; set; }
    public string id_token { get; set; }  
    public string refresh_token { get; set; }
    public int expires_in { get; set; }
}
