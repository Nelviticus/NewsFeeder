namespace NewsFeeder.Repositories.DataTransferObjects.EmpireNews
{
    using System.Collections.Generic;

    internal class Source
    {
        public string AltText { get; set; }
        public string Src { get; set; }
        public int Media { get; set; }
    }

    internal class Category
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    internal class Item
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

    internal class Data
    {
        public List<Item> Items { get; set; }
        public string Type { get; set; }
        public string ComponentId { get; set; }
    }

    internal class NewsItemChunk
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Data Data { get; set; }
    }
}
