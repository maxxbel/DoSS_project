namespace DocReview.DataModels;

public class ReviewRequest
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string DocumentReference { get; set; } = default!;
    public RequestStatus Status { get; set; }
    public Guid? AssignedReviewerId { get; set; }
    public User? AssignedReviewer { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}