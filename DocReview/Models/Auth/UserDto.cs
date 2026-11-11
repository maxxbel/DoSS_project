using DocReview.DataModels;

namespace DocReview.Models.Auth;

public record UserDto(
    Guid Id, 
    string Username, 
    UserRole Role, 
    DateTime CreatedAt
);