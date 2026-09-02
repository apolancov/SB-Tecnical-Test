using Domain.Enums;

namespace Domain.Entities;

public class RequestComment
{
    public const int TextMaximumLength = 4096;

    public Guid Id { get; private set; }

    public Guid RequestId { get; private set; }

    public Guid AuthorId { get; private set; }

    public User Author { get; private set; }

    public string Text { get; private set; }

    public CommentVisibility Visibility { get; private set; }

    public DateTime Date { get; private set; }

    private RequestComment()
    {
        Text = string.Empty;
        Author = null!;
    }

    public RequestComment(User author, string text, CommentVisibility visibility, DateTime date)
    {
        if (author is null)
        {
            throw new ArgumentNullException(nameof(author), "Comment author cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Comment text cannot be null or empty.", nameof(text));
        }

        var normalizedText = text.Trim();
        if (normalizedText.Length > TextMaximumLength)
        {
            throw new ArgumentException(
                $"Comment text cannot exceed {TextMaximumLength} characters.",
                nameof(text));
        }

        Id = Guid.NewGuid();
        AuthorId = author.Id;
        Author = author;
        Text = normalizedText;
        Visibility = visibility;
        Date = date;
    }
}
