using DocReview.DataModels;
using DocReview.Infrastructure.Security;
using DocReview.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace DocReview.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly DatabaseContext _db;
    private readonly ITokenService _tokenService;

    public AuthService(DatabaseContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }
    
    public UserDto Register(RegisterRequest request)
    {
        if (_db.Users.Any(x => x.Username == request.Username))
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
            CreatedAt = DateTime.UtcNow
        };
        
        _db.Users.Add(user);
        _db.SaveChanges();

        return MapUser(user);
    }

    public AuthResponse Login(LoginRequest request)
    {
        var user = _db.Users.AsNoTracking().FirstOrDefault(u => u.Username == request.Username);

        if (user == null)
        {
            throw new InfrastructureNotFoundException("User", request.Username);
        }

        if (PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new InfrastructureForbiddenException("Invalid username or password.");
        }
        
        return new AuthResponse(_tokenService.GenerateToken(user), MapUser(user));
    }

    public UserDto GetCurrentUser(Guid userId)
    {
        var user = _db.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId);
        
        return user == null ? throw new InfrastructureNotFoundException("User", userId) : MapUser(user);
    }

    private static UserDto MapUser(User user)
    {
        return new UserDto(user.Id, user.Username, user.Role, user.CreatedAt);
    }
}