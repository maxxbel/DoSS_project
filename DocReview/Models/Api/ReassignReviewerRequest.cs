namespace DocReview.Models.Api;

public sealed record ReassignReviewerRequest(Guid? NewReviewerId);