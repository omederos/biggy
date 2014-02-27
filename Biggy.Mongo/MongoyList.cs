using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Biggy.Mongo
{
    public class MongoyList<T> : InMemoryList<T> where T: new()
    {        
        public MongoyList(string host,                           
                          string database = "biggy", 
                          string collection = "list",
                          int port = 27017,
                          string username=null, 
                          string password=null)
        {
            Initialize(host, port, database, collection, username, password);
            Reload();
            FireLoadedEvents();
        }

        public void Add(T thing)
        {
            _collection.Insert(thing);
            base.Add(thing);
        }

        public void Add(ICollection<T> things)
        {
            _collection.InsertBatch(things);
            foreach (var thing in things)
            {
                base.Add(thing);
            }
        }

        public void Flush()
        {
            foreach (var item in _items)
            {
                _collection.Save(item);
            }
        }

        public void Reload()
        {
            _items = _collection.FindAll().ToList();
        }

        public void Remove(T thing)
        {
            // todo - best way to achieve an Id constraint?
            var query = Query.EQ("_id", ((dynamic)thing).Id);
            _collection.Remove(query);
            _items.Remove(thing);
        }

        private void Initialize(string host, int port, string database, string collection, string username, string password)
        {
            var clientSettings = CreateClientSettings(host, port, database, username, password);
            _client = new MongoClient(clientSettings);
            _server = _client.GetServer();
            _database = _server.GetDatabase(database);
            _collection = _database.GetCollection<T>(collection);
        }

        private static MongoClientSettings CreateClientSettings(string host, int port, string database, 
                                                                string username, string password)
        {
            var clientSettings = new MongoClientSettings();
            clientSettings.Server = new MongoServerAddress(host, port);
            if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password))
            {
                var credential = MongoCredential.CreateMongoCRCredential(database, username, password);
                clientSettings.Credentials = new[] {credential};
            }
            return clientSettings;
        }


        private MongoClient _client;
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<T> _collection;
    }
}