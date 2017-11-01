using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Postgres;
using Xunit;

namespace Tests.Postgres {

  [Trait("Postsgres List Basic CRUD", "")]
  public class PGList_CRUD
  {
    public string _connectionStringName = "chinookPG";
    PGList<Client> _Clients;

    // Runs before every test:
    public PGList_CRUD() {
      // Drops and re-creates table each time:
      this.SetUpClientTable();
      _Clients = new PGList<Client>(connectionStringName: _connectionStringName, tableName: "clients");
    }


    [Fact(DisplayName = "Loads Empty Table into memory")]
    public void Loads_Data_Into_Memory() {
      Assert.True(_Clients.Count == 0);
    }


    [Fact(DisplayName = "Adds a Record")]
    public void Adds_New_Record() {
      // How many to start with?
      int initialCount = _Clients.Count();
      var newClient = new Client() { FirstName = "John", LastName = "Atten", Email = "jatten@example.com" };
      _Clients.Add(newClient);
      int idToFind = newClient.ClientId;
      _Clients = new PGList<Client>(connectionStringName: _connectionStringName, tableName: "clients");
      var found = _Clients.FirstOrDefault(c => c.ClientId == idToFind);
      Assert.True(found.Email == "jatten@example.com" && _Clients.Count > initialCount);
    }


    [Fact(DisplayName = "Updates a record")]
    public void Updates_Record() {
      var newClient = new Client() {
        FirstName = "John",
        LastName = "Atten",
        Email = "jatten@example.com"
      };
      _Clients.Add(newClient);
      int idToFind = newClient.ClientId;
      _Clients.Reload();
      var found = _Clients.FirstOrDefault(c => c.ClientId == idToFind);

      // After insert, no new record should be added:
      int currentCount = _Clients.Count();
      found.FirstName = "Jimi";
      _Clients.Update(found);
      _Clients.Reload();

      Assert.True(found.FirstName == "Jimi" && _Clients.Count == currentCount);
    }


    int _qtyInserted = 100;
    [Fact(DisplayName = "Bulk Inserts Records")]
    public void Bulk_Inserts_Records() {
      int initialCount = _Clients.Count();
      var rangeToAdd = new List<Client>();

      for (int i = 0; i < _qtyInserted; i++) {
        var newClient = new Client() {
          FirstName = string.Format("John{0}", i.ToString()),
          LastName = "Atten",
          Email = string.Format("jatten@example{0}.com", i.ToString())
        };
        rangeToAdd.Add(newClient);
      }

      int qtyAdded = _Clients.AddRange(rangeToAdd);
      _Clients.Reload();
      Assert.True(_Clients.Count == initialCount + _qtyInserted);
    }


    [Fact(DisplayName = "Deletes a record")]
    public void Deletes_Record() {
      var newClient = new Client() {
        FirstName = "John",
        LastName = "Atten",
        Email = "jatten@example.com"
      };
      _Clients.Add(newClient);
      int idToFind = newClient.ClientId;

      _Clients.Reload();
      var found = _Clients.FirstOrDefault(c => c.ClientId == idToFind);
      // After insert, no new record should be added:
      int initialCount = _Clients.Count();
      _Clients.Remove(found);
      _Clients.Reload();
      Assert.True(_Clients.Count < initialCount);
    }


    [Fact(DisplayName = "Deletes a range of records")]
    public void Deletes_Range() {
      var rangeToAdd = new List<Client>();
      for (int i = 0; i < _qtyInserted; i++) {
        var newClient = new Client() {
          FirstName = string.Format("John{0}", i.ToString()),
          LastName = "Atten",
          Email = string.Format("jatten@example{0}.com", i.ToString())
        };
        rangeToAdd.Add(newClient);
      }

      int qtyAdded = _Clients.AddRange(rangeToAdd);
      _Clients.Reload();
      int initialCount = _Clients.Count;
      var removeThese = _Clients.Where(c => c.Email.Contains("jatten@"));
      _Clients.RemoveSet(removeThese);
      Assert.True(_Clients.Count < initialCount);
    }


    // HELPER METHODS:


    void SetUpClientTable() {
      bool exists = this.TableExists("clients");
      if (exists) {
        this.DropTable("clients");
      }
      this.CreateClientsTable();
    }


    void DropTable(string tableName) {
      string sql = string.Format("DROP TABLE {0}", tableName);
      var Model = new PGTable<Client>(_connectionStringName);
      Model.Execute(sql);
    }


    bool TableExists(string tableName) {
      bool exists = false;
      string select = ""
          + "SELECT * FROM information_schema.tables "
          + "WHERE table_schema = 'public' "
          + "AND  table_name = '{0}'";
      string sql = string.Format(select, tableName);
      var Model = new PGTable<dynamic>(_connectionStringName);
      var query = Model.Query<Client>(sql);
      if (query.Count() > 0) {
        exists = true;
      }
      return exists;
    }


    void CreateClientsTable()  {
      string sql = ""
      + "CREATE TABLE clients "
      + "(client_Id serial NOT NULL, "
      + "last_name Text NOT NULL, "
      + "first_name Text NOT NULL, "
      + "email Text NOT NULL, "
      + "CONSTRAINT client_pkey PRIMARY KEY (client_Id))";

      var Model = new PGTable<Client>(_connectionStringName);
      Model.Execute(sql);
    }


    //[Trait("Basic CRUD for List","")]
    //public class LIstCRUD {
    //  PGList<Actor> actors;
    //  public LIstCRUD() {
    //    actors = new PGList<Actor>("chinookPGPG", "actor", "actor_id");
    //  }

    //  [Fact(DisplayName = "Adds all items in table to list")]
    //  public void AddsAllItemsToList() {
    //    Assert.True(actors.Count() > 199);
    //  }

    //  [Fact(DisplayName = "Adds an Actor to the DB")]
    //  public void AddsAnActor() {
    //    var actor = new Actor { First_Name = "Rob", Last_Name = "Conery" };
    //    actors.Add(actor);
    //    Assert.True(actors.Count > 200);
    //  }
    //  [Fact(DisplayName = "Sets the Actor_ID after insert")]
    //  public void SetsNewId() {
    //    var actor = new Actor { First_Name = "Rob", Last_Name = "Conery" };
    //    actors.Add(actor);
    //    Assert.True(actor.Actor_ID > 200);
    //  }
    //  [Fact(DisplayName = "Updates the record")]
    //  public void UpdatesRecord() {
    //    var actor = new Actor { First_Name = "Rob", Last_Name = "Conery" };
    //    actors.Add(actor);
    //    actor.First_Name = "JoeJoe";
    //    var didUpdate = actors.Update(actor);
    //    Assert.Equal(1, didUpdate);
    //  }

    //  [Fact(DisplayName = "Bulk Inserts")]
    //  public void BulkInserts() {
    //    int INSERT_QTY = 1000;
    //    var inserts = new List<Actor>();
    //    for (int i = 0; i < INSERT_QTY; i++)
    //    {
    //      inserts.Add(new Actor { First_Name = "Actor " + i, Last_Name = "Be Sure To Delete Me" });
    //    }
    //    var inserted = actors.AddRange(inserts);
    //    Assert.Equal(INSERT_QTY, inserted);
    //  }

    //  [Fact(DisplayName = "Deletes by range")]
    //  public void DeletesWhere() {
    //    var toRemove = actors.Where(x => x.Last_Name == "Be Sure To Delete Me");
    //    actors.RemoveSet(toRemove);
    //  }
    //}
  }
}
