using DocReview.Api.Auth;
using DocReview.DataModels;
using DocReview.Infrastructure;
using DocReview.Infrastructure.Auth;
using DocReview.Infrastructure.RequestManagement;
using DocReview.Models.Api;
using DocReview.Models.Auth;
using DocReview.Models.RequestManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocReview.Api.Requests;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequestsController(IRequestManagementService requestManagementService, IAuthService authService): ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly IRequestManagementService _requestManagementService = requestManagementService;

    [HttpPost]
    [Authorize(Roles = "Author,Admin")]
    public async Task<ActionResult<RequestCreationResponse>> Create(RequestCreationRequest request)
    {
        var userId = RequireUserId();
        var user = _authService.GetCurrentUser(userId);

        if (user.Role is not (UserRole.Admin or UserRole.Author))
        {
            return Forbid();
        }

        var created = _requestManagementService.Create(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new RequestCreationResponse(created));
    }

    [HttpGet]
    [Authorize(Roles = "Author,Admin")]
    public async Task<ActionResult<IReadOnlyList<RequestDto>>> ListMyRequests()
    {
        var userId = RequireUserId();
        var user = _authService.GetCurrentUser(userId);

        if (user.Role is not (UserRole.Admin or UserRole.Author))
        {
            return Forbid();
        }

        return Ok(_requestManagementService.ListByAuthor(userId));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<RequestDto>> GetById(Guid id)
    {
        var userId = RequireUserId();
        var result = _requestManagementService.GetById(id, userId);
        return Ok(result);
    }

    [HttpGet("available")]
    [Authorize(Roles = "Reviewer,Admin")]
    public async Task<ActionResult<IReadOnlyList<RequestDto>>> Available()
    {
        var userId = RequireUserId();
        var user = _authService.GetCurrentUser(userId);

        if (user.Role is not (UserRole.Reviewer or UserRole.Admin))
        {
            return Forbid();
        }

        return Ok(_requestManagementService.ListAvailableToReviewer(userId));
    }

    [HttpGet("assigned")]
    [Authorize(Roles = "Reviewer,Admin")]
    public async Task<ActionResult<IReadOnlyList<RequestDto>>> Assigned()
    {
        var userId = RequireUserId();
        var user = _authService.GetCurrentUser(userId);

        if (user.Role is not (UserRole.Reviewer or UserRole.Admin))
        {
            return Forbid();
        }

        return Ok(_requestManagementService.ListAssignedToReviewer(userId));
    }

    [HttpPost("{id:guid}/take")]
    [Authorize(Roles = "Reviewer,Admin")]
    public async Task<IActionResult> TakeIntoWork(Guid id)
    {
        var userId = RequireUserId();
        var user = _authService.GetCurrentUser(userId);

        if (user.Role is not (UserRole.Reviewer or UserRole.Admin))
        {
            return Forbid();
        }

        _requestManagementService.TakeIntoWork(id, userId);
        return Ok();
    }

    [HttpPost("{id:guid}/comment")]
    [Authorize(Roles = "Author,Admin,Reviewer")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest request)
    {
        var userId = RequireUserId();

        _requestManagementService.AddComment(id, userId, request.Text, request.SendToRework);
        return Ok();
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Reviewer,Admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var userId = RequireUserId();
        var user = _authService.GetCurrentUser(userId);

        if (user.Role is not (UserRole.Reviewer or UserRole.Admin))
        {
            return Forbid();
        }

        _requestManagementService.Approve(id, userId);
        return Ok();
    }

    [HttpPost("{id:guid}/version")]
    [Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> AddVersion(Guid id, [FromBody] AddVersionRequest request)
    {
        var userId = RequireUserId();
        var user = _authService.GetCurrentUser(userId);

        if (user.Role is not (UserRole.Author or UserRole.Admin))
        {
            return Forbid();
        }

        _requestManagementService.AddVersion(id, userId, request.FileReference, request.FileName, request.FileSize, request.MediaType);
        return Ok();
    }

    private Guid RequireUserId()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new InfrastructureForbiddenException("Authenticated user id is missing.");
        }

        return userId.Value;
    }
}