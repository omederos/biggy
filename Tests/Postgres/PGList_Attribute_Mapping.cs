using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Biggy;
using Biggy.Postgres;

namespace Tests.Postgres
{
  [Trait("Database Column Mapping with Attributes", "")]
  public class PGList_Attribute_Mapping {

    // WHAT DOES THIS DO? 
    // Runs basic CRUD against a table with Mismatched column names explicitly mapped to object using custom attribute

    public string _connectionStringName = "chinookPG";
    PGList<MismatchedClient> _MismatchedClients;

    public PGList_Attribute_Mapping() {
      // Set up a table with mangled column names:
      this.SetUpWTFTable();
      _MismatchedClients = new PGList<MismatchedClient>(_connectionStringName, "wtf");
	  }


    [Fact(DisplayName = "Test Table Exists")]
    public void Test_Table_Exists() {
      bool exists = this.TableExists("wtf");
      Assert.True(exists);
    }


    [Fact(DisplayName = "Loads Empty List From Table")]
    public void Loads_FromTable_Using_Attribute_Column_Names() {
      Assert.True(_MismatchedClients.Count == 0);
    }


    [Fact(DisplayName = "Adds a New Record")]
    public void Adds_New_Record() {
      int initialCount = _MismatchedClients.Count;
      var newMonkey = new MismatchedClient() { 
        Last = "Jimbo", 
        First = "Jones",
        EmailAddress = "jatten@example.com"
      };
      _MismatchedClients.Add(newMonkey);
      int newID = newMonkey.Id;
      _MismatchedClients = new PGList<MismatchedClient>(_connectionStringName, "wtf");
      var found = _MismatchedClients.FirstOrDefault(c => c.Id == newID);
      Assert.True(found.Id == newID && _MismatchedClients.Count > initialCount);
    }


    [Fact(DisplayName = "Updates a Record")]
    public void Updates_Record() {
      var newMonkey = new MismatchedClient() { 
        Last = "Jones", 
        First = "Davey",
        EmailAddress = "jatten@example.com"
      };
      _MismatchedClients.Add(newMonkey);
      int currentCount = _MismatchedClients.Count;
      int newID = newMonkey.Id;
      _MismatchedClients = new PGList<MismatchedClient>(_connectionStringName, "wtf");
      var found = _MismatchedClients.FirstOrDefault(c => c.Id == newID);
      found.First = "Mick";
      _MismatchedClients.Update(found);
      Assert.True(found.Id == newID && _MismatchedClients.Count == currentCount);
    }


    int _qtyInserted = 100;
    [Fact(DisplayName = "Bulk Inserts Records")]
    public void Bulk_Inserts_Records() {
      int initialCount = _MismatchedClients.Count();
      var rangeToAdd = new List<MismatchedClient>();
      for(int i = 0; i < _qtyInserted; i++) {
        var newCustomer = new MismatchedClient() { 
          First = string.Format("John{0}", i.ToString()), 
          Last = "Atten",
          EmailAddress = "jatten@example.com"
        };
        rangeToAdd.Add(newCustomer);
      }
      int qtyAdded = _MismatchedClients.AddRange(rangeToAdd);
      _MismatchedClients.Reload();
      Assert.True(_MismatchedClients.Count == initialCount + _qtyInserted);
    }


    [Fact(DisplayName = "Deletes a record")]
    public void Deletes_Record() {
      var newCustomer = new MismatchedClient() {
        First = "John",
        Last = "Atten",
        EmailAddress = "jatten@example.com"
      };
      _MismatchedClients.Add(newCustomer);
      int idToFind = newCustomer.Id;

      _MismatchedClients.Reload();
      var found = _MismatchedClients.FirstOrDefault(c => c.Id == idToFind);
      // After insert, no new record should be added:
      int initialCount = _MismatchedClients.Count();
      _MismatchedClients.Remove(found);
      _MismatchedClients.Reload();
      Assert.True(_MismatchedClients.Count < initialCount);
    }


    [Fact(DisplayName = "Deletes a range of records by Criteria")]
    public void Deletes_Range_Of_Records() {
      var newMismatchedClient = new MismatchedClient() {
        First = "John",
        Last = "Atten",
        EmailAddress = "jatten@example.com"
      };
      _MismatchedClients.Add(newMismatchedClient);

      _MismatchedClients.Reload();
      int initialCount = _MismatchedClients.Count;
      var removeThese = _MismatchedClients.Where(c => c.EmailAddress.Contains("jatten@example.com"));
      _MismatchedClients.RemoveSet(removeThese);
      Assert.True(_MismatchedClients.Count < initialCount);
    }


    // HELPER METHODS:


    void SetUpWTFTable() {
      bool exists = this.TableExists("wtf");
      if (exists) {
        this.DropTable("wtf");
      }
      this.CreateWTFTable();
    }


    void DropTable(string tableName) {
      string sql = string.Format("DROP TABLE {0}", tableName);
      var Model = new PGTable<dynamic>(_connectionStringName);
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
      var query = Model.Query<dynamic>(sql);
      if (query.Count() > 0) {
        exists = true;
      }
      return exists;
    }


    void CreateWTFTable() {
      string sql = ""
      + "CREATE TABLE wtf "
      + "(\"CLient_Id\" serial NOT NULL, "
      + "\"Last Name\" Text NOT NULL, "
      + "\"first_name\" Text NOT NULL, "
      + "\"Email\" Text NOT NULL, "
      + "CONSTRAINT wtf_pkey PRIMARY KEY (\"CLient_Id\"))";

      var Model = new PGTable<dynamic>(_connectionStringName);
      Model.Execute(sql);
    }
  }
}
