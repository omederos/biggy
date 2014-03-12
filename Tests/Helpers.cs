using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy;

namespace Tests {

  public class Film {
    [PrimaryKey]
    public int Film_ID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int ReleaseYear { get; set; }
    public int Length { get; set; }

    [FullText]
    public string FullText { get; set; }
  }


  public class MonkeyDocument {
    [PrimaryKey]
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    [FullText]
    public string Description { get; set; }
  }


  public class Client {
    [PrimaryKey]
    public int ClientId { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Email { get; set; }
  }


  public class ClientDocument {
    [PrimaryKey]
    public int ClientDocumentId { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Email { get; set; }
  }


  class MismatchedClient {
    [PrimaryKey]
    [DbColumn("CLient_Id")]
    public int Id { get; set; }
    [DbColumn("Last Name")]
    public string Last { get; set; }
    [DbColumn("first_name")]
    public string First { get; set; }
    [DbColumn("Email")]
    public string EmailAddress { get; set; }
  }
}
