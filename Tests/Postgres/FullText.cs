using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Postgres;
using Xunit;

namespace Tests.Postgres {
  [Trait("PG Full Text Searching","")]
  public class FullText {

    PGList<Film> films;
    public FullText() {
      films = new PGList<Film>("dvds", "film", "film_id");
    }

    [Fact(DisplayName = "Ad hoc TS Vector Query Returns Films")]
    public void AdHocTSVector() {

      var results = films.FullTextOnTheFly("monkey", "description", "title");
      Assert.True(results.Count() > 0);
      
    }

    [Fact(DisplayName = "Tagged full text works wonders")]
    public void TaggedFullText() {

      var results = films.FullText("monkey");
      Assert.True(results.Count() > 0);

    }
  }
}
