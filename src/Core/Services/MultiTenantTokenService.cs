using Azure.DataApiBuilder.Core.Configurations;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Azure.DataApiBuilder.Core.Services;

public class MultiTenantTokenService
{
    private readonly string _tenantEncryptKey = "";

    public MultiTenantTokenService(RuntimeConfigProvider runtimeConfigProvider)
    {
        if (runtimeConfigProvider == null)
        {
            throw new ArgumentNullException(nameof(runtimeConfigProvider));
        }

        if (!runtimeConfigProvider.GetConfig().IsEnabledMultiTenancy)
        {
            return;
        }

        Config.ObjectModel.RuntimeConfig config = runtimeConfigProvider.GetConfig();
        if (config?.Runtime?.MultiTenancy?.TenantEncryptKey == null)
        {
            throw new ArgumentNullException("TenantEncryptKey is null in the configuration.");
        }

        _tenantEncryptKey = config.Runtime.MultiTenancy.TenantEncryptKey;
    }

    public string GenerateToken(string tenantId)
    {
        JwtSecurityTokenHandler tokenHandler = new ();
        byte[] key = Encoding.ASCII.GetBytes(_tenantEncryptKey);
        SecurityTokenDescriptor tokenDescriptor = new ()
        {
            Subject = new ClaimsIdentity(new[] { new Claim("tenantId", tenantId) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string DecodeToken(string token)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        byte[] key = Encoding.ASCII.GetBytes(_tenantEncryptKey);
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,  // Still validate the key
            RequireSignedTokens = false,      // Disable the need for a valid signature (including kid)
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            RequireExpirationTime = false,
            ValidateLifetime = false
        }, out SecurityToken validatedToken);

        JwtSecurityToken jwtToken = (JwtSecurityToken)validatedToken;
        string tenantId = jwtToken.Claims.First(x => x.Type.Equals("tenantId", StringComparison.InvariantCultureIgnoreCase)).Value;

        return tenantId;
    }
}
