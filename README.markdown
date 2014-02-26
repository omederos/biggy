# Biggy: A Very Fast Document/Relational Query Tool with Full LINQ Compliance

I like working with Document databases, and I like working with relational ones. I like LINQ, I like Postgres, and sometimes I just want to store data on disk in a JSON file: **so I made Biggy**.

This project started life as an implementation of ICollection<T> that persisted itself to a file using JSON seriliazation. That quickly evolved into using Postgres as a JSON store, and then SQL Server. What we ended up with is the fastest data tool you can use.

Data is loaded into memory when your application starts, and you query it with Linq. That's it. It loads incredibly fast (100,000 records in about 1 second) and from there will sync your in-memory list with whatever store you choose. 

## Database Document Storage

Biggy supports both SQL Server and Postgres - but we develop with Postgres first so there are a few more bells and whistles for this amazing database (specifically: Full Text search over documents).

To define a Document Store, create an instance of DBDocumentList<T>:

```csharp
class Clown {
  public int ID {get;set;}
  public string Name {get;set;}
  [FullText]
  public string LifeStory {get;set;}
  public DateTime Birthday {get;set;}
  
  public Clown(){
    Birthday = DateTime.Today();
  }
}
var clowns = new PGDocumentList<Clown>(connectionStringName : "Northwind");
var newClown = new Clown{Name : "Dougy Buns", Birthday = DateTime.Today().AddDays(-100), LifeStory = "Once upon a time, I was a little clown"};
clowns.Add(newClown);

```

The above code will do a number of things:

 - Creates a table called "clowns" in the database with an integer primary key (auto-incrementing). 
 - Tries to load every record in the "clowns" table on instantiation
 - Creates an ICollection<Clown> (which is `clowns` itself) that you can query with LINQ as you know how already
 
The table structure under this is interesting too. We tagged the LifeStory property with the `FullText` attribute. This tells the Biggy to create a column on the "clowns" table called "search" which is of type `tsvector` - this is how Postgres indexes text (in SQL Server it's nvarchar(MAX) with a special index). 

There are 3 total columns in the "clowns" table:

 - id (integer)
 - body (json)
 - search (tsvector)

When Biggy loads the data it deserializes it into the backing store and you can access it just like any ICollection<T>. You can also query the full text engine easily:

```csharp
var clowns = new PGDocumentList<Clown>(connectionStringName : "Northwind");
var results = clowns.FullText("happy");
foreach(var clown in results){
  //play with results here
}
```

The same thing works for SQL Server.

## File-based Document Storage
If you don't want to install a database engine, you don't have to. Biggy can load and write to disk easily:

```csharp
class Product {
  public String Sku { get; set; }
  public String Name { get; set; }
  public Decimal Price { get; set; }
  public DateTime CreatedAt { get; set; }

  public Product() {
    this.CreatedAt = DateTime.Now;
  }

  public override bool Equals(object obj) {
    var p1 = (Product)obj;
    return this.Sku == p1.Sku;
  }
} 

//add and save to this list as above
//this will create a Data/products.json file in your project/site root
var products = new BiggyList<Product>();

var newProduct = new Product{Sku : "STUFF", Name : "A new product", Price : 120.00};

//gets appended immediately in a single line-write to file
products.Add(newProduct);

//this won't hit the disk as you're querying in-memory only
var p = products.FirstOrDefault(x => x.Sku == "STUFF");
p.Name = "Something Fun";

//this writes to disk in a single asynchronous flush - so it's fast too
products.Update(p);
```

You can move from the file store over to the relational store by a single type change (as well as moving data over). This makes Biggy attractive for greenfield projects and just trying stuff out.

## Good Old Relational Query Tool

The engine behind Biggy is a newer version of [Massive](http://github.com/robconery/massive) that has some type-driven love built into it. If you want to run queries and do things like you always have, go for it:

```csharp
//this should look familiar to Massive fans
var clowns = new SqlServerTable(connectionStringName= "northwind", tableName= "clowns", primaryKeyField = "ID");

//find by key
var dougy = clowns.Find<Clown>(1);

//same thing
dougy = clowns.FirstOrDefault<Clown>("id=@0",1);

//get them all
var allClowns = clowns.All<Clown>();

//get some
var someClowns = clowns.Where<Clown>("id > @0", 0);

//stop telling me what to do - returns a dynamic result
var myOwnClowns = clowns.Query("select ID, newid() as SomeGuid, Birthday from Clowns");

//add one
clowns.Add(new Clown{...});
//update
clowns.Update(someClown);
//remove
clowns.Delete(someClown);
//remove a bunch
clowns.DeleteWhere("id > 0");

```

This is using the straight-up PGTable (or SqlServerTable) - you can have in-memory performance and **full LINQ capabilities using PGList and SqlServerList**:

```csharp
var products = new PGList<Product>("northwind","products","productid");

//we've connected to Postgres and all records are read into memory - let's rock some LINQ:
var discontinued = products.Where(x => x.Discontinued == true);

```

## Hooks, Callbacks, Events

SQlServerTable and PGTable are very, very close to Massive with a few things removed - specifically the dynamic query builder and the validation stuff. You can add that in as you see fit using the hooks we've always had:

```csharp
  //Hooks
  public virtual void Inserted(T item) { }
  public virtual void Updated(T item) { }
  public virtual void Deleted(T item) { }
  public virtual bool BeforeDelete(T item) { return true; }
  public virtual bool BeforeSave(T item) { return true; }
```
Just override them as needed in your derived class:

```csharp
class ClownTable : SqlServerTable<Clown>{
  public ClownTable(connectionStringName,tableName,primaryKeyField) : base(connectionStringName,tableName,primaryKeyField);
  
  public override bool BeforeSave(Clown item){
    //do your validations here... be sure to return false if things are bad
  }
  
}

```
Using events is pretty straightforward:

```csharp
var clowns = new SqlServerTable(connectionStringName= "northwind", tableName= "clowns", primaryKeyField = "ID");
clowns.Loaded+=Clowns_Loaded;

public void ClownsLoaded(object sender, EventArgs e){
  var biggyArgs = (BiggyEventArgs)e;
  Console.WriteLine("We have {0} clowns y'all",e.Items.Count);
}

```



## What It's Good For

A document-centric, "NoSQL"-style of development is great for high-read, quick changing things. Products, Customers, Promotions and Coupons - these things get read from the database continually and it's sort of silly. Querying in-memory makes perfect sense for this use case. For these you could use one of the document storage ideas above.could 

A relational, write-oriented transactional situation is great for "slowly changing over time" records - like Orders, Invoices, SecurityLogs, etc. For this you could use a regular relational table using the PGTable or SQLServerTable as you see fit.

## Strategies

You only want to read the `InMemoryList<T>` stuff off disk once - and this should be when your app starts up. This is pretty straightforward if you're using a Console or Forms-based app, but if you're using a web app this gets more difficult.

Fortunately, you have a few nice choices.

The first is to use your IoC container to instantiate Biggy for you. For this, create a wrapper class just like you would with EF:

```csharp
  public class StoreDB {

    public BiggyList<Product> Products;
    public BiggyList<Customer> Customers;

    public StoreDB() {
      Products = new BiggyList<Product>(dbPath: HttpRuntime.AppDomainAppPath);
      Customers = new BiggyList<Customer>(dbPath: HttpRuntime.AppDomainAppPath);
    }
  }
```
Passing in the HttpRuntime.AppDomainAppPath here tells Biggy where your web root is. 

### Using Inversion of Control

If you're a fan of IoC then managing Biggy should be simple. Just make sure your wrapper class is in Singleton scope - here's how Ninject does it:

```csharp
Bind<StoreDb>().ToSelf().InSingletonScope();
```

StructureMap and other IoC containers do the same kind of thing.

### Simple Instance in App_Start

You can do the same thing with a static property on your web app class. Here's the example MVC app that's in the source:

```csharp
using System.Web.Routing;
using Biggy;
using Biggy.JSON;
using Biggy.Postgres;
using Web.Models;

namespace Web {

  public class StoreDB {

    public BiggyList<Product> Products;
    public BiggyList<Customer> Customers;

    public StoreDB() {
      Products = new BiggyList<Product>(dbPath: HttpRuntime.AppDomainAppPath);
      Customers = new BiggyList<Customer>(dbPath: HttpRuntime.AppDomainAppPath);
    }
  }


  public class MvcApplication : System.Web.HttpApplication {

    public static StoreDB StoreDB { get; set; }
    
    protected void Application_Start() {
      //load up the DB
      MvcApplication.StoreDB = new StoreDB();
      AreaRegistration.RegisterAllAreas();
      FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
      RouteConfig.RegisterRoutes(RouteTable.Routes);
      BundleConfig.RegisterBundles(BundleTable.Bundles);
      
    }
  }
}
```
It's only called once on start - and from here on out you have high-speed, in-memory querying with full LINQ and your data store can be SQL Server, Postgres, or file storage.


## A Note on Speed and Memory

Some applications have a ton of data and for that, Biggy might not be the best fit if you need to read from that ton of data consistently. We've focused on prying apart data into two camps: High Read, and High Write.

We're still solidifying our benchmarks, but in-memory read is about as fast as you can get. Our writes are getting there too - currently we can drop 100,000 documents to disk in about 2 seconds - which isn't so bad. We can write 10,000 records to Postgres and SQL Server in about 500ms - again not bad.

So if you want to log with Biggy - go for it! Just understand that if you use a `DBList<T>`, it assumes you want to read too so it will store the contents in memory as well as on disk. If you don't need this, just use a `DBTable<T>` (Postgres or SQLServer) and write your heart out.

You might also wonder about memory use. Since you're storing everything in memory - for a small web app this might be a concern. Currently the smallest, free sites on Azure allow you 1G RAM. Is this enough space for your data? [Borrowing from Karl Seguin](http://openmymind.net/redis.pdf):

> I do feel that some developers have lost touch with how little space data can take. The Complete Works of William
Shakespeare takes roughly 5.5MB of storage

The entire customer, catalog, logging, and sales history of Tekpub was around 6MB. If you're bumping up against your data limit - just move from an in-memory list to a regular table object (as shown above) and you're good to go.


## NuGet

Yes, it's on NuGet!

```
Install-Package Biggy
```

## Wanna Help?

Please do! Here's what we ask of you:

 - If you've found a bug, please log it in the Issue list. 
 - If you want to fork and fix (thanks!) - please fork then open a branch on your fork specifically for this issue. Give it a nice name.
 - Make the fix and then in your final commit message please use the Github magic syntax ("Closes #X" or Fixes etc) so we can tie your PR to you and your issue
 - Please please please verify your bug or issue with a test (we use XUnit and it's simple to get going)

Thanks so much!


