    Title: Visualizing LINQ with GraphViz
    Date: 2014-01-17T21:01:00
    Tags: fsharp , sqlprovider, type providers

<p>After my talk last night, several people expressed an interest in the script I was using to draw the LINQ expression trees. I have uploaded<a href="https://github.com/pezipink/SQLProvider/blob/master/src/scripts/GraphViz.fsx"> here on github</a>.</p>
<p>This is just a script I use in development. It doesn't visualize every node by a long shot, and does a fair bit of name replacing to make some of the very long generic type names readable. You will need <a href="http://www.graphviz.org/">GraphViz</a>installed to use this script. You might need to point the function that does the generation at the location where you've installed it. </p>
<p>You can use it like this from FSI :</p>
```fsharp
#load "GraphViz.fsx"
FSharp.Data.Sql.Common.QueryEvents.LinqExpressionEvent
|> Observable.add GraphViz.toGraph
```

<p>Now, whenever you evaluate a query expression, it will pop up a bunch of images with whatever your machine uses to view SVG files, like this rather lovely one below. Have fun.</p>
<p><em>edit; this will spam your temp directory with svg files!</em></p>
<p></p>
<p><img src="../../../../../img/old/Picture1.gif" alt="" /></p>
<!-- more -->
