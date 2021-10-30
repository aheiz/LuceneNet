namespace LuceneNet.Services
{
    using LuceneNet.Model;
    using Microsoft.AspNetCore.Hosting;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;

    public class SampleDataProvider
    {
        private readonly IWebHostEnvironment webHostEnvironment;

        public SampleDataProvider(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }

        public List<Book> GetBooks()
        {
            var filePath = Path.Combine(this.webHostEnvironment.ContentRootPath, "Data", "Books.xml");
            var xmlText = File.ReadAllText(filePath);

            XmlSerializer serializer = new XmlSerializer(typeof(Catalog));
            using (TextReader reader = new StringReader(xmlText))
            {
                return ((Catalog)serializer.Deserialize(reader)).Books;
            }
        }
    }
}
