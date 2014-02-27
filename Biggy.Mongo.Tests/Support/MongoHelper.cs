using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Biggy.Mongo.Tests.Support
{
    public class MongoHelper<T>
    {
        private MongoClient _client;
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<T> _collection; 

        public MongoHelper(string host, string databaseName, string collectionName)
        {
            var settings = new MongoClientSettings
                {
                    Server = new MongoServerAddress(host)
                };
            _client = new MongoClient(settings);
            _server = _client.GetServer();
            _database = _server.GetDatabase(databaseName);
            _collection = _database.GetCollection<T>(collectionName);
        }

        public long Insert(T thing)
        {
            var result = _collection.Insert(thing);
            return result.DocumentsAffected;
        }

        public void Clear()
        {
            _collection.RemoveAll();
        }

        public long Count()
        {
            return _collection.Count();
        }

        public T Find(ObjectId id)
        {
            return _collection.FindOneById(id);
        }
    }
}