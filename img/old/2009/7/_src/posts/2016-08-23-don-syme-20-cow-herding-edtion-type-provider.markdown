    Title: Don Syme 2.0 : Cow Herding Edition Type Provider
    Date: 2016-08-23T23:34:00
    Tags: fsharp, cows, squirrels, type providers


Though the mystery man Don Syme, Father of F#, is generally heralded for various software based innovations and other computer related things, it transpires he has talents in other unrelated areas...
<!-- more --> 
<p>Specifically, Don is master cow herder (yes, as in moo-cows). Whilst this might seem unlikely, I present to you a legendary but scarce video, with Don in action whilst attempting to get to work across the treachrous cow infested fields of Cambridge. Behold! (Watch the whole thing for a bonus cow at the end, tired from the workout)</p>
<p><iframe src="https://www.youtube.com/embed/tYycYHIU_jc" width="420" height="315"> </iframe></p>
<h1>Type Providers?</h1>
<p>It has been quite some time since I wrote a sensible type provider, well overdue I would say. I decided I would give the Don Syme Type Provider a facelift to celebrate his lesser-known talents of cow herding. </p>
<p><a href="http://pinksquirrellabs.com/post/2014/02/21/The-Don-Syme-type-provider.aspx">The original type provider</a> produces an endless stream of facts about the mystery man himself. This of course remains, but now you can also specify if you are interested only in more technical / geeky facts as is evidenced in this picture.</p>
<p><img src="/image.axd?picture=2016%2f8%2fdon2.png" alt="" width="800" height="200" /></p>
<p>(see the last section of this post for instructions on obtaining and running the type provider)</p>
<h1>Cows!</h1>
<p>The primary new feature, however, is a type system game where you can play as Don, attempting to herd the Cambridge Cows back into their cow sheds. Here is a picture of a game in progress:</p>
<p><img src="/image.axd?picture=2016%2f8%2fdcows.png" alt="" width="600" height="200" /></p>
<p>Legend:</p>
<p>"C" -> A Cow</p>
<p>"░" -> empty field</p>
<p>"D" -> Don</p>
<p>"*" -> A cow in a cow shed</p>
<p>"۩" -> A cow shed</p>
<p>"█" -> Wall</p>
<p></p>
<p>The aim is to get the cows into their cow sheds. Don is able to push the cows around, but only if there is an empty space behind them! You will quickly see this is really quite difficult and will require some thought to succeed.</p>
<h1>Sokoban!</h1>
<p>The astute and well gamed reader may have noticed this is a remixed version of the popular game <a href="https://en.wikipedia.org/wiki/Sokoban">Sokoban</a>. It is in fact is a full Sokoban implementation in a type provider, that uses the .slc Sokoban level format. Let's have a look at how it works.</p>
<h2>Model</h2>
<p>We can model the entire game in a couple of unions and records (you can read the source of this file <a href="https://github.com/pezipink/InteractiveProvider/blob/master/CowHerding/Cows.fs">here</a>)</p>
```fsharp
type Cambridge =
  | Cow
  | Field
  | Don of bool  // true if on a shed
  | Shed of bool // true if a cow is in the Shed
  | Wall

  with override x.ToString() =
      match x with
      | Cow -> "C"
      | Field -> "░"
      | Don _ -> "D"
      | Shed true -> "*"
      | Shed false -> "۩"
      | Wall -> "█" 

type Direction =
  | North
  | South
  | East
  | West

type Location = int * int

type LevelData =
  { Id : string
    Width : int
    Height : int
    Data : Map<int*int, Cambridge> }

type LevelCollection =
  { Title : string
    Description : string
    Copyright : string
    Levels : LevelData list }
```

<h2>Level Files</h2>
<p>The .slc files are simply XML files, containing a set of levels. We can use the XML type provider to do the hard work for us (yes, you can use type providers in other type providers). The current directory is scanned for .slc level collection files at compile time, and each one is turned into a Level Collection record.</p>
```fsharp
let readLevels (root:CowLevel.SokobanLevels) =
  // reads an entire .slc sokoban level collection
  // do we care about memory? of course not!
  { Title = root.Title
   Description = root.Description
   Copyright = root.LevelCollection.Copyright
   Levels =
    root.LevelCollection.Levels
    |> Array.map(fun level ->
      { Id = level.Id
        Height = level.Height
        Width = level.Width
        Data =
          [for row = 0 to level.Ls.Length-1 do
             let chars = level.Ls.[row].ToCharArray()
             for col = 0 to chars.Length-1 do
               let c =
                 match chars.[col] with
                 | ' ' -> Field
                 | '#' -> Wall
                 | '$' -> Cow
                 | '.' -> Shed false
                 | '*' -> Shed true
                 | '@' -> Don false
                 | '+' -> Don true
                 | c -> failwithf "unexpected character '%c'" c
               yield (row,col),c] |> Map.ofList })
    |> Seq.toList }
```

<p>Piece of cake, it basically writes itself!</p>
<h2>Logic</h2>
<p>Now for the most difficult part, which is the encoding of the game rules. The type provider will always allow Don to attempt to move in any direction. We must work out if the movement is valid, and update the game state if it is.</p>
<p>First up, find Don's current location in the map, and whether he is "standing" on a shed or field</p>
```fsharp
let moveDon direction map =
  let (dx,dy),don = Map.pick(fun k v -> match v with Don _ -> Some(k,v) | _ -> None) map
  // Don can only ever be standing on a open field or shed tile
  let oldTile = match don with Don true -> Shed false | _ -> Field
```

<p>Next we will need to work out what tiles will be affected by the move. This will always potentially be the two tiles in the direction Don is attempting to move. We can do a bit of trickery here to calculate the indexes and extract the map tiles, and if the index is out of bounds we ignore it. This will return a list of 1 or 2 tiles we can then match on to see what happens.</p>
```fsharp
match direction with
  | North -> [-1,0;-2,0]
  | South -> [1,0;2,0]
  | East -> [0,1;0,2]
  | West -> [0,-1;0,-2]
  |> List.choose(fun (x,y) ->
    Map.tryPick(fun k v -> if k = (dx+x,dy+y) then Some(k,v) else None) map)
  |> function
```

<p>We can say that if Don is attempting to move onto a field, that is always valid regardless of the second tile. The same holds true for empty cow sheds</p>
```fsharp
// Don can always move onto a dirt tile
  | [(x,y),Field]
  | [(x,y),Field;_] ->
    map
    |> Map.add (x,y) (Don false)
    |> Map.add (dx,dy) oldTile
  
  // same as above, for moving onto a field without a cow in it
  | [(x,y),Shed false]
  | [(x,y),Shed false;_] ->
    map
    |> Map.add (dx,dy) oldTile
    |> Map.add (x,y) (Don true)
```

<p>In these cases we simply place Don in the new location, and replace his old location with whatever tile he was "standing" on before.</p>
<p>The slightly more complex cases are of pushing cows around. However, using the pattern matching, the solution to this problem, like a lot of this, basically writes itself.</p>
```fsharp
  // Valid cow cases. We can move a cow forward if there is an empty space behind them.
  // Shed true is also a cow but must then be replaced with a Don true
  | [(x,y),Cow; (x',y'),Field] ->
    map
    |> Map.add (dx,dy) (oldTile)
    |> Map.add (x',y') Cow
    |> Map.add (x,y) (Don false)
   
  // Moving a cow from a shed onto a field
  | [(x,y),Shed true; (x',y'),Field] ->
    map
    |> Map.add (dx,dy) (oldTile)
    |> Map.add (x',y') Cow
    |> Map.add (x,y) (Don true)
   
  // moving a cow from a shed or field to a shed
  | [(x,y),Shed true; (x',y'),Shed false] ->
        map
        |> Map.add (dx,dy) (oldTile)
        |> Map.add (x',y') (Shed true)
        |> Map.add (x,y) (Don true)
        
  | [(x,y),Cow; (x',y'),Shed false] -> 
        map
        |> Map.add (dx,dy) (oldTile)
        |> Map.add (x',y') (Shed true)
        |> Map.add (x,y) (Don false)
    
  // all other cases are invalid.
  | _ -> map
```

<p>And that is the entire game done, except a couple of auxillary functions to determine if the game has been won, to print the level and so forth.</p>
<h1>Providing Bovine Based Types</h1>
<p>Like all my type provider games, this is implemented using my <a href="http://pinksquirrellabs.com/post/2014/05/01/BASIC%E2%80%99s-50th-Anniversary-%E2%80%A6-and-more-crazy-F-type-providers!.aspx">Interactive Provider</a> which allows easy creation of type providers without having to write any horrible provided types code. I have this simple union that determines the menu structure of the type provider</p>
```fsharp
type MenuTypes =
  | Introduction
  | FactSelect
  | Facts of bool
  | CollectionSelect
  | LevelSelect of LevelCollection
  | Game of LevelData
```

<p>then each one has a InteractiveState object associated with it, that determines the text that appears in Intellisense, the options displayed as properties, and a callback to handle the results. These call each other to navigate through the menus and recursively to display the endless amazing facts or the currently playing level of cow herding. I will show the fact states here, but you can look at the full implementation if you want to see how the cow herding works.</p>
```fsharp
let rec factCycle factType =
  { displayOptions = fun _ ->
      ["Learn another amazing fact",box 1]
    displayText = fun _ -> getFact factType
    processResponse = fun _ -> factCycle factType :> _
    state = Facts factType }
 
let factTypeSelect() =
  { displayOptions = fun _ ->
      ["All", box false
       "Technical",box true]
    displayText = fun _ ->
      "Select a fact category"
    processResponse = fun (e,resp) ->
      factCycle (resp :?> bool) :> _
    state = FactSelect }
```

<p>(the fact generator works by calling a webservice and parsing the results using the JSON type provider)</p>
<h1>To Get Herding</h1>
<p>(NOTE. You MUST change your tooltip font to a monospace font. I suggest Lucida Console in at least 16pt.)</p>
<p><a href="https://github.com/pezipink/InteractiveProvider">Grab the InteractiveProvider from my github here</a>, build it, then create a script file and reference the type provider. Since the InteractiveProvider dynamically loads assemblies that contain types implementing the interfaces it is looking for, you will have to tell it as a static parameter the directory that the cow herding dll resides in.</p>
```fsharp
#r @"c:\repos\InteractiveProvider\InteractiveProvider\bin\Debug\InteractiveProvider.dll"
open PinkSquirrels.Interactive
type GamesType = InteractiveProvider< @"c:\repos\InteractiveProvider\Cowherding\bin\Debug\">
games.``Start DonSyme``
```

<p>Credit where credit's due, I have included 3 .slc files of Sokoban levels from the <a href="http://www.sourcecode.se/sokoban/levels">website found here</a>. If you are really bored at work, there are some forty thousand levels of cow herding action for you to download!</p>
<p>Note this is designed to work in Visual Studio. Emacs will probably mess up the popups depending on your settings, and I have no idea what it will do in VSCode.</p>
<p></p>
<h1>MOOOOO! HAPPY HERDING!</h1>
<p></p>