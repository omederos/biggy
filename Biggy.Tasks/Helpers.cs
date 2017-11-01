using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy.Perf {

  class Track {
    [PrimaryKey]
    public int TrackID { get; set; }
    public int AlbumId { get; set; }
    public string Name { get; set; }
    public string Composer { get; set; }
    public decimal UnitPrice { get; set; }
  }

  class Artist {
    [PrimaryKey]
    public int ArtistId { get; set; }
    public string Name { get; set; }
  }

  class Album {
    [PrimaryKey]
    public int AlbumId { get; set; }
    public string Title { get; set; }
    public int ArtistId { get; set; }
  }

  public class Customer {
    [PrimaryKey]
    public int CustomerId { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Email { get; set; }
  }


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
    [PrimaryKey(Auto: false)]
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    [FullText]
    public string Description { get; set; }
  }

  // Use this against a temp Clients table when doing destructive
  // or other operations to avoid blowing up the serial PK and/or
  // ditching the Chinook Data:
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

  class Monkey {
    [PrimaryKey]
    public int ID { get; set; }
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    [FullText]
    public string Description { get; set; }
  }
}
