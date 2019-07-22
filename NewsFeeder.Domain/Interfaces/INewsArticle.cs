namespace NewsFeeder.Domain
{
    using System;

    public interface INewsArticle
    {
        string Title { get; set; }
        string Link { get; set; }
        string Description { get; set; }
        string PubDate { get; }
        DateTime PublicationDate { set; }
        string Comments { get; set; }
        string Guid { get; set; }
        string ImageSrc { set; }
    }
}
