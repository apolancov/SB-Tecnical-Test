using Domain.Entities;

namespace Application.Common.Authentication;

public interface IJwtTokenIssuer
{
    AuthenticationTokenIssue IssueFor(User user);
}
