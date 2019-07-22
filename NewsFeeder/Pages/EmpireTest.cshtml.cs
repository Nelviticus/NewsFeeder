namespace NewsFeeder.Pages
{
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Repositories;

    public class EmpireTestModel : PageModel
    {
        public string Body { get; set; }
        public string Title { get; set; }
        public string SourceLink { get; set; }
        public string SelfLink { get; set; }
        public string Description { get; set; }

        public void OnGet()
        {
            SelfLink = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}{HttpContext.Request.QueryString}";
            EmpireNewsRepository repository = new EmpireNewsRepository();
            Title = repository.Title;
            SourceLink = repository.SourceLink;
            Description = repository.Description;
            Body = repository.Body;
        }
    }
}