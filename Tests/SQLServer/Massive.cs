using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy;
using Biggy.SQLServer;
using Xunit;

namespace Tests
{
  public class Transaction
  {
    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Comment { get; set; }
    public string Identifier { get; set; }
  }

  [Trait("Massive", "")]
  public class Massive
  {
    MassiveSetup _setup = new MassiveSetup();
    static string _connectionStringName = MassiveSetup.CONNECTION_STRING_NAME;
    static string _testTableName = MassiveSetup.TEST_TABLE_NAME;
    static string _tablePkColumn = MassiveSetup.TABLE_PK_COLUMN;
    DBTable<Transaction> _model = null;


    [Fact(DisplayName = "Test Table Exists")]
    public void _Test_Table_Exists()
    {
      _setup.CheckSetUp();
      bool exists = _setup.TransactionTableExists();
      Assert.True(exists);
    }

    [Fact(DisplayName = "Inserts a Single Strongly-Typed Record")]
    public void _Inserts_Single_Typed_Record()
    {
      _setup.CheckSetUp();
      var newRecord = new Transaction()
      {
        Amount = 100,
        Comment = "I Overspent!",
        Identifier = "XXX"
      };
      var model = new SQLServerTable<Transaction>(_connectionStringName, _testTableName, _tablePkColumn);
      var inserted = model.Insert(newRecord);
      Assert.True(newRecord.TransactionId > 0);     
    }


    [Fact(DisplayName = "Inserts a Single Anonymous Record")]
    public void _Inserts_Single_Anonymous_Record()
    {
      _setup.CheckSetUp();
      dynamic newRecord = new
      {
        Amount = 100,
        Comment = "I Anonymously Overspent!",
        Identifier = "YYZ" // Bah da-bah-bah-bah da bah-bah-bah-bah
      };
      var model = new SQLServerTable<Transaction>(_connectionStringName, _testTableName, _tablePkColumn);
      var inserted = model.Insert(newRecord);
      Assert.True(inserted.TransactionId > 0);
    }


    [Fact(DisplayName = "Inserts 12 metric crap-loads of new records")]
    public void _Inserts_Bulk_Records()
    {
      _setup.CheckSetUp();
      int qty = 10000;
      var newTransactions = _setup.getSkinnyTransactionSet(qty);
      var model = new SQLServerTable<Transaction>(_connectionStringName, _testTableName, _tablePkColumn);
      int inserted = model.BulkInsert(newTransactions);
      Assert.True(inserted == qty);
    }

    [Fact(DisplayName = "Updates a Strongly-typed record")]
    public void _Updates_Typed_Record()
    {
      _setup.CheckSetUp();
      var model = new SQLServerTable<Transaction>(_connectionStringName, _testTableName, _tablePkColumn);
      var newRecord = new Transaction()
      {
        Amount = 100,
        Comment = "I Overspent!",
        Identifier = "XXX"
      };

      // Dump the new record in as an UPDATE:
      model.Insert(newRecord);
      int recordPk = newRecord.TransactionId;

      string newValue = "I changed it!";
      newRecord.Identifier = newValue;
      int updated = model.Update(newRecord);

      newRecord = model.Find<Transaction>(recordPk);

      Assert.True(updated > 0 && newRecord.Identifier == newValue);
    }


    //[Fact(DisplayName = "Updates an Anonymously-typed record")]
    //public void _Updates_Anonymous_Record()
    //{
    //  _setup.CheckSetUp();
    //  var model = new SQLServerTable<Transaction>(_connectionStringName, _testTableName, _tablePkColumn);
    //  var newRecord = new Transaction
    //  {
    //    Amount = 100,
    //    Comment = "I Anonymously Overspent!",
    //    Identifier = "YYZ" // Bah da-bah-bah-bah da bah-bah-bah-bah da da . . .
    //  };
    //  var result = model.Insert(newRecord);
    //  int recordPk = result.TransactionId;

    //  var updateThis = new
    //  {
    //    Identifier = "I changed it!"
    //  };
    //  int updated = model.Update(updateThis);

    //  // Retrieve the updated item from the Db:
    //  var updatedRecord = model.Find<Transaction>(recordPk);

    //  Assert.True(updated > 0 && updatedRecord.Identifier == updateThis.Identifier);
    //}


    [Fact(DisplayName = "Deletes a Strongly-typed record")]
    public void _Deletes_Typed_Record()
    {
      _setup.CheckSetUp();
      var model = new SQLServerTable<Transaction>(_connectionStringName, _testTableName, _tablePkColumn);
      var newRecord = new Transaction()
      {
        Amount = 100,
        Comment = "I Overspent!",
        Identifier = "XXX"
      };
      model.Insert(newRecord);
      int recordPk = newRecord.TransactionId;

      newRecord = model.Find<Transaction>(recordPk);
      int deleted = model.Delete(newRecord.TransactionId);
      newRecord = model.Find<Transaction>(recordPk);

      Assert.True(deleted > 0 && newRecord == null);
    }


    //[Fact(DisplayName = "Deletes an Anonymously-typed record")]
    //public void _Deletes_Anonymous_Record()
    //{
    //  _setup.CheckSetUp();
    //  var model = new SQLServerTable<Transaction>(_connectionStringName, _testTableName, _tablePkColumn);
    //  var newRecord = new 
    //  {
    //    Amount = 100,
    //    Comment = "I Anonymously Overspent!",
    //    Identifier = "YYZ" // Bah da-bah-bah-bah da bah-bah-bah-bah
    //  };
    //  //HACK - you can't interrogate the new record like you were doing...
    //  var result = model.Insert(newRecord);
    //  //WTF WHY IS THIS COMING BACK AS A DECIMAL
    //  var recordPk = result.TransactionId;

    //  // Retrieve the updated item from the Db:
    //  var recordToDelete = model.Find<Transaction>(recordPk);
    //  int deleted = model.Delete(recordToDelete.TransactionId);
    //  recordToDelete = model.Find<Transaction>(recordPk);

    //  Assert.True(deleted > 0 && recordToDelete == null);
    //}


    [Fact(DisplayName = "Selects a single Strongly-typed record")]
    public void _Selects_Single_Typed_Record_By_Pk()
    {
      _setup.CheckSetUp();
      int qty = 100;
      var newTransactions = _setup.getSkinnyTransactionSet(qty);
      var model = new SQLServerTable<Transaction>(_connectionStringName, _testTableName, _tablePkColumn);
      int inserted = model.BulkInsert(newTransactions);

      int findRecordPk = 50;
      var newRecord = model.Find<Transaction>(findRecordPk);
      Assert.True(newRecord != null);
    }


    [Fact(DisplayName = "Selects a single Anonymously-typed record")]
    public void _Selects_Singles_Anonymous_Record_By_PK()
    {
      _setup.CheckSetUp();
      int qty = 100;
      var newTransactions = _setup.getSkinnyTransactionSet(qty);
      var model = new SQLServerTable<Transaction>(_connectionStringName, _testTableName, _tablePkColumn);
      int inserted = model.BulkInsert(newTransactions);

      int findRecordPk = 50;
      var newRecord = model.Find<Transaction>(findRecordPk);
      Assert.True(newRecord != null);
    }


    //// UTILITY METHODS:
    //void DropTransctionTable()
    //{
    //  string sql = ""
    //  + "DROP TABLE Transactions ";
    //  var Model = new DynamicModel(_connectionStringName);
    //  Model.Execute(sql);
    //}


    //void CreateTransctionTable()
    //{
    //  string sql = ""
    //  + "CREATE TABLE Transactions "
    //  + "(TransactionId int IDENTITY(1,1) PRIMARY KEY NOT NULL, "
    //  + "Amount Money NOT NULL, "
    //  + "Comment Text NOT NULL, "
    //  + "Identifier Text NOT NULL)";

    //  var Model = new DynamicModel(_connectionStringName);
    //  Model.Execute(sql);
    //}


    //bool TransactionTableExists()
    //{
    //  bool exists = false;
    //  string sql = ""
    //      + "SELECT * FROM INFORMATION_SCHEMA.TABLES "
    //      + "WHERE TABLE_SCHEMA = 'dbo' "
    //      + "AND  TABLE_NAME = 'Transactions'";
    //  var Model = new DynamicModel(_connectionStringName);
    //  var query = Model.Query(sql);
    //  if (query.Count() > 0)
    //  {
    //    exists = true;
    //  }
    //  return exists;
    //}

    //List<Transaction> getSkinnyTransactionSet(int qty)
    //{
    //  Console.WriteLine(Environment.NewLine);
    //  Console.WriteLine("LOAD SKINNY TRANSACTION SET");
    //  var transactions = new List<Transaction>();
    //  for (int i = 1; i <= qty; i++)
    //  {
    //    var newTrans = new Transaction()
    //    {
    //      Amount = i,
    //      Comment = "Transaction no. " + i.ToString(),
    //      Identifier = "AA-" + i.ToString()
    //    };
    //    transactions.Add(newTrans);
    //  }
    //  return transactions;
    //}
  }
}
