using DocReview.DataModels;
using DocReview.Models.RequestManagement;
using Microsoft.EntityFrameworkCore;

namespace DocReview.Infrastructure.RequestManagement;

public class RequestManagementService(DatabaseContext db): IRequestManagementService
{
    private readonly DatabaseContext _db = db;
    
    public RequestDto Create(Guid authorId, RequestCreationRequest request)
    {
        var reviewRequest = new ReviewRequest
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Title = request.Title,
            Description = request.Description,
            DocumentReference = request.DocumentReference,
            Status = RequestStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _db.ReviewRequests.Add(reviewRequest);
        _db.SaveChanges();

        return MapReviewRequest(reviewRequest);
    }

    public RequestDto GetById(Guid requestId, Guid viewerId)
    {
        var reviewRequest = _db.ReviewRequests
            .Include(r => r.Author)
            .Include(r => r.AssignedReviewer)
            .Include(r => r.Versions).ThenInclude(v => v.UploadedBy)
            .Include(r => r.Comments).ThenInclude(c => c.Author)
            .FirstOrDefault(r => r.Id == requestId);

        if (reviewRequest == null)
        {
            throw new InfrastructureNotFoundException("Request", requestId);
        }

        if (reviewRequest.AssignedReviewerId != viewerId && reviewRequest.AuthorId != viewerId)
        {
            var viewer = GetUser(viewerId);
            if (viewer.Role != UserRole.Admin)
            {
                throw new InfrastructureForbiddenException("You don't have permission to view this request");
            }
        }
        
        return MapReviewRequest(reviewRequest);
    }

    public IReadOnlyList<RequestDto> ListByAuthor(Guid authorId)
    {
      var author = GetUser(authorId);
      if (author.Role != UserRole.Author && author.Role != UserRole.Admin)
      {
          throw new InfrastructureForbiddenException("Only authors and admins can list requests by author.");
      }        return
      [
          .. _db.ReviewRequests
              .AsNoTracking()
              .Include(r => r.Author)
              .Include(r => r.Versions).ThenInclude(v => v.UploadedBy)
              .Include(r => r.Comments).ThenInclude(c => c.Author)
              .Where(r => r.AuthorId == authorId)
              .OrderByDescending(r => r.UpdatedAt)
              .Select(MapReviewRequest)
      ];
    }

    public IReadOnlyList<RequestDto> ListAvailableToReviewer(Guid reviewerId)
    {
        var reviewer = GetUser(reviewerId);

        if (reviewer.Role != UserRole.Admin && reviewer.Role != UserRole.Reviewer)
        {
            throw new InfrastructureForbiddenException("Only reviewers and admins can list available requests.");
        }
        
        return
        [
            .. _db.ReviewRequests
                .AsNoTracking()
                .Include(r => r.Author)
                .Include(r => r.Versions).ThenInclude(v => v.UploadedBy)
                .Include(r => r.Comments).ThenInclude(c => c.Author)
                .Where(r => r.Status == RequestStatus.Submitted &&
                            (r.AssignedReviewerId == null || r.AssignedReviewerId == reviewerId))
                .OrderByDescending(r => r.CreatedAt)
                .Select(MapReviewRequest)
        ];
    }

    public IReadOnlyList<RequestDto> ListAssignedToReviewer(Guid reviewerId)
    {
        var reviewer = GetUser(reviewerId);

        if (reviewer.Role != UserRole.Admin && reviewer.Role != UserRole.Reviewer)
        {
            throw new InfrastructureForbiddenException("Only reviewers and admins can list assigned requests.");
        }
        
        return
        [
            .. _db.ReviewRequests
                .AsNoTracking()
                .Include(r => r.Author)
                .Include(r => r.Versions).ThenInclude(v => v.UploadedBy)
                .Include(r => r.Comments).ThenInclude(c => c.Author)
                .Where(r => r.Status == RequestStatus.Submitted &&
                             r.AssignedReviewerId == reviewerId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(MapReviewRequest)
        ];
    }

    public void TakeIntoWork(Guid requestId, Guid reviewerId)
    {
        var reviewer = GetUser(reviewerId);

        if (reviewer.Role != UserRole.Admin && reviewer.Role != UserRole.Reviewer)
        {
            throw new InfrastructureForbiddenException("Only reviewers and admins can take requests into work.");
        }

        var request = GetReviewRequest(requestId);

        if (request.Status != RequestStatus.Submitted)
        {
            throw new InfrastructureConflictException(
                $"Request is not in a state that allows taking into work (current status: {request.Status}).");
        }

        if (request.AssignedReviewerId != null && request.AssignedReviewerId != reviewerId &&
            reviewer.Role != UserRole.Admin)
        {
            throw new InfrastructureForbiddenException("This request is already assigned to another reviewer");
        }

        request.AssignedReviewerId = reviewerId;
        request.Status = RequestStatus.InReview;
        request.UpdatedAt = DateTime.UtcNow;
        
        _db.ReviewRequests.Update(request);
        _db.SaveChanges();
    }

    public void AddComment(Guid requestId, Guid viewerId, string commentText, bool sendToRework)
    {
        var reviewer = GetUser(viewerId); 
        var request = GetReviewRequest(requestId);

        if (request.AssignedReviewerId == null || request.AssignedReviewerId != reviewer.Id)
        {
            if (reviewer.Role != UserRole.Admin)
            {
                throw new InfrastructureForbiddenException("You can comment requests that are related to you.");
            }
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            ReviewRequestId = requestId,
            AuthorId = reviewer.Id,
            Text = commentText,
            CreatedAt = DateTime.UtcNow,
        };
        
        _db.Comments.Add(comment);

        if (sendToRework)
        {
            request.Status = RequestStatus.InRework;
        }
        
        request.UpdatedAt = DateTime.UtcNow;
        _db.ReviewRequests.Update(request);
        _db.SaveChanges();
    }

    public void Approve(Guid requestId, Guid reviewerId)
    {
        var reviewer = GetUser(reviewerId);

        if (reviewer.Role != UserRole.Admin && reviewer.Role != UserRole.Reviewer)
        {
            throw new InfrastructureForbiddenException("Only reviewers and admins can approve requests.");
        } 
        var request = GetReviewRequest(requestId);

        if (request.AssignedReviewerId == null || request.AssignedReviewerId != reviewer.Id)
        {
            if (reviewer.Role != UserRole.Admin)
            {
                throw new InfrastructureForbiddenException("You can approve requests that are related to you.");
            }
        }
        
        request.Status = RequestStatus.Approved;
        request.UpdatedAt = DateTime.UtcNow;
        _db.ReviewRequests.Update(request);
        _db.SaveChanges();
    }

    public void AddVersion(Guid requestId, Guid authorId, string fileReference, string fileName, long? fileSize,
        string? mediaType)
    {
        var author =  GetUser(authorId);

        if (author.Role != UserRole.Admin && author.Role != UserRole.Author)
        {
            throw new InfrastructureForbiddenException("Only authors and admins can add versions.");
        }
        
        var request = GetReviewRequest(requestId);

        if (request.AuthorId != author.Id)
        {
            throw new InfrastructureForbiddenException("You can add versions to your own requests.");
        }

        if (request.Status != RequestStatus.InRework)
        {
            throw new InfrastructureConflictException($"Request is not in a state that allows new versions (current state: {request.Status}).");
        }

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            ReviewRequestId = requestId,
            FileReference = fileReference,
            FileName = fileName,
            FileSize = fileSize,
            MediaType = mediaType,
            UploadedById = author.Id,
            UploadedBy = author,
            UploadedAt = DateTime.UtcNow,
        };
        
        _db.DocumentVersions.Add(version);
        request.Status = RequestStatus.Submitted;
        request.UpdatedAt = DateTime.UtcNow;
        _db.ReviewRequests.Update(request);
        _db.SaveChanges();
    }

    public void ReassignReviewer(Guid requestId, Guid adminId, Guid? newReviewerId)
    {
        var admin = GetUser(adminId);
        if (admin.Role != UserRole.Admin)
        {
            throw new InfrastructureForbiddenException("Only admins can reassign reviewers.");
        }

        var request = GetReviewRequest(requestId);

        if (newReviewerId != null)
        {
            var newReviewer = GetUser(newReviewerId.Value);
            if (newReviewer.Role != UserRole.Reviewer)
            {
                throw new InfrastructureConflictException("New reviewer does not have Reviewer role");
            }
        }
        
        request.AssignedReviewerId = newReviewerId;
        request.UpdatedAt = DateTime.UtcNow;
        _db.ReviewRequests.Update(request);
        _db.SaveChanges();
    }

    public void Delete(Guid requestId, Guid adminId)
    {
        var admin = GetUser(adminId);
        if (admin.Role != UserRole.Admin)
        {
            throw new InfrastructureForbiddenException("Only admins can delete requests.");
        }
        
        var request = GetReviewRequest(requestId);
        _db.ReviewRequests.Remove(request);
        _db.SaveChanges();
    }

    private static RequestDto MapReviewRequest(ReviewRequest reviewRequest)
    {
        return new RequestDto(
            reviewRequest.Id,
            reviewRequest.AuthorId,
            reviewRequest.Author.Username,
            reviewRequest.Title,
            reviewRequest.Description,
            reviewRequest.DocumentReference,
            reviewRequest.Status,
            reviewRequest.AssignedReviewerId,
            reviewRequest.AssignedReviewer?.Username,
            reviewRequest.CreatedAt,
            reviewRequest.UpdatedAt,
            reviewRequest.Versions
                .OrderByDescending(v => v.UploadedAt)
                .Select(v => new DocumentVersionDto(
                    v.Id,
                    v.ReviewRequestId,
                    v.FileReference,
                    v.FileName,
                    v.FileSize,
                    v.MediaType,
                    v.UploadedById,
                    v.UploadedBy.Username,
                    v.UploadedAt))
                .ToList(),
            reviewRequest.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto(
                    c.Id,
                    c.ReviewRequestId,
                    c.AuthorId,
                    c.Author.Username,
                    c.Text,
                    c.CreatedAt))
                .ToList()
        );
    }
    
    
    private User GetUser(Guid userId)
    {
        var user = _db.Users.Find(userId);
        return user ?? throw new InfrastructureNotFoundException("User", userId);
    }

    private ReviewRequest GetReviewRequest(Guid reviewRequestId)
    {
        var request = _db.ReviewRequests.Find(reviewRequestId);
        return request ?? throw new InfrastructureNotFoundException("ReviewRequest", reviewRequestId);
    }
}