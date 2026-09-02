namespace Application.Requests;

public interface IRequestCodeGenerator
{
    Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken);
}
