using DocReview.DataModels;

namespace DocReview.Infrastructure.Security;

public interface ITokenService
{
    string GenerateToken(User user);
}