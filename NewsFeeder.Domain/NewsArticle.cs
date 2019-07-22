namespace NewsFeeder.Domain
{
    using System;

    public class NewsArticle: INewsArticle
    {
        private string _title;
        public string Title
        {
            get => $"<title>{_title}</Title>";
            set => _title = value;
        }

        public string Link { get; set; }
        public string Description { get; set; }
        public DateTime PubDate { get; set; }
        public string Comments { get; set; }
        public string Guid { get; set; }
        public string ImageSrc { get; set; }
    }
}
