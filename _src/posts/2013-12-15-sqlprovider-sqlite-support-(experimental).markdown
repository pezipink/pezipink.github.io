    Title: SqlProvider : SQLite support (experimental)
    Date: 2013-12-15T04:29:00
    Tags: fsharp, type providers, sqlprovider

<p>The <a href="https://github.com/pezipink/SqlProvider">SqlProvider</a>now supports experimental SQLite access.</p>
<!-- more -->

<p>You can achieve this by supplying the SQL provider some additional static parameters</p>
```fsharp
type sql = SqlDataProvider< @"Data Source=F:\sqlite\northwindEF.db;Version=3",Common.DatabaseProviderTypes.SQLITE,@"F:\sqlite\3\" >
```

<p>As you can see, you can now pass SQLITE as the DatabaseVendor static parameter, along with a valid SQLite specific connection string.</p>
<p>The third parameter is ResolutionPath. This is required for a few reasons - the first is that I do not want the SqlProvider to have any dependencies on non BCL types. Secondly, SQLite comes in many different flavours of mixed-mode assemblies. You must have the correct one for your particular system. The path you supply here will be used to dynamically load the SQLite assembly. You could always reference SQLite and remove the dynamic loading if you want a to tie your SqlProvider down to a specific SQLite assembly.</p>
<p>Depending on where the assembly is located, which version of the framework you are running and whether you are using F# interactive, you might run into some security issues. If you get red squigglies on the above line complaining that it cannot load the assembly, you will probably need to add the following app.config setting to your application, fsi.exe's configuration and devenv.exe's configuration. </p>
<p><a href="http://msdn.microsoft.com/en-us/library/dd409252(VS.100).aspx">http://msdn.microsoft.com/en-us/library/dd409252(VS.100).aspx</a></p>
<p>Typically you will find fsi.exe.config in C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\, and devenv.exe.config will be \VsInstallationLocation\Comm7\IDE\</p>
<p>Once you have everything working, you can now proceed to enumerate and query tables/views, perform joins, use select-many over relationships and make use of individuals (see <a href="http://pinksquirrellabs.com/post/2013/12/09/The-Erasing-SQL-type-provider.aspx">last post</a> for details). SQLite doesn't support stored procedures so don't expect to find much in there :)</p>
```fsharp
let ctx = sql.GetDataContext()

let christina = ctx.``[main].[Customers]``.Individuals.``As ContactName``.``BERGS, Christina Berglund``
let christinasOrders = christina.FK_Orders_0_0 |> Seq.toArray

let mattisOrderDetails =
    query { for c in ctx.``[main].[Customers]`` do
            for o in c.FK_Orders_0_0 do
            for od in o.FK_OrderDetails_1_0 do
            for prod in od.FK_OrderDetails_0_0 do // a terribly named constraint
            where (c.ContactName =% "Matti%" &amp;&amp; o.ShipCountry |=| [|"Finland";"England"|])
            select (c.ContactName,o.ShipAddress,o.ShipCountry,prod.ProductName,prod.UnitPrice) } |> Seq.toArray
```

<p></p>
<p>Have fun!</p>