namespace DocReview.Models.Api;

public sealed record AddCommentRequest(string Text, bool SendToRework);
