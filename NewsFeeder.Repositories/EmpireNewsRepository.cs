namespace NewsFeeder.Repositories
{
    using DataTransferObjects.EmpireNews;
    using Domain;
    using HtmlAgilityPack;
    using Microsoft.Extensions.Caching.Distributed;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Domain.Interfaces;
    using Interfaces;

    public class EmpireNewsRepository: INewsRepository
    {
        public string Title => "Empire Latest Movie News";
        public string SourceLink => "https://www.empireonline.com/movies/news/";
        public string Description => "The latest movie news from Empire magazine";

        private const string BaseUrl = "https://www.empireonline.com";
        private const int DesiredImageWidth = 300;
        private readonly List<INewsArticle> _newsArticles = new List<INewsArticle>();
        private readonly IDistributedCache _distributedCache;

        private readonly DistributedCacheEntryOptions _cacheEntryOptions = new DistributedCacheEntryOptions
            {SlidingExpiration = TimeSpan.FromMinutes(90)};

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
            NewsItemChunk newsItemChunk = GetNewsItemChunkFromNodeText(nodeText);

            foreach (Item item in newsItemChunk.Data.Items)
            {
                NewsArticle article = new NewsArticle
                {
                    Guid = item.Id,
                    Title = System.Web.HttpUtility.HtmlEncode(item.Title),
                    Link = $"{BaseUrl}{item.Url}",
                    Description = GetArticleDescription($"{BaseUrl}{item.Url}"),
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

        private static NewsItemChunk GetNewsItemChunkFromNodeText(string nodeText)
        {
            string nodeJson = nodeText.Substring(nodeText.IndexOf("{", StringComparison.Ordinal),
                nodeText.LastIndexOf("}", StringComparison.Ordinal) -
                nodeText.IndexOf("{", StringComparison.Ordinal) + 1);
            NewsItemChunk newsItemChunk = JsonConvert.DeserializeObject<NewsItemChunk>(nodeJson);
            return newsItemChunk;
        }

        private string GetImageSource(List<Source> itemSources)
        {
            if (!itemSources.Any()) return string.Empty;

            string itemImageSource = itemSources.First().Src;
            int widthElementPosition = itemImageSource.LastIndexOf("&width=", StringComparison.OrdinalIgnoreCase);
            if (widthElementPosition >= 0)
            {
                itemImageSource = itemImageSource.Substring(0, widthElementPosition) + "&width=" + DesiredImageWidth;
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
            HtmlDocument articleDocument = GetArticleDocument(articleLink);

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

        private HtmlDocument GetArticleDocument(string articleLink)
        {
            HtmlDocument articleDocument;
            string cachedDocumentHtml = _distributedCache.GetString(articleLink);
            if (!string.IsNullOrEmpty(cachedDocumentHtml))
            {
                articleDocument = new HtmlDocument();
                articleDocument.LoadHtml(cachedDocumentHtml);
            }
            else
            {
                HtmlWeb articleWeb = new HtmlWeb();
                articleDocument = articleWeb.Load(articleLink);
                _distributedCache.SetString(articleLink, articleDocument.DocumentNode.OuterHtml, _cacheEntryOptions);
            }

            return articleDocument;
        }
    }
}
