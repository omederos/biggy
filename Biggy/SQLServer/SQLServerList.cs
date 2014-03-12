using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.SQLServer;

namespace Biggy.SQLServer {

  public class SQLServerList<T> : DBList<T> where T : new(){
    protected SQLServerTable<T> PGModel {
      get {
        return (SQLServerTable<T>)this.Model;
      }
    }

    public override void SetModel() {
      this.Model = new SQLServerTable<T>(this.ConnectionStringName, this.TableName);
    }

    public SQLServerList(string connectionStringName, string tableName = "guess") :
      base(connectionStringName, tableName) { }
  }
}
