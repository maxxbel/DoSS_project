namespace DocReview.Models.Auth;

public record AuthResponse(
    string Token,
    UserDto User
);