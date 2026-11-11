using System.Text.Json.Serialization;
using DocReview.DataModels;

namespace DocReview.Models.Auth;

public record RegisterRequest
{
    [JsonPropertyName("username")]
    public string Username { get; init; } = null!;

    [JsonPropertyName("password")]
    public string Password { get; init; } = null!;

    [JsonPropertyName("role")]
    public UserRole Role { get; init; }
};