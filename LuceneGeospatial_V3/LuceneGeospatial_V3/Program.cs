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

                var searcher = new Searcher(IndexPath);


                Console.WriteLine("Searching places with name Place_3");
                var values = searcher.SearchByName("Place_3", 1);
                Console.WriteLine($" Result: { JsonConvert.SerializeObject(values.First())}");
                Console.WriteLine();
                Console.WriteLine("Searching for all places within 5KM of 51.5107493, -0.155577");
                values = searcher.SearchByCircle(51.5107493, -0.155577, 5).ToList();
                Console.WriteLine($" Result: { JsonConvert.SerializeObject(values.First())}");

                Console.WriteLine("Press any key to close");
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
            Console.Read();
        }
    }
}
