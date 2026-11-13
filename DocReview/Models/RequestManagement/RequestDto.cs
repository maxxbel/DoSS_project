using DocReview.DataModels;

namespace DocReview.Models.RequestManagement;

public record RequestDto
(
    Guid Id,
    Guid AuthorId,
    string AuthorUsername,
    string Title,
    string Description,
    string DocumentReference,
    RequestStatus Status,
    Guid? AssignedReviewerId,
    string? AssignedReviewerUsername,
    DateTime CreatedAt, 
    DateTime UpdatedAt,
    IReadOnlyList<DocumentVersionDto> Versions,
    IReadOnlyList<CommentDto> Comments
);