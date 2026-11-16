using DocReview.DataModels;
using DocReview.Infrastructure.Security;
using DocReview.Models.Auth;

namespace DocReview.Infrastructure.UserManagement;

public class UserManagementService(DatabaseContext db) : IUserManagementService
{
    private readonly DatabaseContext _db = db;

    public UserDto CreateUser(RegisterRequest request)
    {
        if (_db.Users.Any(u => u.Username == request.Username))
        {
            throw new InfrastructureConflictException($"User with username '{request.Username}' already exists.");
        }
        
        var (hash, salt) = PasswordHasher.Hash(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow,
        };
        
        _db.Users.Add(user);
        _db.SaveChanges();
        
        return new UserDto(user.Id, user.Username, user.Role, user.CreatedAt);
    }
}