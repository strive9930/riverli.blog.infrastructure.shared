using System;

namespace RiverLi.Blog.Infrastructure.Shared.Events
{
    public class ArticleCreatedIntegrationEvent
    {
        public Guid ArticleId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Summary { get; set; }
        public string AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }

        public ArticleCreatedIntegrationEvent()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public ArticleCreatedIntegrationEvent(Guid articleId, string title, string content,
            string summary, string authorName)
        {
            ArticleId = articleId;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            AuthorName = authorName ?? throw new ArgumentNullException(nameof(authorName));
            CreatedAt = DateTime.UtcNow;
        }
    }
}