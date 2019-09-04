namespace NewsFeeder.Repositories
{
    using Domain;
    using HtmlAgilityPack;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;

    public class JsonArticle
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public string Rating { get; set; }
        public string Sources { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }
        public string Category { get; set; }
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
                if (!nodeText.Contains("window.bootstrapComponents.push"))
                    continue;

                for (int i = nodeText.IndexOf('{'); i > -1; i = nodeText.IndexOf('{', i + 1))
                {
                    for (int j = nodeText.LastIndexOf('}'); j > -1; j = nodeText.LastIndexOf("}", j - 1, StringComparison.Ordinal))
                    {
                        string jsonProbe = nodeText.Substring(i, j - i + 1);
                        try
                        {
                            JsonArticle jsonArticle = JsonConvert.DeserializeObject<JsonArticle>(jsonProbe);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            continue;
                        }
                    }
                }
            }

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
