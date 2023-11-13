namespace NewsFeeder.Repositories
{
    using Domain;
    using HtmlAgilityPack;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using DataTransferObjects.EmpireNews;
    using Newtonsoft.Json;
    using Microsoft.Extensions.Caching.Distributed;

    public class EmpireNewsRepository: INewsRepository
    {
        public string Title => "Empire Latest Movie News";
        public string SourceLink => "https://www.empireonline.com/movies/news/";
        public string Description => "The latest movie news from Empire magazine";
        private string _empireUrl = "https://www.empireonline.com";
        private readonly List<INewsArticle> _newsArticles = new List<INewsArticle>();
        private readonly int _desiredImageWidth = 300;
        private IDistributedCache _distributedCache;
        private readonly DistributedCacheEntryOptions cacheEntryOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = new TimeSpan(1, 30, 0) };

        public EmpireNewsRepository(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

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

        public IEnumerable<INewsArticle> NewsArticles()
        {
            HtmlWeb newsWeb = new HtmlWeb();

            HtmlDocument newsDocument = newsWeb.Load(SourceLink);

            IEnumerable<HtmlNode> cardNodes = newsDocument.DocumentNode.Descendants("div").Where(d => d.GetAttributeValue("class", "").StartsWith("card_"));

            foreach (HtmlNode cardNode in cardNodes)
            {
                AddNewsArticle(cardNode);
            }

            return _newsArticles;
        }

        private void AddNewsArticle(HtmlNode cardNode)
        {
            IEnumerable<HtmlNode> headerNodes = cardNode.Descendants("h3").ToList();
            IEnumerable<HtmlNode> linkNodes = cardNode.Descendants("a").ToList();
            IEnumerable<HtmlNode> paragraphNodes = cardNode.Descendants("p").ToList();
            IEnumerable<HtmlNode> imageNodes = cardNode.Descendants("img").ToList();
            IEnumerable<HtmlNode> timeNodes = cardNode.Descendants("time").ToList();

            NewsArticle article = new NewsArticle();

            if (headerNodes.Any())
            {
                article.Title = headerNodes.First().InnerText;
            }

            if (linkNodes.Any())
            {
                string linkHref = linkNodes.First().GetAttributeValue("href", string.Empty);
                article.Guid = linkHref;
                string link;
                if (!linkHref.StartsWith("http"))
                {
                    link = $"{_empireUrl}{linkHref}";
                }
                else
                {
                    link = linkHref;
                }

                article.Link = link;
                article.Description = GetArticleDescription(link);
            }

            if (article.Description == string.Empty && paragraphNodes.Any())
            {
                article.Description = paragraphNodes.First().InnerText;
            }

            if (imageNodes.Any())
            {
                string imgSrc = imageNodes.First().GetAttributeValue("data-src", string.Empty).Replace("width=750", "width=150");
                if (!imgSrc.StartsWith("http"))
                {
                    article.ImageSrc = $"http:{imgSrc}";
                }
                else
                {
                    article.ImageSrc = imgSrc;
                }
            }

            if (timeNodes.Any())
            {
                string dateTime = timeNodes.First().GetAttributeValue("dateTime", string.Empty);
                DateTime.TryParse(dateTime, CultureInfo.GetCultureInfo("en-GB").DateTimeFormat, DateTimeStyles.None,
                    out var pubDate);
                article.PublicationDate = pubDate.AddMinutes(-_newsArticles.Count);
            }

            _newsArticles.Add(article);
        }

        private void AddNodeArticles(string nodeText)
        {
            string nodeJson = nodeText.Substring(nodeText.IndexOf("{", StringComparison.Ordinal),
                nodeText.LastIndexOf("}", StringComparison.Ordinal) -
                nodeText.IndexOf("{", StringComparison.Ordinal) + 1);
            NewsItemChunk newsItemChunk = JsonConvert.DeserializeObject<NewsItemChunk>(nodeJson);

            foreach (Item item in newsItemChunk.Data.Items)
            {
                NewsArticle article = new NewsArticle
                {
                    Guid = item.Id,
                    Title = System.Web.HttpUtility.HtmlEncode(item.Title),
                    Link = $"{_empireUrl}{item.Url}",
                    Description = GetArticleDescription($"{_empireUrl}{item.Url}"),
                    PublicationDate = GetPublicationDate(item.Date),
                    ImageSrc = GetImageSource(item.Sources)
                };
                if (article.Description == string.Empty)
                {
                    article.Description = item.Description;
                }

                _newsArticles.Add(article);
            }
        }

        private string GetImageSource(List<Source> itemSources)
        {
            if (!itemSources.Any()) return string.Empty;

            string itemImageSource = itemSources.First().Src;
            int widthElementPosition = itemImageSource.LastIndexOf("&width=", StringComparison.OrdinalIgnoreCase);
            if (widthElementPosition >= 0)
            {
                itemImageSource = itemImageSource.Substring(0, widthElementPosition) + "&width=" + _desiredImageWidth;
            }
            return $"https:{itemImageSource}";
        }

        private DateTime GetPublicationDate(string itemDate)
        {
            string[] dateElements = itemDate.Split(' ');
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
                pubDate = pubDate.AddMinutes(-_newsArticles.Count);
            }

            return pubDate;
        }

        private string GetArticleDescription(string articleLink)
        {
            HtmlDocument articleDocument;
            string cachedDocument = _distributedCache.GetString(articleLink);
            if (!string.IsNullOrEmpty(cachedDocument))
            {
                articleDocument = new HtmlDocument();
                articleDocument.LoadHtml(cachedDocument);
            }
            else
            {
                HtmlWeb articleWeb = new HtmlWeb();
                articleDocument = articleWeb.Load(articleLink);
                _distributedCache.SetString(articleLink, articleDocument.DocumentNode.OuterHtml, cacheEntryOptions);
            }

            HtmlNode contentNode = articleDocument.DocumentNode.Descendants("article").FirstOrDefault();
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
