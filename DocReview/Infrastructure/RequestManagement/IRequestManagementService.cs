using DocReview.Models.RequestManagement;

namespace DocReview.Infrastructure.RequestManagement;

public interface IRequestManagementService
{
   RequestDto Create(Guid authorId, RequestCreationRequest request);
   RequestDto GetById(Guid requestId, Guid viewerId);
   IReadOnlyList<RequestDto> ListByAuthor(Guid authorId);
   IReadOnlyList<RequestDto> ListAvailableToReviewer(Guid reviewerId);
   IReadOnlyList<RequestDto> ListAssignedToReviewer(Guid reviewerId);
   void TakeIntoWork(Guid requestId, Guid viewerId);
   void AddComment(Guid requestId, Guid viewerId, string comment, bool sendToRework);
   void Approve(Guid requestId, Guid reviewerId);
   void AddVersion(Guid requestId, Guid authorId, string fileReference, string fileName, long? fileSize,
      string? mediaType);
   void ReassignReviewer(Guid requestId, Guid adminId, Guid? newReviewerId);
   void Delete(Guid requestId, Guid adminId);
}