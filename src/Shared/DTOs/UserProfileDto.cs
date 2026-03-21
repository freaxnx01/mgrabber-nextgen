namespace MusicGrabber.Shared.DTOs;

public sealed record UserProfileDto(
    string UserId,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt);
