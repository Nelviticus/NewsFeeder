namespace NewsFeeder.Repositories
{
    using Domain;
    using HtmlAgilityPack;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;

    public class Source
    {
        public string altText { get; set; }
        public string src { get; set; }
        public int media { get; set; }
    }

    public class Category
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Item
    {
        public string id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string date { get; set; }
        public object rating { get; set; }
        public List<Source> sources { get; set; }
        public string icon { get; set; }
        public string url { get; set; }
        public Category category { get; set; }
    }

    public class Data
    {
        public List<Item> items { get; set; }
        public string type { get; set; }
        public string componentId { get; set; }
    }

    public class NewsItemChunk
    {
        public string id { get; set; }
        public string name { get; set; }
        public Data data { get; set; }
    }

    public class EmpireNewsRepository: INewsRepository
    {
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

                try
                {
                    string nodeJson = nodeText.Substring(nodeText.IndexOf("{", StringComparison.Ordinal),
                        nodeText.LastIndexOf("}", StringComparison.Ordinal) -
                        nodeText.IndexOf("{", StringComparison.Ordinal) + 1);
                    NewsItemChunk newsItemChunk = JsonConvert.DeserializeObject<NewsItemChunk>(nodeJson);

                    foreach (Item item in newsItemChunk.data.items)
                    {
                        NewsArticle article = new NewsArticle();
                        article.Title = item.title;
                        article.Link = $"{_empireUrl}{item.url}";
                        article.Description = GetArticleDescription($"{_empireUrl}{item.url}");
                        string[] dateElements = item.date.Split(' ');
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
                        article.Guid = item.id;
                        string imgSrc = item.sources.First().src.Replace("width=750", "width=150");
                        article.ImageSrc = $"https:{imgSrc}";

                        _newsArticles.Add(article);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    continue;
                }
            }

            return _newsArticles;

            HtmlNodeCollection cardNodeList = newsDocument.DocumentNode.SelectNodes("//div[contains(concat(' ', normalize-space(@class), ' '), ' card ')]");

            if (cardNodeList == null)
            {
                return _newsArticles;
            }

            foreach (HtmlNode cardNode in cardNodeList)
            {
                AddNewsArticle(cardNode);
            }

            return _newsArticles;
        }

        private void AddNewsArticle(HtmlNode cardNode)
        {
            IEnumerable<HtmlNode> headerNodes = cardNode.Descendants("h3").ToList();
            IEnumerable<HtmlNode> paragraphNodes = cardNode.Descendants("p").ToList();
            IEnumerable<HtmlNode> linkNodes = cardNode.Descendants("a").ToList();
            IEnumerable<HtmlNode> imageNodes = cardNode.Descendants("img").ToList();
            IEnumerable<HtmlNode> spanNodes = cardNode.Descendants("span").ToList();

            NewsArticle article = new NewsArticle();

            if (headerNodes.Any())
            {
                article.Title = headerNodes.First().InnerText;
            }

            if (linkNodes.Any())
            {
                string linkHref = linkNodes.First().GetAttributeValue("href", string.Empty);
                article.Guid = linkHref;
                if (!linkHref.StartsWith("http"))
                {
                    article.Link = $"{_empireUrl}{linkHref}";
                }
                else
                {
                    article.Link = linkHref;
                }

                article.Description = GetArticleDescription(linkHref.StartsWith("http") ? linkHref : $"{_empireUrl}{linkHref}");
            }

            if (article.Description == string.Empty && paragraphNodes.Any())
            {
                article.Description = paragraphNodes.First().InnerText;
            }

            if (imageNodes.Any())
            {
                string imgSrc = imageNodes.First().GetAttributeValue("src", string.Empty).Replace("width=750", "width=150");
                if (!imgSrc.StartsWith("http"))
                {
                    article.ImageSrc = $"http:{imgSrc}";
                }
                else
                {
                    article.ImageSrc = imgSrc;
                }
            }

            if (spanNodes.Any())
            {
                string[] dateElements = spanNodes.Last().InnerText.Split(' ');
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
            }

            _newsArticles.Add(article);
        }

        private string GetArticleDescription(string articleLink)
        {
            HtmlWeb articleWeb = new HtmlWeb();
            HtmlDocument articleDocument = articleWeb.Load(articleLink);
            HtmlNode contentNode = articleDocument.DocumentNode.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' article__content ')]");
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
