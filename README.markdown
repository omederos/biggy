# Biggy: SQLite for Documents and .NET

This is just a goofy idea at this point, inspired by [NeDB](https://github.com/louischatriot/nedb) which is basically the same thing, but with Node.

I like the idea of SQLite (a file-based relational data-store), but wouldn't it be fun to have this kind of thing for a Document database too? One nice thing about C# (among many) is the built-in
LINQ stuff, another nice thing is that C# has Dynamics now too. **Biggy is simply an implementation of ICollection<T> with a JSON backing store**. I added a few helpy things 
in there (like events and a few other things) and this might be completely dumb but I like the idea.

Toss LINQ and Dynamics into a bowl, sprinkle with some JSON serialization and you have Biggy:

```csharp
dynamic db = new BiggyDB();
db.Clowns.Add(new { Name = "Rob Sullivan", Age = 1002 });
```
This does two things:

 - Creates a `Data` directory in your project root (you can override this) as well as a file called "clowns.json"
 - Creates an in-memory list that you can now query using LINQ like you always have (LINQ to Objects)
 
You can also run this Async if you have a lot of writes:

```csharp
dynamic db = new BiggyDB();
db.Clowns.Add(new { Name = "Thingy Bob Joe Bill", Age = 1002 });
```

## It's Not All Dynamic

You can also use Biggy in your normal typey-type ways:

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
var products = new BiggyList<Product>();
//it's all LINQ since you're querying an in-memory list
var p = products.FirstOrDefault(x => x.Sku == "THINGY");
p.Name = "Something Fun";
//writes asynchronously to the backing store in Data/products.json
products.Update(p);
```

The list infers a few things for you:

 - Your project root (and therefore the data directory... which you can overwrite)
 - Your db name (based on the type you're using - in this case this db would be called `products`)

Secondly - notice how `Equals()` is overridden here? This is so that IEnumerable stuff in C# can know whether it's dealing with the same object. If you override `Equals` as I have here,
you can use `Add()` and it acts like an Upsert.

Basically what I'm saying is that **Biggy is simply a List implementation with disk persistence**.

## Some Sample Web Code

There's a sample web app in the repo - have a look. I've scaffolded out `Product` for you so you can see a working example. The first step is to create a DB wrapper class. Since we're using in-memory lists, a static wrapper with static properties should work well so we're not hitting the disk constantly:

```csharp

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Biggy;

namespace Web.Models {
  
  public static class DB {

    static BiggyList<Product> _products;
    static BiggyList<Customer> _customers;

    public static void Load(string appPath) {
      _products = new BiggyList<Product>(dbPath : appPath);
      _customers = new BiggyList<Customer>(dbPath: appPath);
    }

    public static BiggyList<Product> Products {
      get {
        return _products;
      }
    }
    public static BiggyList<Customer> Customers {
      get {
        return _customers;
      }
    }
  }
}
```

Here I've added a "Load" routine that you can call explicitly when your app starts. This will load all data on disk (which is a pretty fast read) and you're off to the the races.

Biggy won't know which directory to use and will default to the current execution path - so you'll want to set that. You can use Global.asax in `App_Start` if you like:

```csharp
protected void Application_Start()
{
    //...
    DB.Load(HttpRuntime.AppDomainAppPath);
}
```

This will send in your current web root, and you'll be good to go.



## Some Caveats

Every time you instantiate a list, it tries to read it's data from disk. This is a one-time read on instantiation, but if you have a lot of data this can be a perf-killer (among other things). You may want to instantiate your list when your program starts up and then pass a reference to it throughout your app. 

Each list type has it's own file for performance reasons. 


## What It's Good For

The only disk activity occurs when you call "Save()" and when you instantiate the List itself - everything else happens in memory. This makes Biggy incredibly fast but it also means we're doing file 
management - which can be tricky.

**This is one place that I hope I can get a PR for** - I'm dropping the entire contents to disk on every save and YES if you try this will millions of records it will probably cause you some problems. But with 
100 or so, it shouldn't be that big of a problem.

That makes Biggy compelling for high-read situations, such as a blog, product catalog, etc. At least that's what I've used NeDB for and it works great.

## Performance

In the Tasks project (a Console app) there are simple loops that write 1000 records to disk at once (in a batch) as well as a simple read. You can see the results for yourself... they are OK.

Writing 1000 records in a batch takes about 30ms (give or take), writing in a loop takes about 4 seconds (!), but reading records out is too small to record :):):).

There's a lot to do to make this a bit more functional, but for now it does what I envisioned.


## Wanna Help?

Please do! This is rough stuff that I put together one morning just wondering if I could do it. I'd love your help if you're game.




