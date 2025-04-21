    Title: Introducing - The Mixin Type Provider
    Date: 2015-03-01T12:28:00
    Tags: fsharp, type providers

<p>I am very excited to finally share the first version of my latest type provider, the Mixin Provider! This post is quite long but you should read it all, this only scratches the surface really.</p>
<!-- more -->
<h1>Background</h1>
<h2></h2>
<h2>Code Generation in F#</h2>
<p>Code generation in any language is a double edged sword. It is an extremely powerful technique used all over the place, often to great effect. It can also turn into a complete nightmare with millions of often unnecessary and bloated lines of code, hard to find bugs, and hard to manage templates.</p>
<p>F# goes a long way to eliminating the need for a lot of boilerplate code with the use of its amazing Erasing Type Providers, of which you may know I am a big fan and have wrote <a href="https://github.com/fsprojects/SQLProvider/tree/master/src/SQLProvider">many useful</a> and <a href="https://skillsmatter.com/skillscasts/6126-where-no-type-has-gone-before#video">even more useless examples</a> of.</p>
<p>However, that does not mean that F# has no need of code generation or boilerplate &ndash; erasing type providers ace and all, but I have still found myself on numerous occasions writing some dodgy F# script that pumps out a load of (incorrectly indented) code using some metadata to save me the legwork of writing and maintaining it manually, and I&rsquo;m sure a lot of you have too, or certainly could have a use for being able to generate F# code in a controlled manner if there was a relatively easy way to do it.</p>
<p>The code generation story for F# is basically non existent. The only thing I am aware of other than just doing it yourself, are generative type providers &ndash; which are extremely limited, hard to write and not very appealing in general. In particular, generative type providers can only generate simple types and are not able to generate arbitrary code or any of the special F# types such as record types, discriminated unions, computation expressions, type providers, or even types that use .NET generics.</p>
<h2>The Mixin</h2>
<p>The concept of Mixin means different things in different languages. I am taking my inspiration from the very awesome D programming language which I have been learning recently. <a href="http://dlang.org/mixin.html">The D mixin</a> is extremely powerful, it can accept any compile-time program, and during compilation it is passed through a D interpreter and the results are inserted into that very location in the program. This is not a pre-compile step. Now, whilst I can&rsquo;t achieve that kind of power, this knowledge along with my many many crazy experiments with type providers led me to the realization that I could do a similar thing.</p>
<h1>The Mixin Type Provider</h1>
<h2>! Type Provider Police !</h2>
<p>If such a thing exists, I am going to be #1 on their most wanted list for this one! (If I wasn&rsquo;t already!). Forget the <a href="https://github.com/fsprojects/FSharp.TypeProviders.StarterPack/blob/master/src/ProvidedTypes.fs">notorious provided types API</a> and type erasure. In fact, the Mixin TP in many ways is not a type provider at all! It is mostly some *ahem* creative use of the fact that a type provider is really a plugin or extension point for the F# compiler we can hook into to do fun stuff which were probably not supposed to.</p>
<h2>Mixin Lite</h2>
<p>Let&rsquo;s take a look at the simplest example of the Mixin provider in action, in what I like to call Lite mode. When used in this fashion, the Mixin TP acts very much like a generative type provider, with most of the same limitations that are inherited from using the Type Provider infrastructure.</p>
```fsharp
open MixinProvider 
type FirstTest = mixin_gen< """let generate() = "let x = 42" """ > 
FirstTest.x // 42
```

<p>What&rsquo;s happening here? A type alias called <em>FirstTest </em>is created, referencing the type provider <em>mixin_gen</em>. The static parameter passed to <em>mixin_gen</em> is a F# <em>metaprogram</em>. This program can be any valid F# interactive program (and would not usually be specified inline in this manner). Mixin Metaprograms have only one rule &ndash; they must have an accessible function called <em>generate </em>which will be called at compile time and is expected to return <em>another</em> F# program that will be compiled into an assembly, written to disk, and then have its types injected back through the <em>FirstTest </em>alias.</p>
<p>Phew, that was a bit of a mouthful. Let&rsquo;s see what&rsquo;s happening step by step:</p>
<ol>
<li>During compilation the type provider creates an FSI session and loads in the code <em>let generate() = "let x = 42"</em></li>
<li>The FSI session evaluates <em>generate() </em>which in turn returns the string <em>"let x = 42"</em></li>
<li>The type provider wraps the resulting program in a module named <em>FirstTest</em></li>
<li>The type provider takes the completed program text, and compiles it with the F# compiler</li>
<li>The resulting assembly is written to a location on the disk named <em>FirstTest.dll</em></li>
<li>The type provider infrastructure is leveraged to provide you access to the generated code directly through the <em>FirstTest</em> type alias.</li>
</ol>
<h2></h2>
<h3></h3>
<h2>Metaprogram files and parameters</h2>
<p>Let&rsquo;s look at how we can take this concept further. Mostly you will not want to write inline programs, but instead specify .fsx files that contain them. You are also able to extend your generate function so that it accepts parameters, which can be passed in as static parameters to <em>mixin_gen.</em></p>
<p>Here&rsquo;s an example metaprogram, ConnectionString.fsx</p>
```fsharp
open System 
let generate user = 
 match user with 
 | "John" when System.DateTime.Now.DayOfWeek = DayOfWeek.Monday -> 
   "let [<Literal>] connectionString = \"JohnMon!\" " 
 | "John" | "Dave" -> 
   "let [<Literal>] connectionString = \"normal :(\" " 
 | _ -> failwithf "user %s is not allowed a connection string!" user
```

<p>In this metaprogram we use both a parameter passed in and some environmental data to calculate what our connection string should be. Notice how the two good branches both generate a [<Literal>] string called connectionString. If the user is not Dave or John, the Mixin provider will refuse to generate any code.</p>
```fsharp
type ConnectionString_Test = mixin_gen<"connectionstring.fsx", metaprogramParameters = "\"John\"" >

type SqlDataProvider<ConnectionString=ConnectionString_Test.connectionString>
```

<p>Awesome! In this example, a compile time program is used to work out which connection string is required, and because the output is a literal, it can be fed straight into a static parameter of <em>another</em> type provider, in this case the erasing SQL provider. This is an immediate and powerful fusion of the Mixin type provider with erasing type providers, and solves a real problem a lot of people experience when forced to use literals as static parameters. We also use a mixin static parameter here that is passed through to the generate function. You might be thinking, that somewhat mitigates the benefit gained by generating the connection string, and you&rsquo;d be right! I just wanted to show it&rsquo;s you are able to, and can open some very powerful possibilities.</p>
<h2>Mixin Full</h2>
<p>You might have noticed that in the previous examples I didn&rsquo;t actually generate any types, and you&rsquo;d be right. In fact the Mixin provider used an odd sort of form of <em><a href="http://en.wikipedia.org/wiki/Compile_time_function_execution">compile time function execution</a></em> . This lets you calculate stuff up front rather than at runtime. The obvious candidates here are lookup tables and heavy math crunching, though once you get a metaprogram mindset going you will realise a lot more potential for it.</p>
<p>Types, on the other hand, are probably what most people will want from a code generator. I deliberately left types out of the above examples, because when the Mixin provider is used in the above manner, it is basically the same as a generative type provider &ndash; that is, though you can generate any code you like, you will not &ldquo;see&rdquo; F# specific types for what they are, rather they will be presented to you as normal .NET types (you can still use generics and stuff though!)</p>
<h2>A change of mindset</h2>
<p>To harness the full power of the Mixin Provider, a change of mindset is going to be required. Forget this is even a type provider at all &ndash; in fact is isn&rsquo;t really - it is a code generator hooked into the compile pipeline with some powerful features and libraries to go with it. If you reference the libraries it produces from another program, you will have none of the aforementioned limitations, and you will be able to generate everything from records to type providers.</p>
<p>If that sounds like it&rsquo;s going to be a pain, it really isn&rsquo;t. Create a code generator project that contains all your metaprograms and the Mixin provider reference. You are able to tell the provider where to write its assemblies, and you make that your shared \lib\ directory. After the first time you generate the assemblies, reference them in your other projects are you are done &ndash; as long as your code generation project builds first, all the dependent libraries will see any updated assemblies. This, in my mind, is a very small trade off for the power gained :) (by the way, there are switches that can tell the Mixin provider to never generate, always generate, or generate when something changed)</p>
<h2>Strings Suck!</h2>
<p>Yeah yeah, I know. Almost all code generators (macro expansion style aside) suffer from this problem in one form or another. It&rsquo;s the tradeoff you have to make. I could argue that dealing with massive expression trees is also not much fun, even if it is &ldquo;safer&rdquo;. In fact, strings make some things really easy that would be very tough in expression trees. For F#, things are even worse, as being a whitespace sensitive language, it is at minimum a pain to get the indentation right, and in many cases can be really quite tricky. (By the way, if you are thinking &ldquo;Quotations!&rdquo; at this point, <a href="https://github.com/eiriktsarpalis/QuotationCompiler">Erik&rsquo;s excellent Quotation Compiler</a> can be used in conjunction with the Mixin TP, but quotations are very limited and can&rsquo;t deal with a whole bunch of stuff including type declarations.)</p>
<h3></h3>
<h3></h3>
<h2>A compositional code generation DSL (<a href="https://github.com/pezipink/MixinProvider/blob/master/src/MixinProvider/SquirrelGen.fs">SquirrelGen</a>!)</h2>
<p>I was thinking about how I could make code generation easier. The two main problems to deal with are to reduce the amount of strings to a minimum, and somehow tackle the indentation in a nice way that would be largely transparent to the user. Being a big fan of <a href="http://www.quanttec.com/fparsec/">FParsec</a>, I thought it should be possible to do basically the opposite, where we start with small string creation functions and gradually compose them together into bigger and bigger functions that are able to generate various F# constructs. This approach is very powerful in many ways, partial function application here means we can almost entirely remove the problem of worrying about indentation &ndash; the downside is the library source is quite complicated to understand at first if you have not done a lot of compositional heavy work (neither have I!). However, you don&rsquo;t really need to understand it fully to use it effectively!</p>
<p><strong>(NOTE! This generation DSL is very young, the product of a few train journeys! it is subject to complete change at any point!)</strong></p>
<p>Let&rsquo;s look at a new metaprogram that will introduce several new ideas.</p>
```fsharp
#r @"FSharp.Data.SqlProvider.dll" 
#load "SquirrelGen.fs"

[<Literal>] 
let peopleCs = @"Driver={Microsoft Excel Driver (*.xls)};DriverId=790;Dbq=I:\people.xls;DefaultDir=I:\;"

open FSharp.Data.Sql 
open MixinProvider 
open System.Text

type sql = SqlDataProvider<Common.DatabaseProviderTypes.ODBC, peopleCs> 
let ctx = sql.GetDataContext()

let generate() = 
 // create a person record type 
 let personType = 
   crecord 
     "Person" 
     ["firstName", "string" 
      "lastName", "string" 
      "age", "int" ] 
     []

 let createPersonRecord firstName lastName age = 
   let fullName = sprintf "%s%s" firstName lastName 
   // create record instantation 
   let record = 
     instRecord 
       ["firstName", str firstName 
        "lastName", str lastName 
        "age", age ] 
 
 // create let expression 
 cleti fullName record

 let peopleRecords = 
 ctx.``[].[SHEET1$]`` 
 |> Seq.map(fun person -> 
   createPersonRecord 
     person.FIRSTNAME 
     person.LASTNAME 
     (string person.AGE)) 
 |> Seq.toList

 (1,new StringBuilder()) 
 // create a module with all our stuff in it 
 ||> cmodule "People" (personType :: peopleRecords) 
 |> fun sb -> sb.ToString()
```

<p>In this example the metaprogram <em>itself </em>uses the SQL type provider. This time, it is used in ODBC mode connecting to a spreadsheet that has some information about people in it. A record type <em>Person </em>is created to hold the information and then an instance for each person is created, and finally it is all wrapped in a module named <em>People</em>. This produces the following code</p>
```fsharp
[<AutoOpen>]module Excel_Test 
 module People = 
 type Person = { firstName : string; lastName : string; age : int }

 let DaveJones = 
 { firstName = "Dave"; lastName = "Jones"; age = 21; } 
 let JohnJuan = 
 { firstName = "John"; lastName = "Juan"; age = 42; } 
 let RossMcKinlay = 
 { firstName = "Ross"; lastName = "McKinlay"; age = 42; } 
 let JuanJuanings = 
 { firstName = "Juan"; lastName = "Juanings"; age = 52; }
```

<p>We can harness the huge power of the erasing type providers as in input to the generation DSL and very easily create code. This is a trivial example of course, a real example might create types from metadata and have implementations that also use runtime erasing type providers! The code generation DSL is very young in changing a lot, but notice how you do not have to care about indentation at all, it just sorts it out for you based on an initial number that was passed into <em>cmodule </em>(1 in this case, as I know the Mixin provider wants to wrap the results with another module). It contains (or will) functions to create most F# types and common constructs, and if it&rsquo;s missing something or you want to compose further pieces, you can simply build up your own functions on top of it.</p>
<h3></h3>
<h2>A head exploder</h2>
<p>In the last example, I used the SQL type provider <em>inside</em> the metaprogram. What if I wanted work out the connection string of the spreadsheet at compile time, like in the earlier example? Fret not, I made it so that you can use the Mixin type provider recursively, <em>inside </em>the Mixin metaprograms!</p>
```fsharp
#r @"FSharp.Data.SqlProvider.dll" 
#load "SquirrelGen.fs" 
#r@"MixinProvider.dll "

open FSharp.Data.Sql 
open MixinProvider 
open System.Text

[<Literal>] 
let cstringMetaProgram = """let generate() = 
  if System.Environment.MachineName = "PEZI" then 
    "[<Literal>]let peopleCs = \"localConnection\"" 
  else 
    "[<Literal>]let peopleCs = \"otherConnection\"" """

type CString = mixin_gen< cstringMetaProgram >

type sql = SqlDataProvider<Common.DatabaseProviderTypes.ODBC, CString.peopleCs>
```

<p>&hellip;.</p>
<h2>Stuff to watch out for</h2>
<p>As you might imagine, I had to jump through many flaming hoops to get the Mixin type provider to work, and as such it is not without a few issues and should be treated as an early alpha. In particular, look out for :</p>
<ol>
<li>If you use the F# power tools extension or another extension that uses FSharp.Compiler.Services, the Mixin provider might be confused with which version to use &ndash; this should not be a problem if your other extensions are using a recent version of the compiler services</li>
<li>This has not been tested at all in IDEs other that Visual Studio and almost certainly not work in Mono without some tweaks, though they should be simple (let me know if you&rsquo;d like to do this!)</li>
<li>Type providers are notorious for locking assemblies and the Mixin provider is worse than normal. This is why it is recommended to have a separate project for &ldquo;Full&rdquo; mode. However, even in Lite mode you might run into some locking problems whilst you are messing with the generation. Simply restart visual studio to fix this &ndash; but be aware that as soon as you have code on the screen that uses mixin_gen, the assemblies it generates often be locked by the background compiler / intellisense. Not much I can do about this. You might like to make sure the source files in the editor are closed before you restart, as it is mostly the background compiler that causes the problem.</li>
<li>The provider will try to report errors from the FSI evaluation and compiler into intellisense. You can look at the .fs file it generated if it got that far, it will be in the same location as the output dll.</li>
<li>At the moment, the source metaprograms must be in in a location relative to the location of the mixin provider assembly, so mark your fsx metaprgoram files so they are copied to the output directory and you should be good.</li>
</ol>
<h2></h2>
<h2>To get going</h2>
<p>If you want to try out the Mixin provider, <a href="https://github.com/pezipink/MixinProvider">you can get it at my github here</a>. I have not pushed a package for it yet. There is not really any documentation for it yet either so you will mostly be on your own experimenting with it. The code generation DSL does not have a ton of capabilities and it not very well tested. I&rsquo;d be happy to help with any problems though, and would like to know if you do anything cool with it! Let me on twitter @pezi_pink or you can email me at pezi_pink [at] pinksquirrellabs com</p>
<h2>Final Words</h2>
<p>The Mixin type provider brings a powerful code generation story to F#, and I hope you find it useful. Over the coming months it should see more features implemented and some proper documentation, packages and so forth added.  I will also be talking at the <a href="https://skillsmatter.com/conferences/6724-f-exchange">F# exchange in April</a> on the Mixin Type provider if you can make it down :)</p>