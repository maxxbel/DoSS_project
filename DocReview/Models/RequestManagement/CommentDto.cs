namespace DocReview.Models.RequestManagement;

public record CommentDto(
    Guid Id,
    Guid ReviewRequestId,
    Guid AuthorId,
    string AuthorUsername,
    string Text,
    DateTime CreatedAt
);