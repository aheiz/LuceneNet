namespace LuceneNet.Model
{
    using System.Xml.Serialization;
    public class Book
    {
        [XmlAttribute(attributeName: "id")]
        public string Id { get; set; }

        [XmlElement("author")]
        public string Author { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("genre")]
        public string Genre { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }
    }
}
