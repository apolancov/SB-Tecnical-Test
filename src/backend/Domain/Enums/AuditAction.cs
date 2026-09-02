namespace Domain.Enums;

public enum AuditAction
{
    LoginSucceeded,
    LoginFailed,
    RequestCreated,
    RequestUpdated,
    RequestAssigned,
    RequestStatusChanged,
    RequestReopened,
    RequestCommentAdded,
    InstitutionCreated,
    InstitutionUpdated,
    InstitutionDeleted,
    UserCreated,
    UserUpdated,
    UserDeactivated,
    UserPasswordChanged,
    AuthorizationDenied
}