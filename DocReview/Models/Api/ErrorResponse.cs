namespace DocReview.Models.Api;

public record ErrorResponse
(
    int StatusCode,
    string ErrorMessage,
    string ErrorDetail,
    DateTime TimeStamp
);