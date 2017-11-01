using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy.Postgres {
  public class PGList<T> : DBList<T> where T : new(){

    protected PGTable<T> PGModel {
      get {
        return (PGTable<T>)this.Model;
      }
    }

    public override void SetModel() {
      this.Model = new PGTable<T>(this.ConnectionStringName, this.TableName);
    }

    public PGList(string connectionStringName, string tableName = "guess") :
      base(connectionStringName, tableName) { }
    
    //custom PG goodies
    public IEnumerable<T> FullTextOnTheFly(string query, params string[] columns) {
      return PGModel.FullTextOnTheFly(query, columns);
    }
    public IEnumerable<T> FullText(string query) {
      return PGModel.FullText(query);
    }

  }
}
