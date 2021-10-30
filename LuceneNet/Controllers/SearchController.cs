namespace LuceneNet.Controllers
{
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;


    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<SearchController> _logger;
        private readonly IWebHostEnvironment webHostEnvironment;

        public SearchController(
            ILogger<SearchController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            this.webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IEnumerable<ScoreDoc> Search()
        {
            // Ensures index backward compatibility
            const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

            // Construct a path for the index:
            string indexPath = Path.Combine(this.webHostEnvironment.ContentRootPath, "Index");
            
            using var dir = Lucene.Net.Store.FSDirectory.Open(indexPath);

            // Create an analyzer to process the text
            var analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(AppLuceneVersion);

            // Create an index writer
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
            using var writer = new IndexWriter(dir, indexConfig);

            // Add to the index
            var source = new
            {
                Name = "Kermit the Frog",
                FavoritePhrase = "The quick brown fox jumps over the lazy dog"
            };
            var doc = new Document
            {
                // StringField indexes but doesn't tokenize
                new StringField("name",
                    source.Name,
                    Field.Store.YES),
                new TextField("favoritePhrase",
                    source.FavoritePhrase,
                    Field.Store.YES)
            };

            writer.AddDocument(doc);
            writer.Flush(triggerMerge: false, applyAllDeletes: false);


            // Search with a phrase
            var phrase = new MultiPhraseQuery
            {
                new Term("favoritePhrase", "brown"),
                new Term("favoritePhrase", "fox")
            };

            // Re-use the writer to get real-time updates
            using var reader = writer.GetReader(applyAllDeletes: true);
            var searcher = new IndexSearcher(reader);
            var hits = searcher.Search(phrase, 20 /* top 20 */).ScoreDocs;

            // Display the output in a table
            Console.WriteLine($"{"Score",10}" +
                $" {"Name",-15}" +
                $" {"Favorite Phrase",-40}");

            foreach (var hit in hits)
            {
                var foundDoc = searcher.Doc(hit.Doc);
                Console.WriteLine($"{hit.Score:f8}" +
                    $" {foundDoc.Get("name"),-15}" +
                    $" {foundDoc.Get("favoritePhrase"),-40}");
            }

            return hits;
        }
    }
}
