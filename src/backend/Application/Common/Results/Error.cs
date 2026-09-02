namespace Application.Common.Results;

public sealed record Error(ErrorKind Kind, string Message, string Code)
{
    public static Error Validation(string message, string code) =>
        new(ErrorKind.Validation, message, code);

    public static Error NotFound(string message, string code) =>
        new(ErrorKind.NotFound, message, code);

    public static Error Conflict(string message, string code) =>
        new(ErrorKind.Conflict, message, code);

    public static Error Unauthorized(string message, string code) =>
        new(ErrorKind.Unauthorized, message, code);

    public static Error Forbidden(string message, string code) =>
        new(ErrorKind.Forbidden, message, code);

    public static Error Unexpected(string message, string code) =>
        new(ErrorKind.Unexpected, message, code);
}