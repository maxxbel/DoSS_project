using DocReview.DataModels;
using DocReview.Infrastructure;
using DocReview.Infrastructure.Auth;
using DocReview.Infrastructure.UserManagement;
using DocReview.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocReview.Api.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService, IUserManagementService userManagementService) : ControllerBase
{
    private readonly IAuthService  _authService = authService;
    private readonly IUserManagementService  _userManagementService =  userManagementService;

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var response = await Task.FromResult(_authService.Login(req));
        return Ok(response);
    }
    
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest dto)
    {
        var isAuthenticated = User.Identity?.IsAuthenticated == true;
        var userRole = isAuthenticated ? User.GetUserRole() : null;

        if (!isAuthenticated)
        {
            if (dto.Role != UserRole.Admin || AdminAlreadyExists())
            {
                return Forbid();
            }
        }
        else if (userRole != UserRole.Admin)
        {
            return Forbid();
        }

        var user = await Task.FromResult(_userManagementService.CreateUser(dto));

        if (isAuthenticated && userRole != UserRole.Admin && dto.Role == UserRole.Admin)
        {
            return Forbid();
        }

        var tokenResponse = await Task.FromResult(_authService.Login(new LoginRequest
        {
            Username = dto.Username,
            Password = dto.Password
        }));
        return Ok(new AuthResponse(tokenResponse.Token, user));
    }

    private bool AdminAlreadyExists()
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        return db.Users.Any(u => u.Role == UserRole.Admin);
    }
}