    Title: SQL Provider - new stuff including PostgreSQL support and option types!
    Date: 2013-12-24T09:21:00
    Tags: fsharp, type providers , sqlprovider


<p>The latest version of the SQL provider can be found on <a href="https://github.com/pezipink/SQLProvider">github </a>or at <a href="https://www.nuget.org/packages/SQLProvider/0.0.3-alpha">nuget</a>.  (EDIT: MYSQL and Oacle are now a supported as well!)</p>
<h2>PostgreSQL</h2>
<p>The provider now has support for connecting to PostgreSQL databases! How exciting. Like SQLite, this new mode uses dynamic assembly loading and is based on the .NET connector libraries <a href="http://npgsql.projects.pgfoundry.org/">here</a>. </p>
<!-- more -->
<p>Once you have your Npgsql libraries you can connect yourself up easily as so:</p>
```fsharp
type sql = SqlDataProvider< @"Server=localhost;Port=5432;Database=world;user=postgres;password=sekret;",
                      Common.DatabaseProviderTypes.POSTGRESQL, @"F:\postgre\f#\Npgsql" >

let ctx = sql.GetDataContext()

// access individuals
let southend = ctx.``[public].[city]``.Individuals.``As name``.``486, Southend-on-Sea``

// get all cities
ctx.``[public].[city]`` |> Seq.toArray

// query some cities
query { for x in ctx.``[public].[city]`` do
        where (x.name =%"%s%")
        select x
        take 100 } |> Seq.toArray
```

<p>The whole shooting match should mostly work, including relationship navigation, individuals and complex queries. Unfortunately the Ngpsql connector doesn't give any type mapping information so it had to be done the old fashioned way (e.g, me writing out long match expressions, which was fun). Postgre lets you have some really crazy data types so I would not be surprised if you don't get all your columns depending on what your database schema is like.</p>
<h2 class="brush: c-sharp;">Option Types</h2>
<p>There is a new static parameter called <em>UseOptionTypes. </em>If you flip this little puppy to true, all columns in the database that are marked as nullable will now be generated as F# option types, woop! This means that in your queries you will have to access the .Value property when supplying criteria and join information. You can also quite handily use the IsSome and IsNone properties in a criteria expression, which will be translated to IS NOT NULL, IS NULL, or whatever the equivalent is for the specific database vendor you are using.</p>
```fsharp
type sql = SqlDataProvider< @"Data Source=F:\sqlite\northwindEF.db ;Version=3", 
        Common.DatabaseProviderTypes.SQLITE, @"F:\sqlite\3", 1000, true >

let ctx = sql.GetDataContext()
query { for c in ctx.``[main].[Customers]`` do
        // IS NOT NULL
        where (c.CompanyName.IsSome)
        // IS NULL
        where (c.CompanyName.IsNone)
        // Usual criteria is now specified with the Value property
        where (c.CompanyName.Value =% "%TESCO%")
        // obviously you will need to be careful in projections with option types..
        select (c.CompanyName, c.CompanyName.Value) } |> Seq.toArray
```

<p></p>
<h2>Documentation!</h2>
<p>I still have not written any. Sorry.</p>
<p></p>