namespace LuceneNet.Model
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("catalog")]
    public class Catalog
    {
        [XmlElement("book")]
        public List<Book> Books { get; set; }
    }
}
