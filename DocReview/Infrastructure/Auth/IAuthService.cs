using DocReview.Models.Auth;

namespace DocReview.Infrastructure.Auth;

public interface IAuthService
{
    UserDto Register(RegisterRequest request);
    AuthResponse Login(LoginRequest request);
    UserDto GetCurrentUser(Guid userId);
}