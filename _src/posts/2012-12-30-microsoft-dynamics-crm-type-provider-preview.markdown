    Title: Microsoft Dynamics CRM Type Provider preview
    Date: 2012-12-30T03:50:00
    Tags: fsharp, type providers, xrm


<p>Are you tired of generating strongly typed classes with millions of lines of C# using crmsvcutil.exe that often crashes? Fed up with having to re-generate the classes every time something in the schema changes? Feeling restricted by the LINQ provider's limitations? Ever wonder why you should need to know what attribute joins to what in your relationships to perform your joins? F# to the rescue!</p>
<p>I am working on a F# type provider than aims to solve a lot of these pains, and more besides! This is just a sneak preview with what is to come, I have not released any of this yet. First, here is an example of a rather silly but fairly complex query using the new type provider and the F# query syntax :</p>
```fsharp
type XRM = Common.XRM.TypeProvider.XrmDataProvider<"http://something/Organization.svc">

let dc = XRM.GetDataContext()

type TestRecord = 
    { x : string; 
      y : int }

let test = { x = "John%"; y = 42 }

let q =
    query { for s in dc.new_squirrel do                            
            where (s.new_name <>% test.x || s.new_age = test.y)
            for f in s.``N:1 <- new_new_forest_new_squirrel`` do
            for o in f.``N:1 <- owner_new_forest`` do
            where (s.new_colour |=| [|"Pink";"Red"|])
            where (f.new_name = "Sherwood")
            where (o.name = "PEZI THE OWNER!")
            select (s, f.new_name, f, o) }
```

<!-- more -->

<p>This example illustrates several cool features of the F# type provider ;</p>
<ul>
<li>No code generation here - these "virtual" types are magically created on the fly by the compiler, using the organization service's metadata capabilities. This provides full intellisense and only lazily loads the entity attributes and relationships on demand. If your schema changes and breaks something, the code will not compile.</li>
</ul>
<div></div>
<ul>
<li>Where clauses can appear anywhere and be as complex as you want. the only restriction is that only one entity type is used in each clause - this is because mixing OR logic between entities is impossible to translate into the underlying QueryExpression tree.</li>
</ul>
<div></div>
<ul>
<li>Relationships can be accessed via the SelectMany (for) syntax. In this example Squirrel is the ultimate child and the code is traversing up the relationships through its parents (forest, and then owner). Instead of needing to know or care about how these relationships are joined, this is handled for you in the magic. Additionally, should you care, the intellisense will show you exactly what attribute is being joined to what. It is also possible to start at a parent and traverse down the one-to-many relationships, as long as you don't branch off anywhere to more than one child as this is not supported in the underlying provider.</li>
</ul>
<div></div>
<ul>
<li>Custom operators! F# lets you define your own operators. Currently I am supporting =% (like) <>% (not like) |=| (in) and |<>| (not in) however it will be very easy to add more of these, or extensions to the relevant types to implement the wealth of other, sometimes rather exotic, XRM condition operators, of which almost all are not currently accessible from the existing LINQ provider, including in and not in.</li>
</ul>
<div></div>
<ul>
<li>Projection expression - not only can you access attributes and perform any transformation, you may have noticed it allows you to select entire parent entities - you cannot do this in the current LINQ provider because the QueryExpression is only capable of returning an single entity type which has to be the ultimate child that you are selecting. Any attributes from parents are kind of shoehorned into the result entity with aliases - more magic in the type provider enables you to select all the entities out as real CRM entities :)</li>
</ul>
<div>Joins are also still supported should you wish to use them explicitly for some reason. At runtime, these "virtual" entities get "erased down" to a XrmEntity which is a thin wrapper inheriting from Entity - this means the resulting objects you can use everywhere like you normally would in your CRM code.</div>
<div></div>
<div>Lots of stuff to come - paging, many LINQ execution methods, ordering, possibly even an auto conversion to fetchxml which will enable aggregate operations...</div>