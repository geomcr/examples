using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LuceneGeospatial_V3
{
    public class Searcher
    {
        private readonly Version LuceneVersion = Version.LUCENE_30;
        private readonly SpatialContext _spatialContext = SpatialContext.GEO;
        private readonly SpatialStrategy _strategy;
        private const string Location = "location";
        private const int Limit = 50;
        private FSDirectory IndexDirectory;
        private string LuceneIndex_Path;

        public Searcher(string indexPath)
        {
            this.LuceneIndex_Path = indexPath;
            IndexDirectory = FSDirectory.Open(LuceneIndex_Path);
            SpatialPrefixTree grid = new GeohashPrefixTree(_spatialContext, 11);
            _strategy = new RecursivePrefixTreeStrategy(grid, Location);
        }



        public List<Place> SearchByName(string name, int limit)
        {
            var items = new List<Place>();

            using (var searcher = new IndexSearcher(FSDirectory.Open(new DirectoryInfo(this.LuceneIndex_Path)), true))
            using (var analyser = new StandardAnalyzer(LuceneVersion))
            {
                var parser = new MultiFieldQueryParser(LuceneVersion, new[] { "Name" }, analyser);
                var query = parser.Parse(name);

                var sort = new Sort(new SortField("Rank", SortField.INT, true));
                var topFieldDocs = searcher.Search(query, null, limit, sort);
                var scoreDocs = topFieldDocs.ScoreDocs;
                items = scoreDocs.Select(x => MapDocumentToPlace(searcher.Doc(x.Doc))).ToList();

                analyser.Close();
                searcher.Dispose();
            }
            return items;
        }

        public IList<Place> SearchByCircle(double latitude, double longitude, double radiusInKM, int maxHits = 10)
        {
            IList<Place> results;

            using (var searcher = new IndexSearcher(IndexDirectory, true))
            {
                var distance = DistanceUtils.Dist2Degrees(radiusInKM, DistanceUtils.EARTH_MEAN_RADIUS_KM);
                var searchArea = _spatialContext.MakeCircle(longitude, latitude, distance);
                var args = new SpatialArgs(SpatialOperation.Intersects, searchArea);
                var query = _strategy.MakeQuery(args);

                BooleanQuery bq = new BooleanQuery
                {
                    { query, Occur.MUST }
                };

                var hits = searcher.Search(bq, maxHits).ScoreDocs;
                results = hits.Select(x => MapDocumentToPlace(searcher.Doc(x.Doc))).ToList();
            }

            return results;
        }

        public IList<Place> SearchByRectangleAndName(string venueName, Point upperRight, Point lowerLeft)
        {
            using (var searcher = new IndexSearcher(IndexDirectory, true))
            using (var analyser = new StandardAnalyzer(LuceneVersion))
            {

                var fields = new[] { "Name" };
                var parser = new MultiFieldQueryParser(LuceneVersion, fields, analyser);
                var query = parser.Parse(venueName);

                var searchArea = _spatialContext.MakeRectangle(lowerLeft, upperRight);
                var args = new SpatialArgs(SpatialOperation.Intersects, searchArea);
                Filter filter = _strategy.MakeFilter(args);

                TopDocs topDocs = searcher.Search(query, filter, Limit);
                ScoreDoc[] scoreDocs = topDocs.ScoreDocs;
                var results = scoreDocs.Select(hit => MapDocumentToPlace(searcher.Doc(hit.Doc))).ToList();
                return results;
            }
        }


        public IList<Place> SearchByCircle_ScoringByDistance(string venueName, double latitude, double longitude, double searchRadiusKm, int maxHits = 10)
        {
            IList<Place> results;

            using (var searcher = new IndexSearcher(IndexDirectory, true))
            using (var analyser = new StandardAnalyzer(LuceneVersion))
            {
                var distance = DistanceUtils.Dist2Degrees(searchRadiusKm, DistanceUtils.EARTH_MEAN_RADIUS_KM);
                var searchArea = _spatialContext.MakeCircle(longitude, latitude, distance);

                var fields = new[] { "Name" };
                var parser = new MultiFieldQueryParser(LuceneVersion, fields, analyser);
                var query = parser.Parse(venueName);

                var spatialArgs = new SpatialArgs(SpatialOperation.Intersects, searchArea);
                var spatialQuery = _strategy.MakeQuery(spatialArgs);
                var valueSource = _strategy.MakeRecipDistanceValueSource(searchArea); //Thus the scores will be 1 for indexed points  at the center of the query shape and as low as ~0.1 at its furthest edges.
                var valueSourceFilter = new ValueSourceFilter(new QueryWrapperFilter(spatialQuery), valueSource, 0, 1);

                var filteredSpatial = new FilteredQuery(query, valueSourceFilter);
                var spatialRankingQuery = new FunctionQuery(valueSource);

                BooleanQuery bq = new BooleanQuery();
                bq.Add(filteredSpatial, Occur.MUST);
                bq.Add(spatialRankingQuery, Occur.MUST);

                var hits = searcher.Search(bq, maxHits).ScoreDocs;
                results = hits.Select(hit => MapDocumentToPlace(searcher.Doc(hit.Doc))).ToList();
            }

            return results;
        }



        private Place MapDocumentToPlace(Document doc)
        {
            var location = doc.Get(Location).Split(','); //also we can have the coordinates from the Location param
            var Place = new Place()
            {
                Id = int.Parse(doc.Get("Id")),
                Name = doc.Get("Name"),
                Rank = int.Parse(doc.GetFieldable("Rank").StringValue),
                Latitude = NumericUtils.PrefixCodedToDouble(doc.GetFieldable("Latitude").StringValue),
                Longitude = NumericUtils.PrefixCodedToDouble(doc.GetFieldable("Longitude").StringValue)
            };
            return Place;
        }

    }
}
