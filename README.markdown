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
var newClown = new Clown{Name : "Dougy Buns", Birthday = DateTime.Today().AddDays(-100)};
clowns.Add(newClown);

```

The above code will do a number of things:

 - Creates a table called "clowns" in the database with an integer primary key (auto-incrementing). This table has a 

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


## NuGet

Yes, it's on NuGet!

```
Install-Package Biggy
```

## Wanna Help?

Please do! This is rough stuff that I put together one morning just wondering if I could do it. I'd love your help if you're game.



