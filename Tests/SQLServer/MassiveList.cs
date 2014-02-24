using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Biggy;
using Biggy.SQLServer;

namespace Tests
{
  [Trait("Massive", "")]
  public class MassiveList
  {
    MassiveSetup _setup = new MassiveSetup();

    static string _connectionStringName = MassiveSetup.CONNECTION_STRING_NAME;
    static string _testTableName = MassiveSetup.TEST_TABLE_NAME;
    static string _tablePkColumn = MassiveSetup.TABLE_PK_COLUMN;
    SQLServerList<Transaction> _transactions = null;
    int _qtyCrapTons = 10000;

    public MassiveList()
    {
      // Make sure tests can run independently:
      _setup.CheckSetUp();
      _transactions = new SQLServerList<Transaction>(_connectionStringName, _testTableName, _tablePkColumn);
    }


    [Fact(DisplayName = "Loads Empty list from Db into memory")]
    public void _Loads_Empty_List_Into_Memory()
    {
      _transactions = new SQLServerList<Transaction>(_connectionStringName, _testTableName, _tablePkColumn);
      Assert.True(_transactions != null && _transactions.Count() == 0);
    }


    [Fact(DisplayName = "Writes 12 metric crap-loads of records into memory, and into Db")]
    public void _Writes_large_DataSet_to_Memory()
    {
      if (_transactions.Count() > 0)
      {
        _transactions.Clear();
      }
      var data = _setup.getSkinnyTransactionSet(_qtyCrapTons);

      // TODO: Should the flush back to the Db be implicit, or explicit?
      // It is currently implicit, and happens automatically. Maybe change this. 
      _transactions.AddRange(data);
      Assert.True(_transactions.Count == _qtyCrapTons);
    }


    [Fact(DisplayName = "Loads a single record from memory")]
    public void _Loads_Single_Record_From_Memory()
    {
      if(_transactions.Count() == 0)
      {
        var data = _setup.getSkinnyTransactionSet(_qtyCrapTons);
        _transactions.AddRange(data);
      }

      var idToFind = 5001;
      var trans = _transactions.First(t => t.TransactionId == idToFind);
      Assert.True(trans != null);
    }


    [Fact(DisplayName = "Queries a range of records from memory")]
    public void _Queries_Range_From_Memory()
    {
      if (_transactions.Count() == 0)
      {
        var data = _setup.getSkinnyTransactionSet(_qtyCrapTons);
        _transactions.AddRange(data);
      }

      var transactions = from t in _transactions where t.TransactionId >= 500 && t.TransactionId <= 1000 select t;
      Assert.True(transactions.Count() > 0);
    }


    [Fact(DisplayName = "Updates a single record in memory")]
    public void _Update_Record_In_Memory()
    {
      if (_transactions.Count() == 0)
      {
        var data = _setup.getSkinnyTransactionSet(_qtyCrapTons);
        _transactions.AddRange(data);
      }
      string updateData = "I updated this!";
      var idToFind = 5001;
      var trans = _transactions.First(t => t.TransactionId == idToFind);
      trans.Identifier = updateData;
      trans = _transactions.First(t => t.TransactionId == idToFind);
      Assert.True(trans != null && trans.Identifier == updateData);
    }


    [Fact(DisplayName = "Deletes a single record in memory")]
    public void _Delete_Record_In_Memory()
    {
      _setup.CheckSetUp();
      var data = _setup.getSkinnyTransactionSet(_qtyCrapTons);
      _transactions.AddRange(data);

      var idToFind = 5001;
      var trans = _transactions.First(t => t.TransactionId == idToFind);
      _transactions.Remove(trans);
      _transactions.Reload();
      Assert.True(_transactions.Count == _qtyCrapTons -1);
    }







  }
}
