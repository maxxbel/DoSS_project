namespace DocReview.DataModels;

public class DocumentVersion
{
    public Guid Id { get; set; }
    public Guid ReviewRequestId { get; set; }
    public ReviewRequest ReviewRequest { get; set; } = default!;
    public string FileReference { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public long? FileSize { get; set; }
    public string? MediaType { get; set; }
    public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
}