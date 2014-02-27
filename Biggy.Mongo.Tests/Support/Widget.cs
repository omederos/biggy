using System;
using MongoDB.Bson;

namespace Biggy.Mongo.Tests.Support
{
    public class Widget
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Size { get; set; }
        public DateTime Expiration { get; set; }
        public decimal Price { get; set; }
    }
}