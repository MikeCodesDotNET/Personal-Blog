using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Diagnostics;
using LuceneDirectory = Lucene.Net.Store.Directory;

using System.IO;

using System;
using MikeCodesDotNET.Models;
using static Lucene.Net.Util.Packed.PackedInt32s;
using System.Threading.Tasks;

namespace MikeCodesDotNET.Services.Blog
{
    public class BlogPostSearchService
    {
        // Ensures index backward compatibility
        const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        private IndexWriter _writer;
        private Analyzer _standardAnalyzer;

        private void Start()
        {
            var indexDir = GetDirectory();
            //Create an analyzer to process the text 
            _standardAnalyzer = new StandardAnalyzer(AppLuceneVersion);

            //Create an index writer
            IndexWriterConfig indexConfig = new IndexWriterConfig(AppLuceneVersion, _standardAnalyzer);
            indexConfig.OpenMode = OpenMode.CREATE;                             // create/overwrite index
            _writer = new IndexWriter(indexDir, indexConfig);
        }

        public void Add(BlogPost post)
        {
            Document doc = new Document();
            doc.Add(new TextField("title", post.MarkdownContent.Title, Field.Store.YES));
            doc.Add(new TextField("content", post.MarkdownContent.MarkdownText, Field.Store.YES));
            doc.Add(new StringField("url", post.PublishedUrl, Field.Store.YES));
            _writer.AddDocument(doc);

            _writer.Commit();
        }

        //public Task<BlogPost> Search(string term)
        //{
        //    using DirectoryReader reader = _writer.GetReader(applyAllDeletes: true);
        //    IndexSearcher searcher = new IndexSearcher(reader);

        //    Query query = new TermQuery(new Term("title", term));
        //    var hits = searcher.Search(query, 20).ScoreDocs; 


        //}


        private FSDirectory GetDirectory()
        {
            // Construct a machine-independent path for the index
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var indexPath = Path.Combine(basePath, "index");

            using var indexDir = FSDirectory.Open(indexPath);
            return indexDir;
        }
    }
}
