using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SyncnetPlatform.Authentication.SyncnetAuthProvider;

public interface ISyncnetJwtAuthenticationService
{
    string IssueNewToken(string playerId);
}
public class SyncnetAuthenticationService : ISyncnetJwtAuthenticationService
{
    private readonly SyncnetAuthenticationOptions _options;
    private readonly ILogger<SyncnetAuthenticationService> _logger;

    public SyncnetAuthenticationService(
        ILogger<SyncnetAuthenticationService> logger, 
        IOptions<SyncnetAuthenticationOptions> options
        )
    {
        _logger = logger;
        _options = options.Value;
    }


    public string IssueNewToken(string syncnetPlayerId)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_options.SecretKey));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, syncnetPlayerId),
            new Claim(JwtRegisteredClaimNames.Iss, _options.Issuer),
            new Claim(JwtRegisteredClaimNames.Aud, _options.Audience),
        };

        JwtSecurityToken newToken = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiresInMins),
            signingCredentials: credentials
            );
        return new JwtSecurityTokenHandler().WriteToken(newToken);
    }

}

public class SyncnetAuthenticationOptions
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public string SecretKey { get; set; }
    public int AccessTokenExpiresInMins { get; set; }
}

