using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Spatial4n.Core.Context;

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


        public Searcher()
        {
            IndexDirectory = FSDirectory.Open(LuceneIndex_Path);
            SpatialPrefixTree grid = new GeohashPrefixTree(_spatialContext, 11);
            _strategy = new RecursivePrefixTreeStrategy(grid, Location);
        }
      

    }
}
