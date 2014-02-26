using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Postgres;
using Xunit;

namespace Tests.Postgres {

  [Trait("PG Documents","")]
  public class PGDocs {

    PGDocumentList<CustomerDocument> docs;
    public PGDocs() {
      docs = new PGDocumentList<CustomerDocument>("dvds");
      //drop and reload
      docs.Clear();
    }

    [Fact(DisplayName = "Creates a table if none exists")]
    public void CreatesTable() {
      Assert.True(docs.Count() == 0);
    }

    [Fact(DisplayName = "Adds a document")]
    public void AddsDocument() {
      var newCustomer = new CustomerDocument { Email = "rob@tekpub.com", First = "Rob", Last = "Conery" };
      docs.Add(newCustomer);
      Assert.Equal(1, docs.Count);
    }

    [Fact(DisplayName = "Updates a document")]
    public void UpdatesDocument() {
      var newCustomer = new CustomerDocument { Email = "rob@tekpub.com", First = "Rob", Last = "Conery" };
      docs.Add(newCustomer);
      newCustomer.First = "Bill";
      var updated = docs.Update(newCustomer);
      Assert.Equal(1, updated);
    }

    [Fact(DisplayName = "Deletes a document")]
    public void DeletesDocument() {
      var newCustomer = new CustomerDocument { Email = "rob@tekpub.com", First = "Rob", Last = "Conery" };
      docs.Add(newCustomer);
      var removed = docs.Remove(newCustomer);
      Assert.True(removed);
    }
  }
}
