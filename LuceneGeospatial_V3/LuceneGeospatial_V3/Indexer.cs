using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using System.Collections.Generic;
using System.IO;

namespace LuceneGeospatial_V3
{
    class Indexer
    {
        private readonly SpatialContext _spatialContext;
        private readonly SpatialStrategy _strategy;
        private string LuceneIndex_Path;

        public Indexer(string indexPath)
        {
            this.LuceneIndex_Path = indexPath;
            _spatialContext = SpatialContext.GEO;
            SpatialPrefixTree grid = new GeohashPrefixTree(_spatialContext, 11);
            _strategy = new RecursivePrefixTreeStrategy(grid, "location");

        }

        public void InsertDataToIndex(List<Place> places)
        {
            var indexDirectory = FSDirectory.Open(new DirectoryInfo(this.LuceneIndex_Path));
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
            using (var writer = new IndexWriter(indexDirectory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                foreach (var place in places)
                {
                    // Purge place from Lucene if already exist
                    var query = new TermQuery(new Term("Id", place.Id.ToString()));
                    writer.DeleteDocuments(query);
                    IndexPlace(place, writer);
                }

                writer.Optimize();
                writer.Commit();
                analyzer.Close();
                writer.Dispose();
                indexDirectory.Dispose();
            }
        }


        private void IndexPlace(Place place, IndexWriter writer)
        {
            var doc = new Document();
            doc.Add(new Field("Id", place.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Name", place.Name, Field.Store.YES, Field.Index.ANALYZED));

            var scoreField = new NumericField("Rank", Field.Store.YES, true); //The sort fields must be indexed
            scoreField.SetIntValue(place.Rank);
            doc.Add(scoreField);

            doc.Add(new Field("Latitude", NumericUtils.DoubleToPrefixCoded(place.Latitude), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Longitude", NumericUtils.DoubleToPrefixCoded(place.Longitude), Field.Store.YES, Field.Index.NOT_ANALYZED));


            // These document values will be used when searching the index.
            var shape = (Shape)_spatialContext.MakePoint(place.Longitude, place.Latitude);
            foreach (var field in _strategy.CreateIndexableFields(shape))
            {
                doc.Add(field);
            }
            var point = (Point)shape;
            doc.Add(new Field("location", $"{point.GetX().ToString("0.0000000")},{point.GetY().ToString("0.0000000")}", Field.Store.YES, Field.Index.NO));

            if (place.Important) { doc.Boost=1.5f; }
            else { doc.Boost=0.1f; }
            writer.AddDocument(doc);
        }
    }

}

