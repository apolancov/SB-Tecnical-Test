namespace Application.Users;

public sealed class DuplicateUserException : Exception
{
    public DuplicateUserException(string field, string value)
        : base($"A user with the same {field} '{value}' already exists.")
    {
        Field = field;
        Value = value;
    }

    public string Field { get; }

    public string Value { get; }
}
