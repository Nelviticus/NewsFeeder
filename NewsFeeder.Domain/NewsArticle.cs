namespace NewsFeeder.Domain
{
    using System;
    using System.Text;
    using Interfaces;

    public class NewsArticle: INewsArticle
    {
        private string _title;
        private string _link;
        private string _description;
        private DateTime _publicationDate;
        private string _comments;
        private string _guid;

        public string Title
        {
            get => _title == string.Empty ? "<title/>" : $"<title>{_title}</title>";
            set => _title = value;
        }

        public string Link
        {
            get => _link == string.Empty ? "<link/>" : $"<link>{_link}</link>";
            set => _link = value;
        }

        public string Description
        {
            get
            {
                StringBuilder descriptionBuilder = new StringBuilder();
                descriptionBuilder.Append("<description>");
                if (_title != string.Empty)
                {
                    descriptionBuilder.Append(System.Web.HttpUtility.HtmlEncode($"<h3>{_title}</h3>"));
                }

                if (ImageSrc != string.Empty)
                {
                    descriptionBuilder.Append(System.Web.HttpUtility.HtmlEncode($"<img src=\"{ImageSrc}\"><br>"));
                }
                descriptionBuilder.Append(_description);
                descriptionBuilder.Append("</description>");
                return descriptionBuilder.ToString();
            }
            set => _description = value;
        }

        public DateTime PublicationDate
        {
            set => _publicationDate = value;
        }

        public string PubDate => $"<pubDate>{_publicationDate:ddd, dd MMM yyyy HH:mm:ss K}</pubDate>";

        public string Comments
        {
            get => _comments == string.Empty ? "<comments/>" : $"<comments>{_comments}</comments>";
            set => _comments = value;
        }

        public string Guid {
            get => $"<guid>{_guid}</guid>";
            set => _guid = value;
        }

        public string ImageSrc { private get; set; }

        public override string ToString()
        {
            return $"<item>{Title}{Link}{Description}{PubDate}{Comments}{Guid}</item>";
        }
    }
}
