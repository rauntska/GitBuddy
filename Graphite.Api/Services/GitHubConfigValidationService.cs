using Graphite.Domain.Models;

namespace Graphite.Api.Services;

public interface IGitHubConfigValidationService
{
    bool IsValidConfiguration(GitHubConfig? config);
}

public class GitHubConfigValidationService : IGitHubConfigValidationService
{
    public bool IsValidConfiguration(GitHubConfig? config)
    {
        if (config == null || string.IsNullOrEmpty(config.Organization))
            return false;

        if (!config.UseGitHubApp)
            return false;

        return !string.IsNullOrEmpty(config.AppId) &&
               !string.IsNullOrEmpty(config.PrivateKey) &&
               !string.IsNullOrEmpty(config.InstallationId);
    }
}
