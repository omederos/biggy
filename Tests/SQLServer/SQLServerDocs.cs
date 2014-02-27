using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy;
using Biggy.SQLServer;
using Xunit;

namespace Tests.SQLServer {

  [Trait("SQL Server Document Store","")]
  public class SQLServerDocs {


    SQLDocumentList<CustomerDocument> docs;
    public SQLServerDocs() {
      docs = new SQLDocumentList<CustomerDocument>("northwind");
      //drop and reload
      docs.Clear();
    }


    [Fact(DisplayName = "Creates a store if one doesn't exist")]
    public void FactName() {
      Assert.True(docs.Count() == 0);
    }

    [Fact(DisplayName = "Adds a document")]
    public void AddsDocument() {
      var newCustomer = new CustomerDocument { Email = "rob@tekpub.com", First = "Rob", Last = "Conery" };
      docs.Add(newCustomer);
      Assert.Equal(1, docs.Count);
    }

    [Fact(DisplayName = "Updates a document")]
    public void UpdatesDocument() {
      var newCustomer = new CustomerDocument { Email = "rob@tekpub.com", First = "Rob", Last = "Conery" };
      docs.Add(newCustomer);
      newCustomer.First = "Bill";
      var updated = docs.Update(newCustomer);
      Assert.Equal(1, updated);
    }

    [Fact(DisplayName = "Deletes a document")]
    public void DeletesDocument() {
      var newCustomer = new CustomerDocument { Email = "rob@tekpub.com", First = "Rob", Last = "Conery" };
      docs.Add(newCustomer);
      var removed = docs.Remove(newCustomer);
      Assert.True(removed);
    }

    class Monkey {
      [PrimaryKey]
      public string Name { get; set; }
      public DateTime Birthday { get; set; }
      [FullText]
      public string Description { get; set; }
    }

    [Fact(DisplayName = "Inserts metric butt-load of new records as JSON documents with string key")]
    static void InsertsManyMonkeys() {
      int INSERT_QTY = 10000;
      var monkies = new SQLDocumentList<Monkey>("northwind");
      monkies.Clear();

      var addRange = new List<Monkey>();
      for (int i = 0; i < INSERT_QTY; i++) {
        addRange.Add(new Monkey { Name = "MONKEY " + i, Birthday = DateTime.Today, Description = "The Monkey on my back" });
      }
      var inserted = monkies.AddRange(addRange);
      Assert.True(inserted == INSERT_QTY && monkies.Count == inserted);
    }

    [Fact(DisplayName = "Will create a table with serial PK")]
    public void CreatesSerialPK() {
      var actors = new SQLDocumentList<Actor>("northwind");
      var newActor = new Actor { First_Name = "Joe", Last_Name = "Blow" };
      actors.Add(newActor);
      Assert.True(newActor.Actor_ID > 0);
    }

    [Fact(DisplayName = "Inserts metric butt-load of new records as JSON documents with integer key")]
    static void InsertsManyActors() {
      var actors = new SQLDocumentList<Actor>("northwind");
      var bulkList = new List<Actor>();
      for (int i = 0; i < 100; i++) {
        var newActor = new Actor { First_Name = "Actor " + i, Last_Name = "Test" };
        bulkList.Add(newActor);
      }
      actors.AddRange(bulkList);
      Assert.True(actors.Last().Actor_ID > 90);

    }

    //[Fact(DisplayName = "Creates a FullText document table")]
    //public void FullTextDocument() {
    //  var films = new SQLDocumentList<Film>("northwind");
    //  var film = new Film { Description = "Lorem ipsum", FullText = "Search on this marigold", Length = 100, ReleaseYear = DateTime.Today, Title = "Test Title" };
    //  films.Add(film);
    //  Assert.True(film.Film_ID > 0);
    //}
  }
}
