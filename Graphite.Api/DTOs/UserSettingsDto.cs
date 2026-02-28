namespace Graphite.Api.DTOs;

public record UserSettingsDto(
    string? PersonalAccessToken,
    bool HasPersonalAccessToken
);

public record UpdateUserSettingsRequest(
    string? PersonalAccessToken
);
