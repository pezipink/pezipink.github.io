    Title: CRUD Operations and Experimental ODBC support in the SQLProvider
    Date: 2014-05-18T05:22:00
    Tags: fsharp, sqlprovider, type providers

<p>The SQL provider now supports basic transactional CRUD functionality and an ODBC provider. A new nuget package is up for you to grab <a href="https://www.nuget.org/packages/SQLProvider/0.0.9-alpha">here</a>. As always, you can download and build from source <a href="https://github.com/fsprojects/SQLProvider">here</a>.</p>
<p>The nuget package is still pre-release. You can find it in Visual Studio by toggling the search filter to include pre-release packages. I'm sure Xamarin has a similar feature. Once this work has been tested well enough, I will likely upgrade the SQL Provider to a proper release.</p>
<!-- more -->

<h2>Experimental ODBC Support</h2>
<p>We now have support for general ODBC connectivity in the SQL provider. This provides us with awesome power to connect to almost anything, including Excel spreadsheets. However, because each driver can vary substantially, not all drivers may work, and others may have reduced functionality. ODBC is provided in the .NET core libraries, although you will of course need the appropriate ODBC driver installed on your machine for it to function.</p>
<p>ODBC has Where, Select and Join support. If the source in question provides primary key information, you will also get Individuals. Although explicit joins are supported, foreign key constraint information and navigation are not yet available. The new CRUD operations are also supported, where appropriate. We will see how this works from ODBC later in the post.</p>
```fsharp
[<Literal>] 
let excelCs = @"Driver={Microsoft Excel Driver (*.xls)};DriverId=790;Dbq=I:\test.xls;DefaultDir=I:\;" 
type xl = SqlDataProvider<excelCs, Common.DatabaseProviderTypes.ODBC>
```

<p>You can use the new features with the following providers :</p>
<ul>
<li>SQL Server</li>
<li>SQLite</li>
<li>PostgreSQL</li>
<li>MySQL</li>
<li>Oracle</li>
<li>ODBC (limited)</li>
</ul>
<p>The following examples will use SQLite, but the mechanics are identical for all the providers.</p>
<h2>CRUD Operations</h2>
<p>IMPORTANT IMFORMATION ON RESTRICTIONS!</p>
<p><em>The CRUD operations will only work for tables that have a well-defined, non composite primary key. Identity keys are fully supported.</em></p>
<p>The data context is the core object that enables CRUD. It is important to understand that each data context you create from the type provider will track all entities that are modified from it. In a sense, the data context &ldquo;owns&rdquo; all of the entities returned by queries or that you have created. You can create more than one data context that have different connection strings at runtime. You can pass a different connection string in the GetDataContext method. This is an important concept as it allows you to connect to multiple instances of the same database, and even replicate data between them fairly easily.</p>
```fsharp
type sql = SqlDataProvider< ConnectionString, Common.DatabaseProviderTypes.SQLITE, ResolutionPath > 
let ctx = sql.GetDataContext() 
let ctx2 = sql.GetDataContext("some other connection string")
```

<p>whenever you select entire entities from a data context, be that by a query expression or an Individual, the data context involved will track the entity. You can make changes to the fields by setting the relevant properties. You do not need to do anything else, as the data context handles everything for you.</p>
```fsharp
let hardy = ctx.``[main].[Customers]``.Individuals.``As ContactName``.``AROUT, Thomas Hardy`` 
hardy.ContactName <- "Pezi the Pink Squirrel"

ctx.SubmitUpdates()
```

<p>Note the call to SubmitUpdates() on the data context. This will take all pending changes to all entities tracked by the data context, and execute them in a transaction. An interesting property of the above code is that after Thomas Hardy has had his name changed, the first line will no longer compile! Go F#!</p>
<p>Similarly, you can delete an entity by calling the Delete() method on it. This action will put the entity into a pending delete state.</p>
```fsharp
hardy.Delete()

ctx.SubmitUpdates()
```

<p>After the deletion is committed, the data context will remove the primary key from the entity instance.</p>
<p>Creation is slightly different. You will find various Create methods on the IQueryable Set types that represent the tables. Up to 3 different overloads of this method will be available. The first takes no parameters and will return a new entity which are you expected to populate with at least the required fields. A second version will accept the required fields as parameters &ndash; this is only available if there are any columns marked as NOT NULL. The final version will create an entity from a (string * obj) sequence &ndash; it is potentially unsafe but very handy for copying entities or creating them from some stream of data, if you know the attribute / column names are correct.</p>
<p>A note on primary keys &ndash; presently the SQL provider does not detect identity columns, although it does deal with them on the insert appropriately. You will never see a primary key field as a parameter to the Create method. If your primary key is an identity column, you can simply not include it. Upon insert, the provider will automatically update the instance of the entity with the ID it has been assigned. If on the other hand your key is not an identity column, you will be expected to assign it an appropriate value before attempting to submit the changes to the database.</p>
```fsharp
// create an employee 
let pez = ctx.``[main].[Employees]``.Create("Pezi","Squirrel") 
ctx.SubmitUpdates()

printfn "Pezi's new employee id is %i" pez.EmployeeID

//update something 
pez.Address <- "the forest" 
ctx.SubmitUpdates()

// delete 
pez.Delete() 
ctx.SubmitUpdates()
```

<p>Now let&rsquo;s see how we can effortlessly combine different SQLProviders together in order to perform "extract, transform, load" type processes.</p>
```fsharp
type sql = SqlDataProvider<northwindCs, UseOptionTypes = true> 
type xl = SqlDataProvider<excelCs, Common.DatabaseProviderTypes.ODBC>

let northwind = sql.GetDataContext() 
let spreadsheet = xl.GetDataContext()

```

```fsharp
// load our tasty new employees from our lovely spreadsheet
let newEmployees = 
  spreadsheet.``[].[Sheet1$]`` 
  |> Seq.map(fun e -> 
    let emp = northwind.``[dbo].[Employees]``.Create() 
    emp.FirstName <- e.FirstName 
    emp.LastName <- e.LastName 
    emp) 
  |> Seq.toList

// save those puppies away 
northwind.SubmitUpdates()
```

<p>In this sample we use the ODBC type provider to gain instant typed access to an Excel spreadsheet that contains some data about new employees. the SQL Server type provider is also used to connect to a Northwind database instance.</p>
<p>We then very simply pull all the rows from the spreadsheet, map them to an Employee database record, and save them away. It doesn&rsquo;t get much easier than this!</p>
<p>Or does it? Actually, in this case we know up front that the spreadsheet contains identical field names to that of the target database. In this case we can use the overload of Create that accepts a sequence of data &ndash; this method is unsafe, but if you know that the names match up it means you don&rsquo;t have to manually map any field names :)</p>
```fsharp
let newEmployees = 
 spreadsheet.``[].[Sheet1$]`` 
 |> Seq.map(fun e -> northwind.``[dbo].[Employees]``.Create(e.ColumnValues)) 
 |> Seq.toList
```

<p>I think we can agree it really does not get much easier than that! You can also use this technique to easily copy data between different data contexts, as long as you watch out for primary and foreign keys where applicable.</p>
<h2>Data Binding</h2>
<p>The SQLProvider, with a fair amount of trickery, supports two-way data binding over its entities. This works together very well with the CRUD operations.</p>
```fsharp
open System.Windows.Forms 
open System.ComponentModel

let data = BindingList(Seq.toArray ctx.``[main].[Customers]``) 
let form = new Form(Text="Edit Customers") 
let dg = new DataGridView(Dock = DockStyle.Fill,DataSource=data) 
form.Controls.Add dg 
form.Show()
```

<p><a href="http://pinksquirrellabs.com/image.axd?picture=image_12.png"><img style="display: inline; border: 0px;" title="image" src="http://pinksquirrellabs.com/image.axd?picture=image_thumb_12.png" alt="image" width="1106" height="417" border="0" /></a></p>
<p>This is all the code you need to create a fully editable data grid. A call to SubmitUpdates() afterwards will push all the changes to the database.</p>
<h2>Other Bits</h2>
<p>The data context now has a few new methods on it that you can use.</p>
<ol>
<li>ClearPendingChanges() : Ronseal. Remove any tracked entities that have changed. Use this with caution, as subsequent changes to the entities will be tracked, but the previous changes will have been lost.</li>
<li>GetPendingChanges() : This function will return a list of the entities the data context has tracked. This is useful in a variety of situations, and it also means you do not have to bind to created or updated entities in order to not &ldquo;lose&rdquo; them in your program.</li>
</ol>
<div>The SQL Provider does not currently have any support for transactionally creating heirarchies of data - that is, where you are able to create foreign-key related entities within the same transaction. This feature may be added at a later date.</div>
<div></div>
<div>Shout outs to <a href="https://twitter.com/SimonHDickson">@simonhdickson</a> for this work on the ODBC provider, and <a href="https://twitter.com/colinbul">@colinbul</a> for the Oracle CRUD implementation, thanks guys!</div>
<div></div>
<div>Now I have done this, I can finally get on with my <strong><em>really</em></strong> important type providers, such as the <a href="http://pinksquirrellabs.com/post/2014/05/01/BASIC%E2%80%99s-50th-Anniversary-%E2%80%A6-and-more-crazy-F-type-providers!.aspx">interactive provider</a> of which I was stuck with my current extension to it as I had no way to create data in my sqlite database! :)</div>