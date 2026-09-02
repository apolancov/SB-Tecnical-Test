namespace Application.Users.ChangeUserPassword;

public sealed record ChangeUserPasswordCommand(
    Guid Id,
    string NewPassword);
