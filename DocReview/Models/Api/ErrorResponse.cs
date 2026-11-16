namespace DocReview.Models.Api;

public record ErrorResponse
(
    int StatusCode,
    string Error,
    string Detail,
    DateTime Timestamp
)
{
    public static ErrorResponse NotFound(string detail) => new(404, "Not Found", detail, DateTime.UtcNow);
    public static ErrorResponse Forbidden(string detail) => new(403, "Forbidden", detail, DateTime.UtcNow);
    public static ErrorResponse Conflict(string detail) => new(409, "Conflict", detail, DateTime.UtcNow);
    public static ErrorResponse ServerError(string detail) => new(500, "Internal Server Error", detail, DateTime.UtcNow);
}