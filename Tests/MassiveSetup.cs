using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Massive;

namespace Tests
{
  public class MassiveSetup
  {
    public const string CONNECTION_STRING_NAME = "northwind";
    public const string TEST_TABLE_NAME = "Transactions";
    public const string TABLE_PK_COLUMN = "TransactionId";

    public void CheckSetUp()
    {
      var setup = new MassiveSetup();
      bool exists = setup.TransactionTableExists();
      if (!exists)
      {
        this.CreateTransctionTable();
        exists = this.TransactionTableExists();
      }
      else
      {
        this.DropTransctionTable();
        this.CreateTransctionTable();
        exists = this.TransactionTableExists();
      }
    }



    // UTILITY METHODS:
    void DropTransctionTable()
    {
      string sql = ""
      + "DROP TABLE Transactions ";
      var Model = new DynamicModel(CONNECTION_STRING_NAME);
      Model.Execute(sql);
    }


    void CreateTransctionTable()
    {
      string sql = ""
      + "CREATE TABLE Transactions "
      + "(TransactionId int IDENTITY(1,1) PRIMARY KEY NOT NULL, "
      + "Amount Money NOT NULL, "
      + "Comment Text NOT NULL, "
      + "Identifier Text NOT NULL)";

      var Model = new DynamicModel(CONNECTION_STRING_NAME);
      Model.Execute(sql);
    }


    public bool TransactionTableExists()
    {
      bool exists = false;
      string sql = ""
          + "SELECT * FROM INFORMATION_SCHEMA.TABLES "
          + "WHERE TABLE_SCHEMA = 'dbo' "
          + "AND  TABLE_NAME = 'Transactions'";
      var Model = new DynamicModel(CONNECTION_STRING_NAME);
      var query = Model.Query(sql);
      if (query.Count() > 0)
      {
        exists = true;
      }
      return exists;
    }

    public List<Transaction> getSkinnyTransactionSet(int qty)
    {
      Console.WriteLine(Environment.NewLine);
      Console.WriteLine("LOAD SKINNY TRANSACTION SET");
      var transactions = new List<Transaction>();
      for (int i = 1; i <= qty; i++)
      {
        var newTrans = new Transaction()
        {
          Amount = i,
          Comment = "Transaction no. " + i.ToString(),
          Identifier = "AA-" + i.ToString()
        };
        transactions.Add(newTrans);
      }
      return transactions;
    }

  }
}
