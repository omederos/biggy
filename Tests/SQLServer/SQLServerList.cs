using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Biggy;
using Biggy.SQLServer;

namespace Tests.SQLServer
{
  [Trait("SQL Server List Basic CRUD", "")]
  public class SQLServerList_CRUD
  {
    public string _connectionStringName = "chinook";
    SQLServerList<Client> _Clients;

    // Runs before every test:
    public SQLServerList_CRUD() {
      // Drops and re-creates table each time:
      this.SetUpClientTable();
      _Clients = new SQLServerList<Client>(connectionStringName: _connectionStringName, tableName: "Clients");
    }

    [Fact(DisplayName = "Pulls things dynamically")]
    public void PullsThingsDynamically()
    {
        var list = new SQLServerList<dynamic>(_connectionStringName);
        var results = list.Query(@"select Artist.Name AS ArtistName, Track.Name, Track.UnitPrice
                                   from Artist inner join
                                   Album on Artist.ArtistId = Album.ArtistId inner join
                                   Track on Album.AlbumId = Track.AlbumId
                                   where (Artist.Name = @0)", "ac/dc");
        Assert.True(results.Count() > 0);
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
      _Clients = new SQLServerList<Client>(connectionStringName: _connectionStringName, tableName: "Clients");
      var found = _Clients.FirstOrDefault(c => c.ClientId == idToFind);
      Assert.True(found.Email == "jatten@example.com" && _Clients.Count > initialCount);
    }


    [Fact(DisplayName = "Updates a record")]
    public void Updates_Record() {
      var newClient = new Client() { 
        FirstName = "John", 
        LastName = "Atten", 
        Email = "jatten@example.com" };
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

      for(int i = 0; i < _qtyInserted; i++) {
        var newClient = new Client() { 
          FirstName = string.Format("John{0}", i.ToString()), 
          LastName = "Atten", 
          Email = string.Format("jatten@example{0}.com", i.ToString()) };
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
      bool exists = this.TableExists("Clients");
      if (exists) {
        this.DropTable("Clients");
      }
      this.CreateClientsTable();
    }


    void DropTable(string tableName) {
      string sql = string.Format("DROP TABLE {0}", tableName);
      var Model = new SQLServerTable<Client>(_connectionStringName);
      Model.Execute(sql);
    }


    bool TableExists(string tableName) {
      bool exists = false;
      string select = ""
          + "SELECT * FROM INFORMATION_SCHEMA.TABLES "
          + "WHERE TABLE_SCHEMA = 'dbo' "
          + "AND  TABLE_NAME = '{0}'";
      string sql = string.Format(select, tableName);
      var Model = new SQLServerTable<dynamic>(_connectionStringName);
      var query = Model.Query<Client>(sql);
      if (query.Count() > 0) {
        exists = true;
      }
      return exists;
    }


    void CreateClientsTable() {
      string sql = ""
      + "CREATE TABLE Clients "
      + "(ClientId int IDENTITY(1,1) PRIMARY KEY NOT NULL, "
      + "[LastName] Text NOT NULL, "
      + "firstName Text NOT NULL, "
      + "Email Text NOT NULL)";

      var Model = new SQLServerTable<Client>(_connectionStringName);
      Model.Execute(sql);
    }

  }
}
