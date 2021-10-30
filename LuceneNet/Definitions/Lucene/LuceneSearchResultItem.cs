namespace LuceneNet.Definitions.Lucene
{
    using global::Lucene.Net.Documents;
    using System.Collections.Generic;

    public class LuceneSearchResultItem
    {
        public int ID { get; set; }
        public Document Document { get; set; }
        public float Score { get; set; }
        public List<LuceneMatch> Matches { get; set; } = new List<LuceneMatch>();
    }
}
