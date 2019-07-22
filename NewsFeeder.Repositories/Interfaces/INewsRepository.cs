namespace NewsFeeder.Repositories
{
    using Domain;
    using System.Collections.Generic;

    public interface INewsRepository
    {
        string Title { get; }
        string SourceLink { get; }
        string Description { get; }
        string Body { get; }

        IEnumerable<INewsArticle> NewsArticles();
    }
}
