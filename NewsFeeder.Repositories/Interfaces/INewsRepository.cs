namespace NewsFeeder.Repositories
{
    using Domain;
    using System.Collections.Generic;

    public interface INewsRepository
    {
        string Title { get; }
        string SourceLink { get; }
        string Description { get; }

        IEnumerable<INewsArticle> NewsArticles();
    }
}
