using DocReview.Infrastructure;
using DocReview.Infrastructure.Auth;
using DocReview.Infrastructure.RequestManagement;
using DocReview.Infrastructure.UserManagement;
using DocReview.Models.Api;
using DocReview.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocReview.Api.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IUserManagementService userManagementService, IRequestManagementService requestManagementService): ControllerBase
{
        private readonly IRequestManagementService _requestManagementService = requestManagementService;
        private readonly IUserManagementService _userManagementService = userManagementService;

        [HttpPost("users")]
        public async Task<ActionResult<UserDto>> CreateUser(RegisterRequest request)
        {
                var user = await Task.FromResult(_userManagementService.CreateUser(request));
                return Ok(user);
        }

        [HttpPost("requests/{id:guid}/reassign")]
        public IActionResult ReassignReviewer(Guid id, [FromBody] ReassignReviewerRequest request)
        {
                var adminId = RequireUserId();
                _requestManagementService.ReassignReviewer(id, adminId, request.NewReviewerId);
                return Ok();
        }

        [HttpDelete("requests/{id:guid}")]
        public async Task<IActionResult> DeleteRequest(Guid id)
        {
                var adminId = RequireUserId();
                _requestManagementService.Delete(id, adminId);
                return NoContent();
        }
        
        private Guid RequireUserId()
        {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId is null)
                {
                        throw new InfrastructureForbiddenException("Admin user id is missing.");
                }

                return Guid.Parse(userId);
        }
}