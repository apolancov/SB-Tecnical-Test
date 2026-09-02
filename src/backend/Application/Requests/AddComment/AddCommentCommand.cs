using Domain.Enums;

namespace Application.Requests.AddComment;

public sealed record AddCommentCommand(
    Guid RequestId,
    string Text,
    CommentVisibility Visibility);
