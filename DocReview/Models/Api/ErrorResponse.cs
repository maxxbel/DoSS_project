namespace DocReview.Models.Api;

public record ErrorResponse
(
    int StatusCode,
    string Error,
    string Detail,
    DateTime Timestamp
);