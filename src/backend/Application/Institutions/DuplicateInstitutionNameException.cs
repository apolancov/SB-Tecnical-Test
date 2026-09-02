namespace Application.Institutions;

public sealed class DuplicateInstitutionNameException : Exception
{
    public string ConflictingName { get; }

    public DuplicateInstitutionNameException(string conflictingName)
        : base($"An institution with name '{conflictingName}' already exists.")
    {
        ConflictingName = conflictingName;
    }
}
