namespace NewsFeeder.Repositories
{
    using Domain;
    using HtmlAgilityPack;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;

    public class EmpireNewsRepository: INewsRepository
    {
        private struct Source
        {
            public string AltText { get; set; }
            public string Src { get; set; }
            public int Media { get; set; }
        }

        private struct Category
        {
            public string Name { get; set; }
            public string Url { get; set; }
        }

        private struct Item
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Date { get; set; }
            public object Rating { get; set; }
            public List<Source> Sources { get; set; }
            public string Icon { get; set; }
            public string Url { get; set; }
            public Category Category { get; set; }
        }

        private struct Data
        {
            public List<Item> Items { get; set; }
            public string Type { get; set; }
            public string ComponentId { get; set; }
        }

        private struct NewsItemChunk
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public Data Data { get; set; }
        }

        public string Title => "Empire Latest Movie News";
        public string SourceLink => "https://www.empireonline.com/movies/news/";
        public string Description => "The latest movie news from Empire magazine";

        public string Body
        {
            get
            {
                StringBuilder feedBuilder = new StringBuilder();
                feedBuilder.Append("<lastBuildDate>");
                feedBuilder.Append(DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss K"));
                feedBuilder.Append("</lastBuildDate>");

                foreach (INewsArticle newsArticle in NewsArticles())
                {
                    feedBuilder.Append(newsArticle);
                }

                return feedBuilder.ToString();
            }
        }

        private string _empireUrl = "https://www.empireonline.com";
        private readonly List<INewsArticle> _newsArticles = new List<INewsArticle>();

        public IEnumerable<INewsArticle> NewsArticles()
        {
            HtmlWeb newsWeb = new HtmlWeb();

            HtmlDocument newsDocument = newsWeb.Load(SourceLink);

            HtmlNodeCollection scriptNodeList = newsDocument.DocumentNode.SelectNodes("//script");

            if (scriptNodeList == null)
                return _newsArticles;

            foreach (HtmlNode scriptNode in scriptNodeList)
            {
                string nodeText = scriptNode.InnerText;
                if (!nodeText.Contains("window.bootstrapComponents.push") 
                    || !nodeText.Contains("cards") 
                    || !nodeText.Contains('{') 
                    || !nodeText.Contains('}'))
                    continue;

                AddNodeArticles(nodeText);
            }

            return _newsArticles;
        }

        private void AddNodeArticles(string nodeText)
        {
            string nodeJson = nodeText.Substring(nodeText.IndexOf("{", StringComparison.Ordinal),
                nodeText.LastIndexOf("}", StringComparison.Ordinal) -
                nodeText.IndexOf("{", StringComparison.Ordinal) + 1);
            NewsItemChunk newsItemChunk = JsonConvert.DeserializeObject<NewsItemChunk>(nodeJson);

            foreach (Item item in newsItemChunk.Data.Items)
            {
                NewsArticle article = new NewsArticle();
                article.Title = System.Web.HttpUtility.HtmlEncode(item.Title);
                article.Link = $"{_empireUrl}{item.Url}";
                article.Description = GetArticleDescription($"{_empireUrl}{item.Url}");
                if (article.Description == string.Empty)
                {
                    article.Description = item.Description;
                }

                string[] dateElements = item.Date.Split(' ');
                DateTime pubDate = DateTime.UtcNow;
                try
                {
                    switch (dateElements[1])
                    {
                        case "hour":
                            pubDate = pubDate.AddHours(-1);
                            break;
                        case "hours":
                            pubDate = pubDate.AddHours(0d - double.Parse(dateElements[0]));
                            break;
                        case "day":
                            pubDate = pubDate.AddDays(-1);
                            break;
                        case "days":
                            pubDate = pubDate.AddDays(0d - double.Parse(dateElements[0]));
                            break;
                    }
                }
                finally
                {
                    article.PublicationDate = pubDate.AddMinutes(-_newsArticles.Count);
                }

                article.Guid = item.Id;
                article.ImageSrc = $"https:{item.Sources.Last().Src}";

                _newsArticles.Add(article);
            }
        }

        private string GetArticleDescription(string articleLink)
        {
            HtmlWeb articleWeb = new HtmlWeb();
            HtmlDocument articleDocument = articleWeb.Load(articleLink);
            HtmlNode contentNode = articleDocument.DocumentNode.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' article__content ')]");
            if (contentNode == null)
                return string.Empty;

            IEnumerable<HtmlNode> paragraphNodes = contentNode.Descendants("p");
            StringBuilder descriptionBuilder = new StringBuilder();
            foreach (HtmlNode paragraphNode in paragraphNodes)
            {
                descriptionBuilder.Append(paragraphNode.OuterHtml);
            }

            return System.Web.HttpUtility.HtmlEncode(descriptionBuilder.ToString());
        }
    }
}
