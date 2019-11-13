using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LuceneGeospatial_V3
{
    class Program
    {
        public static string IndexPath = "/LuceneIndex";

        static void Main(string[] args)
        {

            try
            {
                if (!Directory.Exists(IndexPath))
                {
                    Directory.CreateDirectory(IndexPath);
                }

                var places = JsonConvert.DeserializeObject<List<Place>>(File.ReadAllText("./Resources/PlacesData.json"));
                var indexer = new Indexer(IndexPath);
                indexer.InsertDataToIndex(places);             

            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
            Console.Read();
        }
    }
}
