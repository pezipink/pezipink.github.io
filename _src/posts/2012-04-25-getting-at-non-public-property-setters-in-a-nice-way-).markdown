    Title: Getting at non-public property setters in a nice way :)
    Date: 2012-04-25T02:18:00
    Tags: fsharp

<p>I’m sure every seasoned .NET developer has been in the situation at one stage or another, probably in testing code, where they need to access a non-public setter of a property (or maybe a private member), and it can’t be mocked.&#160; We all know the (somewhat scary) reflection trick to get at the said setter method and invoke it.&#160;&#160; I just hit this problem today trying to mock some response messages from a Microsoft Dynamics XRM 2011 OrganizationService.&#160; Thankfully F# gives us cool things like Quotations and symbolic functions (custom operators) to make this process more succinct.&#160; Instead of writing a method to reflect on a type, get the relevant method, then finally invoke the method and pass in both the original object instance and the new value being set, we can do the following: </p>

```fsharp
let (<~) property value =
	match property with 
	| PropertyGet(Some(Value(x)),ri,_) -> 
	    ri.GetSetMethod(true).Invoke(fst x,[|value|]) |> ignore 
	| _ -> failwith "unsupported expression"
    
```

<!-- more -->


<p>PropertyGet is an active pattern defined in the Quotations namespace that matches a piece of code that is accessing a property. The first value bound in the pattern (if it exists) is a tuple containing the object instance that the property was accessed on and its type.&#160; The second value bound (ri) is the PropertyInfo for the property in question from the System.Reflection namespace.&#160; Using this information we can simply obtain the Set method, and invoke it on the first item in the x tuple and pass in the new value as its argument.</p>

<p>To use this is simple :</p>

```fsharp
<@ entity.Attributes @> <~ newAttributes
```


<p>Simply quote the property in question, and invoke the operator with the new value (that looks very closely like the assignment operator <- )  :) </p>

<p>Another little use for operators&#160; I embraced is to deal with potential null values on properties when you are accessing them (assuming you don’t go all out and wrap everything in the Option type – you might not have the option to do this though (pun intended)) – write a custom operator like that lets you provide a default value if the property evaluates to null :</p>

```fsharp
let (>?) input def = input |> function null -> def | _ -> input
```


<p>Now you can use this like so, instead of having to repeat the code above all over the place, or explicitly call a function :</p>

```fsharp
match e.Attributes >? [||] |> Array.tryFind(fun am -> am.SchemaName = req.Attribute.SchemaName ) with ...
```


<p>All in all, loving the custom operators you can define in F# especially as you can scope them however you like, even in nested functions :) </p>
