namespace LuceneNet.Definitions.Lucene
{
    using System.Collections.Generic;

    public class LuceneSearchResult
    {
        public List<LuceneSearchResultItem> SearchResultItems { get; set; } = new List<LuceneSearchResultItem>();
        public int TotalResultCount { get; set; }
    }
}
