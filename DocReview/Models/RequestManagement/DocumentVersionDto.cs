namespace DocReview.Models.RequestManagement;

public record DocumentVersionDto(
    Guid Id,
    Guid ReviewRequestId,
    string FileReference,
    string FileName,
    long? FileSize,
    string? MediaType,
    Guid UploadedById,
    string UploadedByUsername,
    DateTime UploadedAt
);