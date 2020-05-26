using System;
using System.Security;
using Microsoft.Azure.Documents.Client;

namespace CosmosLoadGenerator {
    public class Doc {
        public string id { get; set; }
        public string text { get; set; }
    }

    class Program {
        static Uri _cosmosUri;
        static string _sas;
        static string _database;
        static string _collection;
        static bool _continuationToken = true;

        static void Main (string[] args) {
            _cosmosUri = new Uri (Environment.GetEnvironmentVariable ("COSMOSURI"));
            _sas = Environment.GetEnvironmentVariable ("COSMOSKEY");
            _database = Environment.GetEnvironmentVariable ("DATABASE");
            _collection = Environment.GetEnvironmentVariable ("COLLECTION");

            for (int i = 0; i < 1000; i++)
            {
                CreateDocs();
            }

            System.Threading.Thread.Sleep(60000);
            _continuationToken = false;
        }

        static async void CreateDocs () {
            var client = new DocumentClient (_cosmosUri, _sas);

            while (_continuationToken) {
                var id = Guid.NewGuid().ToString();
                var doc = new Doc() {
                    id = id,
                    text = "Hello world " + id
                };
                client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri (_database, _collection), doc);
                System.Threading.Thread.Sleep(250);
            }
        }
    }
}