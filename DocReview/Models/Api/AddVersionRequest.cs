namespace DocReview.Models.Api;

public sealed record AddVersionRequest(string FileReference, string FileName, long? FileSize, string? MediaType);
