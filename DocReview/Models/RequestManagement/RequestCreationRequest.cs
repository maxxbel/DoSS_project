namespace DocReview.Models.RequestManagement;

public record RequestCreationRequest(   
    string Title,
    string Description,
    string DocumentReference
);