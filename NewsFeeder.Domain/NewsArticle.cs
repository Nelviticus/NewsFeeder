using NewsFeeder.Repositories;
using System;

namespace NewsFeeder.Domain
{
    public class NewsArticle: INewsArticle
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public string PubDate { get; set; }
        public string Comments { get; set; }
        public string Guid { get; set; }

        public NewsArticle(INewsRepository newsRepository)
        {

        }
    }
}
