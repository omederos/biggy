using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy;
using Biggy.Postgres;
using Xunit;
using Newtonsoft.Json;

namespace Tests.Postgres {

  [Trait("SQL Server Document Store","")]
  public class PGDocs {

    public string _connectionStringName = "chinookPG";

    PGDocumentList<ClientDocument> clientDocs;
    PGDocumentList<MonkeyDocument> monkeyDocs;
    public PGDocs() {
      // Start fresh each time, with no existing table, to keep serial PK's from exploding:
      if(this.TableExists("clientdocuments")) {
        this.DropTable("clientdocuments");
      }
      if (this.TableExists("monkeydocuments")) {
        this.DropTable("monkeydocuments");
      }
      clientDocs = new PGDocumentList<ClientDocument>(_connectionStringName);
      monkeyDocs = new PGDocumentList<MonkeyDocument>(_connectionStringName);
      clientDocs.Clear();
    }


    [Fact(DisplayName = "Creates a store with a serial PK if one doesn't exist")]
    public void Creates_Document_Table_With_Serial_PK_If_Not_Present() {
      Assert.True(clientDocs.Count() == 0);
    }


    [Fact(DisplayName = "Creates a store with a string PK if one doesn't exist")]
    public void Creates_Document_Table_With_String_PK_If_Not_Present() {
      Assert.True(monkeyDocs.Count() == 0);
    }


    [Fact(DisplayName = "Adds a document with a serial PK")]
    public void Adds_Document_With_Serial_PK() {
      var newCustomer = new ClientDocument { 
        Email = "rob@tekpub.com", 
        FirstName = "Rob", 
        LastName = "Conery" };
      clientDocs.Add(newCustomer);
      Assert.Equal(1, clientDocs.Count);
    }


    [Fact(DisplayName = "Updates a document with a serial PK")]
    public void Updates_Document_With_Serial_PK() {
      var newCustomer = new ClientDocument {
        Email = "rob@tekpub.com",
        FirstName = "Rob",
        LastName = "Conery"
      };
      clientDocs.Add(newCustomer);
      int idToFind = newCustomer.ClientDocumentId;
      // Go find the new record after reloading:
      clientDocs.Reload();
      var updateMe = clientDocs.FirstOrDefault(cd => cd.ClientDocumentId == idToFind);
      // Update:
      updateMe.FirstName = "Bill";
      clientDocs.Update(updateMe);
      // Go find the updated record after reloading:
      clientDocs.Reload();
      var updated = clientDocs.FirstOrDefault(cd => cd.ClientDocumentId == idToFind);
      Assert.True(updated.FirstName == "Bill");
    }


    [Fact(DisplayName = "Deletes a document with a serial PK")]
    public void Deletes_Document_With_Serial_PK() {
      var newCustomer = new ClientDocument {
        Email = "rob@tekpub.com",
        FirstName = "Rob",
        LastName = "Conery"
      };
      clientDocs.Add(newCustomer);
      // Count after adding new:
      int initialCount = clientDocs.Count;
      var removed = clientDocs.Remove(newCustomer);
      clientDocs.Reload();
      // Count after removing and reloading:
      int finalCount = clientDocs.Count;
      Assert.True(finalCount < initialCount && removed);
    }


    [Fact(DisplayName = "Bulk-Inserts new records as JSON documents with string key")]
    public void Bulk_Inserts_Documents_With_String_PK() {
      int INSERT_QTY = 100;
      var monkies = new PGDocumentList<MonkeyDocument>("chinookPG");
      monkies.Clear();

      var addRange = new List<MonkeyDocument>();
      for (int i = 0; i < INSERT_QTY; i++) {
        addRange.Add(new MonkeyDocument { Name = "MONKEY " + i, Birthday = DateTime.Today, Description = "The Monkey on my back" });
      }
      var inserted = monkies.AddRange(addRange);
      Assert.True(inserted == INSERT_QTY);
    }


    [Fact(DisplayName = "Bulk-Inserts new records as JSON documents with serial int key")]
    static void Bulk_Inserts_Documents_With_Serial_PK() {
      int insertQty = 100;
      var ClientDocuments = new PGDocumentList<ClientDocument>("chinookPG");
      var bulkList = new List<ClientDocument>();
      for (int i = 0; i < insertQty; i++) {
        var newClientDocument = new ClientDocument { 
          FirstName = "ClientDocument " + i, 
          LastName = "Test",
          Email = "jatten@example.com"
        };
        bulkList.Add(newClientDocument);
      }
      int inserted = ClientDocuments.AddRange(bulkList);

      var last = ClientDocuments.Last();
      Assert.True(inserted == insertQty && last.ClientDocumentId >= insertQty);
    }


    ////[Fact(DisplayName = "Creates a FullText document table")]
    ////public void FullTextDocument() {
    ////  var films = new PGDocumentList<Film>("chinook");
    ////  var film = new Film { Description = "Lorem ipsum", FullText = "Search on this marigold", Length = 100, ReleaseYear = DateTime.Today, Title = "Test Title" };
    ////  films.Add(film);
    ////  Assert.True(film.Film_ID > 0);
    ////}


    void DropTable(string tableName)
    {
      string sql = string.Format("DROP TABLE {0}", tableName);
      var Model = new PGTable<dynamic>(_connectionStringName);
      Model.Execute(sql);
    }


    bool TableExists(string tableName)
    {
      bool exists = false;
      string select = ""
          + "SELECT * FROM information_schema.tables "
          + "WHERE table_schema = 'public' "
          + "AND  table_name = '{0}'";
      string sql = string.Format(select, tableName);
      var Model = new PGTable<dynamic>(_connectionStringName);
      var query = Model.Query<dynamic>(sql);
      if (query.Count() > 0)
      {
        exists = true;
      }
      return exists;
    }



  }
}
