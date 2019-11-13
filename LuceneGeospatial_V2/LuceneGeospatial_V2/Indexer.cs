using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Tier.Projectors;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.IO;

namespace LuceneGeospatial_V2
{
    public class Indexer
    {
        private static Dictionary<int, CartesianTierPlotter> _Plotters { get; set; }
        public const double KmsToMiles = 0.621371192;
        private const double MaxKms = 50 * KmsToMiles;
        private const double MinKms = 1 * KmsToMiles;

        private static int _startTier;
        private static int _endTier;
        private string LuceneIndex_Path;

        public Indexer(string indexPath)
        {
            this.LuceneIndex_Path = indexPath;
            Tiers_Generate();
        }

        public void InsertDataToIndex(List<Place> places)
        {
            var indexDirectory = FSDirectory.Open(new DirectoryInfo(this.LuceneIndex_Path));
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);
            using (var writer = new IndexWriter(indexDirectory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                foreach (var place in places)
                {
                    // Purge places from Lucene if they already exist
                    var query = new TermQuery(new Term("Id", place.Id.ToString()));
                    writer.DeleteDocuments(query);

                    IndexPlace(place, writer);
                }

                writer.Optimize();
                writer.Commit();
                analyzer.Close();
                writer.Dispose();
            }
        }
       

        private void IndexPlace(Place place, IndexWriter writer)
        {
            var doc = new Document();
            doc.Add(new Field("Id", place.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Name", place.Name, Field.Store.YES, Field.Index.ANALYZED));
            var scoreField = new NumericField("Rank", Field.Store.YES, true); //The sorted fields must be indexed
            scoreField.SetIntValue(place.Rank);
            doc.Add(scoreField);

            doc.Add(new Field("Latitude", NumericUtils.DoubleToPrefixCoded(place.Latitude), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Longitude", NumericUtils.DoubleToPrefixCoded(place.Longitude), Field.Store.YES, Field.Index.NOT_ANALYZED));
            AddCartesianTiers(place.Latitude, place.Longitude, doc);

            if (place.Important) { doc.SetBoost(1.5f); }
            else { doc.SetBoost(0.1f); }

            writer.AddDocument(doc);
        }

        private void AddCartesianTiers(double latitude, double longitude, Document document)
        {
            for (var tier = _startTier; tier <= _endTier; tier++)
            {
                var ctp = _Plotters[tier];
                var boxId = ctp.GetTierBoxId(latitude, longitude);
                document.Add(new Field(ctp.GetTierFieldName(), NumericUtils.DoubleToPrefixCoded(boxId), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            }
        }

        private void Tiers_Generate()
        {
            IProjector projector = new SinusoidalProjector();
            var ctp = new CartesianTierPlotter(0, projector, CartesianTierPlotter.DefaltFieldPrefix);

            //The starting tier (the largest grid square) calculated by providing the furthest distance in miles that we want to search
            _startTier = ctp.BestFit(MaxKms);

            //The last tier (the smallest grid square) calculated by providing the closest distance in miles that we want to search
            _endTier = ctp.BestFit(MinKms);

            _Plotters = new Dictionary<int, CartesianTierPlotter>();
            for (var tier = _startTier; tier <= _endTier; tier++)
            {
                _Plotters.Add(tier, new CartesianTierPlotter(tier, projector, CartesianTierPlotter.DefaltFieldPrefix));
            }
        }
    }
}
