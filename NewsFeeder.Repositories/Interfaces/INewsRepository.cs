namespace NewsFeeder.Repositories.Interfaces
{
    using System.Collections.Generic;
    using Domain;
    using Domain.Interfaces;

    public interface INewsRepository
    {
        string Title { get; }
        string SourceLink { get; }
        string Description { get; }
        string Body { get; }

        IEnumerable<INewsArticle> NewsArticles();
    }
}
