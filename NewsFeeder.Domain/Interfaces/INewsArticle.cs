namespace NewsFeeder.Domain
{
    using System;

    public interface INewsArticle
    {
        string Title { get; set; }
        string Link { get; set; }
        string Description { get; set; }
        DateTime PubDate { get; set; }
        string Comments { get; set; }
        string Guid { get; set; }
        string ImageSrc { get; set; }
    }
}
