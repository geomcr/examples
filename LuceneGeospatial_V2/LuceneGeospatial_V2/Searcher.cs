using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using static Lucene.Net.Search.BooleanClause;
using Lucene.Net.Spatial.Tier.Projectors;
using Lucene.Net.Spatial.Tier;
using Lucene.Net.Documents;
using Lucene.Net.Util;

namespace LuceneGeospatial_V2
{
    public class Searcher
    {

        private string LuceneIndex_Path;

        public Searcher(string indexPath)
        {
            this.LuceneIndex_Path = indexPath;
        }

        public List<Place> SearchByField(string field, string value)
        {
            Query query;
            if (field == "Name")
            {
                Analyzer analyzer = new StandardAnalyzer(Version.LUCENE_29);
                var parser = new MultiFieldQueryParser(Version.LUCENE_29, new[] { field }, analyzer);
                query = parser.Parse(value);
                analyzer.Close();
            }
            else
            {
                query = new TermQuery(new Term(field, value));
            }
            return Search(query, 50);
        }

        private List<Place> Search(Query query, int top)
        {
            var items = new List<Place>();
            using (var searcher = new IndexSearcher(FSDirectory.Open(new DirectoryInfo(LuceneIndex_Path)), true))
            {

                var sort = new Sort(new SortField("Rank", SortField.INT, true));
                var topFieldDocs = searcher.Search(query, null, top, sort);
                int results = topFieldDocs.ScoreDocs.Length;

                for (int i = 0; i < results; i++)
                {
                    var scoreDoc = topFieldDocs.ScoreDocs[i];
                    var place = Convert(scoreDoc, searcher);
                    items.Add(place);
                }

                searcher.Close();
            }
            return items;
        }


        private const double KmsToMiles = 0.621371192;

        public IEnumerable<Place> SearchByRadius(double latitude, double longitude, double searchRadiusInKms)
        {
            /*  Builder allows us to build a polygon which we will use to limit  
             * search scope on our cartesian tiers, this is like putting a grid  over a map */
            CartesianPolyFilterBuilder builder = new CartesianPolyFilterBuilder(CartesianTierPlotter.DefaltFieldPrefix);

            /*  Bounding area draws the polygon, this can be thought of as working out which squares of the grid over a map to search */
            var boundingArea = builder.GetBoundingArea(latitude, longitude, searchRadiusInKms * KmsToMiles);

            /*  We refine, this is the equivalent of drawing a circle on the map,  
             *  within our grid squares, ignoring the parts the squares we are  searching that aren't within the circle - ignoring extraneous corners  and such */
            var distFilter = new LatLongDistanceFilter(boundingArea, searchRadiusInKms * KmsToMiles, latitude, longitude, "Latitude", "Longitude");
            var query = new BooleanQuery();
            query.Add(new ConstantScoreQuery(distFilter), Occur.MUST);

            var items = new List<Place>();
            using (var searcher = new IndexSearcher(FSDirectory.Open(new DirectoryInfo(LuceneIndex_Path)), true))
            {
                var sort = new Sort(new SortField("Rank", SortField.INT, true));
                var topDocs = searcher.Search(query, null, 50, sort);
                int results = topDocs.ScoreDocs.Length;

                for (int i = 0; i < results; i++)
                {
                    var scoreDoc = topDocs.ScoreDocs[i];
                    var place = Convert(scoreDoc, searcher);
                    items.Add(place);
                    var distanceInKM = distFilter.GetDistance(scoreDoc.doc) / KmsToMiles;
                    items.Add(place);
                }
            }
            return items;
        }

        private Place Convert(ScoreDoc scoreDoc, IndexSearcher searcher)
        {
            int docId = scoreDoc.doc;
            Document doc = searcher.Doc(docId);

            var result = new Place()
            {
                Id = int.Parse(doc.Get("Id")),
                Name = doc.Get("Name"),
                Rank = int.Parse(doc.GetFieldable("Rank").StringValue()),
                Latitude = NumericUtils.PrefixCodedToDouble(doc.GetFieldable("Latitude").StringValue()),
                Longitude = NumericUtils.PrefixCodedToDouble(doc.GetFieldable("Longitude").StringValue())
            };
            return result;
        }
    }
}
