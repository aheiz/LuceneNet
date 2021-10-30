namespace LuceneNet.Controllers
{
    using LuceneNet.Definitions.Lucene;
    using LuceneNet.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Route("[controller]")]
    public partial class SearchController : ControllerBase
    {
        private readonly ILogger<SearchController> _logger;
        private readonly IndexProvider indexProvider;

        public SearchController(
            ILogger<SearchController> logger,
            IndexProvider indexProvider)
        {
            _logger = logger;
            this.indexProvider = indexProvider;
        }

        [HttpGet]
        [Route("/{text}")]
        public LuceneSearchResult Search(string text)
        {
            var luceneSearchResult = this.indexProvider.Search(text);

            return luceneSearchResult;
        }
    }
}
