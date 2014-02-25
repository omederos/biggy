using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Postgres;
using Xunit;

namespace Tests.Postgres {
  [Trait("Basic CRUD for List","")]
  public class LIstCRUD {
    PGList<Actor> actors;
    public LIstCRUD() {
      actors = new PGList<Actor>("dvds", "actor", "actor_id");
    }

    [Fact(DisplayName = "Adds all items in table to list")]
    public void AddsAllItemsToList() {
      Assert.True(actors.Count() > 199);
    }

    [Fact(DisplayName = "Adds an Actor to the DB")]
    public void AddsAnActor() {
      var actor = new Actor { First_Name = "Rob", Last_Name = "Conery" };
      actors.Add(actor);
      Assert.True(actors.Count > 200);
    }
    [Fact(DisplayName = "Sets the Actor_ID after insert")]
    public void SetsNewId() {
      var actor = new Actor { First_Name = "Rob", Last_Name = "Conery" };
      actors.Add(actor);
      Assert.True(actor.Actor_ID > 200);
    }
    [Fact(DisplayName = "Updates the record")]
    public void UpdatesRecord() {
      var actor = new Actor { First_Name = "Rob", Last_Name = "Conery" };
      actors.Add(actor);
      actor.First_Name = "JoeJoe";
      var didUpdate = actors.Update(actor);
      Assert.Equal(1, didUpdate);
    }

    [Fact(DisplayName = "Bulk Inserts")]
    public void BulkInserts() {
      var inserts = new List<Actor>();
      for (int i = 0; i < 1000; i++) {
        inserts.Add(new Actor { First_Name = "Actor " + i, Last_Name = "Be Sure To Delete Me" });
      }
      var inserted = actors.AddRange(inserts);
      Assert.Equal(1000, inserted);
    }

    [Fact(DisplayName = "Deletes by range")]
    public void DeletesWhere() {
      var toRemove = actors.Where(x => x.Last_Name == "Be Sure To Delete Me");
      actors.RemoveSet(toRemove);
    }
  }
}
