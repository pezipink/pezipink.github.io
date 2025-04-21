    Title: Monads to help with COM cleanup
    Date: 2012-12-01T23:49:00
    Tags: fsharp


<p>I currently do a lot of Office type automation work where I scan a bunch of email from exchange, download excel attachments, open and transform a bunch of data from them, reconcile these against databases using FLinq, produce graphs and charts with the results using FSharpChart, and so forth.</p>
<p>(p.s - as a side note, F# is <em>awesome</em>at doing this kind of thing, I can knock all kinds of stuff out super fast. p.p.s - Active patterns with Excel = win)</p>
<p>As anyone who has done any office automation will know, cleaning up the COM objects is a right pain. Any object you bind to a value be it a workbook, sheet, cell, range or anything at all has to be explicitly freed with Marshal.FinalReleaseComObject. If you are not really careful with this you will end up with memory leaks, and EXCEL.EXE (or whatever) running forever in the background even once your program has shut down. The problem actually goes further than this, if you use the . . . notation to traverse the object models, all the intermediate objects are also bound and need freeing up. However, these ones as they were not explicitly bound you can get away with calling GC.Collect() andGC.WaitForPendingFinalizers().</p>
<p>You can see any number of articles on the interwebs about this issue and all solutions are ugly. F# to the rescue! Here is a simple computation expression I use to deal with this problem. It is not optimal but the interop is horribly slow anyway so it makes no difference.</p>
<!-- more -->

```fsharp
type ComCleaner(cleanUp) =
    let objects = new System.Collections.Generic.HashSet<obj>()
    let mutable isDisposed = false

    member this.Zero() = (this :> IDisposable).Dispose()
    member this.Delay(f) = f()
    member this.Bind(x, f) = 
        objects.Add (box x) |> ignore
        f(x)
    member this.Return(x) =         
        (this :> IDisposable).Dispose()
        x
    interface System.IDisposable with
        member x.Dispose() =  
            if not isDisposed then
                isDisposed <- true
                objects |> Seq.iter (Marshal.FinalReleaseComObject > ignore)
                match cleanUp with Some(f) -> f() | None -> ()        
                GC.Collect()
                GC.WaitForPendingFinalizers()
```

<p>I thought this was a fairly interesting use of the disposable pattern, by implementing it on the builder class itself. Any objects bound with let! are added to a object hashset. When Zero or Return is hit dispose is called. This also means the whole thing can be bound with the use keyword to ensure dispose really will get called even if an unhandled exception is raised. This is the simplest version - it is relatively easy to add support for combine, looping constructs and so on.</p>
<p>The builder takes a parameter <em>cleanUp</em> - this is a optional function that can be passed in which will be executed on dispose. This facilitates building custom versions of the com cleanup monad, for example here is one I use for Excel.</p>
```fsharp
let xlCom = // excel com cleaner that closes and frees all workbooks then quits and frees excel
    fun (app:Excel.Application) ->
        new ComCleaner(Some(fun ()->
            if app.Workbooks <> null && app.Workbooks.Count > 0 then app.Workbooks |> Seq.cast<Workbook> |> Seq.iter(fun x -> x.Close(false))
            app.Quit()
            Marshal.FinalReleaseComObject app |> ignore))
```

<p>As you can see this performs a whole shutdown of excel and forces proper cleanup of the application object. I also have a function that creates aComCleanerwith no function :</p>
```fsharp
let com = fun () -> new ComCleaner(None) // com cleaner that performs no additional cleanup
```

<p>Now the basic use for an entire excel data extraction looks like this</p>
```fsharp
let excelData file = 
    let xl = Common.Excel.excelApp()
    use xlc = xlCom xl
    xlc { 
        let! wb = xl.Workbooks.Open(file)
        let! ws = wb.Worksheets.[1] :?> Worksheet
        let! rows = ws.Rows
        return
            rows
            |> Seq.cast<Range>
            |> Seq.takeWhile(fun row -> row.Cellsft 1 1 <> "")
            |> Seq.choose(do something here)
            |> Seq.toList }
```

<p>If you need to bind / free some stuff in one of the lambda functions then you can easily use the normal com cleaner inside like so</p>
```fsharp
|> Seq.choose( fun row -> com() {  do something } )
```

<p>Things to be wary of are returning unevaluated sequences (obviously) and let! bindings to objects that are already in the hashset or not COM objects at all - in both cases you will get an exception but I prefer it this way as it's a good indicator you have done something very wrong! You will also need to be careful it if you need to return a COM object, you can do it but will need to not let! bind it and then make sure it is dealt with properly later on. Generally it should be avoided.</p>