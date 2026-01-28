namespace Graphite.Api.DTOs;

public record LoginResponseDto(
    string Token,
    string TokenType,
    int ExpiresIn,
    CurrentUserDto User
);

public record CurrentUserDto(
    int Id,
    string Username,
    string Email,
    string? AvatarUrl
);

public record GitHubUserDto(
    long Id,
    string Login,
    string? Email,
    string? AvatarUrl
);

public record GitHubAccessTokenResponse(
    string Access_Token,
    string Token_Type,
    string Scope
);
