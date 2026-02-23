using System.Security.Cryptography;
using Graphite.Domain.Models;
using Octokit;

namespace Graphite.Api.Services;

public interface IGitHubTokenService
{
    Task<string> GetAccessTokenAsync(GitHubConfig config);
}

public class GitHubTokenService(ILogger<GitHubTokenService> logger) : IGitHubTokenService
{
    private string? _cachedInstallationToken;
    private DateTime _installationTokenExpiration = DateTime.MinValue;

    public async Task<string> GetAccessTokenAsync(GitHubConfig config)
    {
        if (config.UseGitHubApp)
        {
            return await GetGitHubAppAccessTokenAsync(config);
        }

        return config.PersonalAccessToken;
    }

    private async Task<string> GetGitHubAppAccessTokenAsync(GitHubConfig config)
    {
        var installationId = long.Parse(config.InstallationId);

        if (_cachedInstallationToken == null || DateTime.UtcNow >= _installationTokenExpiration)
        {
            try
            {
                var jwt = GenerateJwt(config.AppId, config.PrivateKey);
                logger.LogInformation("Generated JWT for App ID: {AppId}", config.AppId);

                var client = new GitHubClient(new ProductHeaderValue("Graphite-PR-Dashboard"))
                {
                    Credentials = new Credentials(jwt, AuthenticationType.Bearer)
                };

                var installationToken = await client.GitHubApps.CreateInstallationToken(installationId);
                _cachedInstallationToken = installationToken.Token;
                _installationTokenExpiration = installationToken.ExpiresAt.UtcDateTime.AddMinutes(-5);
                logger.LogInformation("Successfully obtained installation token");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating GitHub App JWT or obtaining installation token. AppId: {AppId}, InstallationId: {InstallationId}", config.AppId, config.InstallationId);
                throw;
            }
        }

        return _cachedInstallationToken;
    }

    private string GenerateJwt(string appId, string privateKeyPem)
    {
        try
        {
            ValidatePrivateKey(privateKeyPem);
            var normalizedKey = NormalizePrivateKey(privateKeyPem);

            logger.LogInformation("Generating JWT for App ID: {AppId}", appId);

            var rsa = ImportRsaKey(normalizedKey);
            var (iat, exp) = GetJwtTimestamps();
            var jwt = BuildAndSignJwt(appId, iat, exp, rsa);

            LogJwtDetails(jwt);

            return jwt;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating JWT for App ID: {AppId}", appId);
            throw;
        }
    }

    private void ValidatePrivateKey(string privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            throw new ArgumentException("Private key is empty or whitespace", nameof(privateKeyPem));
        }

        if (!privateKeyPem.StartsWith("-----BEGIN"))
        {
            throw new ArgumentException("Private key does not start with BEGIN marker", nameof(privateKeyPem));
        }

        if (!privateKeyPem.Contains("-----END"))
        {
            throw new ArgumentException("Private key does not contain END marker", nameof(privateKeyPem));
        }
    }

    private static string NormalizePrivateKey(string privateKeyPem)
    {
        var normalized = privateKeyPem;

        if (normalized.Contains("\\n"))
        {
            normalized = normalized.Replace("\\n", "\n");
        }

        normalized = normalized.Replace("\r\n", "\n").Replace("\r", "\n");
        return normalized.Trim();
    }

    private RSA ImportRsaKey(string privateKeyPem)
    {
        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(privateKeyPem);
            logger.LogInformation("Successfully imported RSA private key. Key size: {KeySize} bits", rsa.KeySize);
            return rsa;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import RSA private key");
            throw;
        }
    }

    private static (long iat, long exp) GetJwtTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        var issuedAt = now.AddSeconds(-60);
        var expiresAt = now.AddMinutes(10);

        return (issuedAt.ToUnixTimeSeconds(), expiresAt.ToUnixTimeSeconds());
    }

    private string BuildAndSignJwt(string appId, long iat, long exp, RSA rsa)
    {
        logger.LogInformation("Generating JWT with iat={Iat}, exp={Exp}, iss={Iss}", iat, exp, appId);

        var headerJson = "{\"alg\":\"RS256\",\"typ\":\"JWT\"}";
        var payloadJson = $"{{\"iat\":{iat},\"exp\":{exp},\"iss\":\"{appId}\"}}";

        var headerBase64 = Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(headerJson));
        var payloadBase64 = Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(payloadJson));

        var unsignedToken = $"{headerBase64}.{payloadBase64}";

        var dataToSign = System.Text.Encoding.UTF8.GetBytes(unsignedToken);
        var signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var signatureBase64 = Base64UrlEncode(signature);

        var jwt = $"{unsignedToken}.{signatureBase64}";
        logger.LogInformation("JWT generated successfully. Length: {Length}", jwt.Length);

        return jwt;
    }

    private void LogJwtDetails(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            var headerDecoded = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
            var payloadDecoded = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            logger.LogInformation("JWT Header (decoded): {Header}", headerDecoded);
            logger.LogInformation("JWT Payload (decoded): {Payload}", payloadDecoded);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not decode JWT for verification");
        }
    }

    private static string Base64UrlEncode(byte[] input)
    {
        var output = Convert.ToBase64String(input);
        output = output.TrimEnd('=');
        output = output.Replace('+', '-');
        output = output.Replace('/', '_');
        return output;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input;
        output = output.Replace('-', '+');
        output = output.Replace('_', '/');

        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }

        return Convert.FromBase64String(output);
    }
}
