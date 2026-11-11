using DocReview.Models.Auth;

namespace DocReview.Infrastructure.UserManagement;

public interface IUserManagementService
{
    UserDto CreateUser(RegisterRequest request);
}