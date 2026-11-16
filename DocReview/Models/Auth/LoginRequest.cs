using System.Text.Json.Serialization;

namespace DocReview.Models.Auth;

public sealed record LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; init; } = null!;

    [JsonPropertyName("password")]
    public string Password { get; init; } = null!;
}