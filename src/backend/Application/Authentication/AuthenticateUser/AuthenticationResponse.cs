namespace Application.Authentication.AuthenticateUser;

public sealed record AuthenticationResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    AuthenticatedUserDto User);
