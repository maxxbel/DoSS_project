namespace DocReview.DataModels;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string PasswordSalt { get; set; } = default!;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}