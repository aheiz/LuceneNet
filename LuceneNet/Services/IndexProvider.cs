namespace LuceneNet.Services
{
    using Lucene.Net.Analysis;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Search.Highlight;
    using Lucene.Net.Store;
    using Lucene.Net.Util;
    using LuceneNet.Definitions.Lucene;
    using Microsoft.AspNetCore.Hosting;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class IndexProvider
    {
        // Ensures index backward compatibility
        const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly SampleDataProvider sampleDataProvider;

        public IndexProvider(
            IWebHostEnvironment webHostEnvironment,
            SampleDataProvider sampleDataProvider)
        {
            this.webHostEnvironment = webHostEnvironment;
            this.sampleDataProvider = sampleDataProvider;

            this.CreateSearchIndex();
        }

        public LuceneSearchResult Search(string pattern)
        {
            var fields = new string[] { "id", "author", "title", "gerne", "description" };

            var combinedQuery = new BooleanQuery();

            // Add wildcard queries: (match *pattern*)
            this.CreateWildcardQueriesFor(fields, pattern).ForEach(query =>
            {
                combinedQuery.Add(query, Occur.SHOULD);
            });

            // Add term queries (exact match) = boost 
            this.CreateTermQueriesFor(fields, pattern).ForEach(query =>
            {
                combinedQuery.Add(query, Occur.SHOULD);
            });

            if (pattern.Trim().Length > 4)
            {
                // Add fuzzy queries (levenshtein edit distance algorithm):
                // if string length is long enough to avoid too broad searches
                this.CreateFuzzyQueriesFor(fields, pattern).ForEach(query =>
                {
                    combinedQuery.Add(query, Occur.SHOULD);
                });
            }

            using var fsIndexDirectory = FSDirectory.Open(this.IndexPath);

            using var writer = this.GetDefaultIndexWriter(fsIndexDirectory);

            using var reader = writer.GetReader(applyAllDeletes: true);

            var searcher = new IndexSearcher(reader);

            // Perform search:
            var searchResult = searcher.Search(combinedQuery, 20);

            int totalHits = searchResult.TotalHits;

            // Get results and highlighted fragments:
            var searchResultItems = new List<LuceneSearchResultItem>();

            searchResult.ScoreDocs.ToList().ForEach(x =>
            {
                var document = searcher.Doc(x.Doc);
                var searchResultItem = GetResultItemFromDocument(writer.Analyzer,
                    document, combinedQuery, x.Score);

                searchResultItems.Add(searchResultItem);
            });

            return new LuceneSearchResult()
            {
                SearchResultItems = searchResultItems,
                TotalResultCount = totalHits
            };
        }

        //Extract the relevant information about the search.
        private LuceneSearchResultItem GetResultItemFromDocument(
            Analyzer analyzer,
            Document document,
            Query query,
            float score)
        {
            var searchResultItem = new LuceneSearchResultItem
            {
                Document = document,
                Score = score
            };

            var scorer = new QueryScorer(query);
            var formatter = new SimpleHTMLFormatter(
                "<span class=\"highlighted-match\">", "</span>");

            // Makes sure always the entire content of a
            // field is fetched.
            var fragmenter = new NullFragmenter();
            var highlighter = new Highlighter(formatter, scorer)
            {
                TextFragmenter = fragmenter
            };

            // For each field check if there were any search matches
            // on that field.
            document.Fields.ToList().ForEach(field =>
            {
                var fieldKey = field.Name;
                var fieldValue = field.GetStringValue();
                var tokenStream = analyzer.GetTokenStream(
                    fieldKey, new StringReader(fieldValue));

                // For each field get value with HTML markup around
                // matches. Ignore fields with a score of 0 (no match)
                // As NullFragmenter anyway only creates one Fragment
                // per Field, maxNumberOfFragments can be set to 1.
                var matches = highlighter
                    .GetBestTextFragments(tokenStream, fieldValue, false, 1)
                    .Where(m => m.Score > 0)
                    .Select(m => GetMatchFromFragment(m, fieldKey));

                searchResultItem.Matches.AddRange(matches);
            });

            return searchResultItem;
        }

        // Create a LuceneMatch instance for every field which had
        // one or more matches.
        private LuceneMatch GetMatchFromFragment(
            TextFragment fragment, string fieldKey)
        {
            var matchText = fragment.ToString();

            var match = new LuceneMatch
            {
                FieldKey = fieldKey,
                MatchText = matchText
            };

            return match;
        }

        private string IndexPath => Path.Combine(this.webHostEnvironment.ContentRootPath, "Index");

        private void CreateSearchIndex()
        {
            // delete and recreate index:
            System.IO.Directory.Delete(this.IndexPath, true);

            using var fsIndexDirectory = FSDirectory.Open(this.IndexPath);

            using var writer = GetDefaultIndexWriter(fsIndexDirectory);

            // read some sample data:
            var books = this.sampleDataProvider.GetBooks();

            var documents = books.Select(book => new Document
            {
                new StringField("id",
                    book.Id,
                    Field.Store.YES),
                new TextField("author",
                    book.Author,
                    Field.Store.YES),
                new TextField("title",
                    book.Title,
                    Field.Store.YES),
                new TextField("genre",
                    book.Genre,
                    Field.Store.YES),
                new TextField("description",
                    book.Description,
                    Field.Store.YES),
            }).ToList();

            writer.AddDocuments(documents);
            writer.Flush(triggerMerge: false, applyAllDeletes: false);
        }

        private IndexWriter GetDefaultIndexWriter(FSDirectory fsIndexDirectory) 
        {
            // Create an analyzer to process the text
            var analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(AppLuceneVersion);
            
            // Create an index writer
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);

            return new IndexWriter(fsIndexDirectory, indexConfig);
        }

        private List<WildcardQuery> CreateWildcardQueriesFor(string[] fields, string pattern)
            => fields.Select(field => new WildcardQuery(new Term(field, "*" + pattern + "*")))
            .ToList();

        private List<FuzzyQuery> CreateFuzzyQueriesFor(string[] fields, string pattern)
            => fields.Select(field => new FuzzyQuery(new Term(field, pattern)))
            .ToList();

        private List<TermQuery> CreateTermQueriesFor(string[] fields, string pattern)
            => fields.Select(field =>
            {
                var termQuery = new TermQuery(new Term(field, pattern));
                termQuery.Boost = 10;
                return termQuery;
            })
            .ToList();
    }
}
