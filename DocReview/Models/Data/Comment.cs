namespace DocReview.DataModels;

public class Comment
{
    public Guid Id { get; set; }
    public Guid ReviewRequestId { get; set; }
    public ReviewRequest ReviewRequest { get; set; } = default!;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = default!;
    public string Text { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}