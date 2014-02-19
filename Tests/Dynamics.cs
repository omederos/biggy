using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy;
using Xunit;

namespace Tests {

  [Trait("Dynamic stuff","")]
  public class Dynamics {

    dynamic db;
    public Dynamics() {
db = new BiggyDB();
db.Clowns.Add(new { Name = "Fully Dully", Age = 1002 });
db.Clowns.Save();
    }

    [Fact(DisplayName = "Returns a BiggyList of dynamic")]
    public void UsesBiggyListDynamic() {
      Assert.True(db.Clowns is BiggyList<dynamic>);
    }
    [Fact(DisplayName = "Writes based on name")]
    public void FactName() {
      Assert.True(db.Clowns.HasDbFile);
    }
    [Fact(DisplayName = "Loads just like a regular BiggyList")]
    public void LoadsLikeNormal() {
      dynamic db2 = new BiggyDB();
      Assert.True(db2.Clowns.Count > 0);

    }
  }
}
