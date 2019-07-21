using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewsFeeder.Pages
{
    public class IndexModel : PageModel
    {
        public string Body { get; set; }
        public string Title { get; set; }
        public string SourceLink { get; set; }
        public string SelfLink { get; set; }
        public string Description { get; set; }

        public void OnGet()
        {
            const string empireUrl = "https://www.empireonline.com";
            Title = "Empire Latest Movie News";
            SourceLink = "https://www.empireonline.com/movies/news/";
            Description = "The latest movie news from Empire magazine";
            SelfLink = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}{HttpContext.Request.QueryString}";

            HtmlWeb newsWeb = new HtmlWeb();

            HtmlDocument newsDocument = newsWeb.Load(SourceLink);

            HtmlNodeCollection cardNodeList = newsDocument.DocumentNode.SelectNodes("//div[contains(concat(' ', normalize-space(@class), ' '), ' card ')]");

            if (cardNodeList == null)
            {
                Body = "No cards found!";
                return;
            }

            StringBuilder feedBuilder = new StringBuilder();
            feedBuilder.Append("<lastBuildDate>");
            feedBuilder.Append(DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss K"));
            feedBuilder.Append("</lastBuildDate>");

            int articleCount = 0;

            foreach (HtmlNode cardNode in cardNodeList)
            {
                string articleTitle = string.Empty;

                feedBuilder.Append("<item>");

                IEnumerable<HtmlNode> headerNodes = cardNode.Descendants("h3");
                IEnumerable<HtmlNode> paragraphNodes = cardNode.Descendants("p");
                IEnumerable<HtmlNode> linkNodes = cardNode.Descendants("a");
                IEnumerable<HtmlNode> imageNodes = cardNode.Descendants("img");
                IEnumerable<HtmlNode> spanNodes = cardNode.Descendants("span");

                // <title>
                if (headerNodes.Any())
                {
                    feedBuilder.Append("<title>");
                    feedBuilder.Append(headerNodes.First().InnerText);
                    feedBuilder.Append("</title>");
                }

                // <link>
                if (linkNodes.Any())
                {
                    feedBuilder.Append("<link>");
                    string linkHref = linkNodes.First().GetAttributeValue("href", string.Empty);
                    if (!linkHref.StartsWith("http"))
                        feedBuilder.Append(empireUrl);
                    feedBuilder.Append(linkHref);
                    feedBuilder.Append("</link>");
                }

                // <description>
                if (paragraphNodes.Any())
                {
                    feedBuilder.Append("<description>");
                    if (headerNodes.Any())
                    {
                        feedBuilder.Append(headerNodes.First().InnerText);
                        feedBuilder.Append("&lt;br&gt;");
                    }
                    if (imageNodes.Any())
                    {
                        string imgSrc = imageNodes.First().GetAttributeValue("src", string.Empty).Replace("width=750", "width=150");
                        feedBuilder.Append("&lt;img src=\"");
                        if (!imgSrc.StartsWith("http"))
                            feedBuilder.Append("http:");
                        feedBuilder.Append(imgSrc);
                        feedBuilder.Append("\"/&gt;&lt;br&gt;");
                    }
                    feedBuilder.Append(paragraphNodes.First().InnerText);
                    feedBuilder.Append("</description>");
                }

                // <pubDate>
                if (spanNodes.Any())
                {
                    string[] dateElements = spanNodes.Last().InnerText.Split(' ');
                    DateTime pubDate = DateTime.Now;
                    try
                    {
                        switch(dateElements[1])
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
                            default:
                                break;
                        }
                    }
                    finally
                    {
                        pubDate.AddMinutes(-articleCount++);
                        feedBuilder.Append("<pubDate>");
                        feedBuilder.Append(pubDate.ToString("ddd, dd MMM yyyy HH:mm:ss K"));
                        feedBuilder.Append("</pubDate>");
                    }
                }

                // <comments>
                if (imageNodes.Any())
                {
                    string imgSrc = imageNodes.First().GetAttributeValue("src", string.Empty);
                    feedBuilder.Append("<comments>");
                    if (!imgSrc.StartsWith("http"))
                        feedBuilder.Append("http:");
                    feedBuilder.Append(imgSrc);
                    feedBuilder.Append("</comments>");
                }

                // <guid>
                if (linkNodes.Any())
                {
                    feedBuilder.Append("<guid>");
                    feedBuilder.Append(linkNodes.First().GetAttributeValue("href", string.Empty));
                    feedBuilder.Append("</guid>");
                }

                feedBuilder.Append("</item>");
            }

            Body = feedBuilder.ToString();
        }
    }
}
