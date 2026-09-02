namespace Application.Requests.Lookups;

public sealed record StaffCandidateDto(
    Guid Id,
    string Username,
    string Email,
    string Role);