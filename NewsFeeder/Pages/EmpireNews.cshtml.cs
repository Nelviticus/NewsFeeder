namespace NewsFeeder.Pages
{
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Caching.Distributed;
    using Repositories;

    public class EmpireNewsModel : PageModel
    {
        public string Body { get; set; }
        public string Title { get; set; }
        public string SourceLink { get; set; }
        public string SelfLink { get; set; }
        public string Description { get; set; }
        private IDistributedCache _distributedCache;

        public EmpireNewsModel(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public void OnGet()
        {
            SelfLink = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}{HttpContext.Request.QueryString}";
            INewsRepository repository = new EmpireNewsRepository(_distributedCache);
            Title = repository.Title;
            SourceLink = repository.SourceLink;
            Description = repository.Description;
            Body = repository.Body;
        }
    }
}