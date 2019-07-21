using System;
using System.Collections.Generic;
using System.Text;

namespace NewsFeeder.Domain
{
    public interface INewsArticle
    {
        string Title { get; set; }
        string Link { get; set; }
        string Description { get; set; }
        string PubDate { get; set; }
        string Comments { get; set; }
        string Guid { get; set; }
    }
}
