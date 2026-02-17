namespace Graphite.Api.DTOs;

using System.Text.Json.Serialization;
using Graphite.Domain.Models;

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
    string? AvatarUrl,
    UserRole Role
);

public record GitHubUserDto(
    long Id,
    string Login,
    string? Email,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl
);

public record GitHubAccessTokenResponse(
    string Access_Token,
    string Token_Type,
    string Scope
);
