    Title: 2048 – Type Provider Edition
    Date: 2014-07-03T11:36:00
    Tags: F#, type providers


<h2><span style="font-size: 1.5em;">Intro</span></h2>
<p>I&rsquo;m sure you would have all seen the highly addictive and annoying game <a href="http://gabrielecirulli.github.io/2048/">2048</a> by now (if not, follow the link and have a go now, don&rsquo;t forget to come back here though! ). Fellow F#er <a href="https://twitter.com/brandewinder">@brandewinder</a> wrote a bot that wins the game for you, subsequently turning it into an cool F# dojo. It is <a href="http://www.meetup.com/FSharpLondon/events/185190272/">London&rsquo;s turn for this dojo next Thursday</a>, so I figured before then I would have a go myself and do the obvious thing which is to turn it into a type provider :)</p>
<p>2048 TP Edition is available as part of my <a href="http://pinksquirrellabs.com/post/2014/05/01/BASIC%E2%80%99s-50th-Anniversary-%E2%80%A6-and-more-crazy-F-type-providers!.aspx">type provider abstraction</a> the <a href="https://github.com/pezipink/InteractiveProvider">Interactive Provider</a>. You will want to set your tooltips to a fixed-width font for this to render for you properly. Here is a picture of it in action !</p>
<p><a href="http://pinksquirrellabs.com/image.axd?picture=image_13.png"><img style="display: inline; border: 0px;" title="image" src="http://pinksquirrellabs.com/image.axd?picture=image_thumb_13.png" alt="image" width="902" height="305" border="0" /></a></p>
<!-- more -->
<h2>2048 Implementation</h2>
<p>I will start by saying that I have not looked at any other implementations of either the game or any automated bots, so if this is terrible then please forgive me. I had also not played the game at all until recently and as such the rules implemented here are from my brief analysis of playing it. There might be some subtleties I have overlooked.</p>
<p>I first implemented this using arrays as it seemed like a natural fit for the 4 x 4 board, but although I got it to work, it was horrible and instead I replaced it with this much more functional version.</p>
```fsharp
type data = Map<int * int, int> 
type direction = Up | Down | Left | Right
```

<p>That covers the entire domain :) Each location of the grid is stored in the map along with the value, if one exists.</p>
```fsharp
let shift (x,y) = function 
  | Up -> (x,y-1) 
  | Down -> (x,y+1) 
  | Left -> (x-1,y) 
  | Right -> (x+1,y)

let moves = function 
  | Up -> 
    [for x in 0..3 do 
       for y in 0..3 do 
         yield x,y] 
  | Down -> 
    [for x in 0..3 do 
       for y in 3..-1..0 do 
         yield x,y] 
  | Left -> 
     [for y in 0..3 do 
        for x in 0..3 do 
          yield x,y] 
  | Right -> 
     [for y in 0..3 do 
        for x in 3..-1..0 do 
          yield x,y]
```

<p>A couple of utility functions. The first is pretty obvious, the second returns a list of tuples indicating the order that the cells should be processed. The order is very important for a number of reasons as will become clear.</p>
```fsharp
let rec move direction data (x,y) (px,py) = 
  match x, y with 
  | -1, _ 
  | _, -1 
  | 4, _ 
  | _, 4 -> (px,py) 
  | x, y when Map.containsKey (x,y) data -> (px,py) 
  | _ -> move direction data (shift (x,y) direction) (x,y)
```

<p>This function takes a location and attempts to move it in the specified direction until either it goes out of bounds, or it finds the location is already taken in the map. In either case, it returns the last good position that can be moved to.</p>
```fsharp
let replace direction data inputs = 
  let move = move direction 
  (data,inputs) 
  ||> List.fold(fun m p -> 
    match move m (shift p direction) p with 
    | newpos when newpos = p -> m 
    | newpos -> let v = m.[p] in m |> Map.remove p |> Map.add newpos v)

let compress direction data = 
  direction 
  |> moves 
  |> List.filter(fun k -> Map.containsKey k data) 
  |> replace direction data
```

<p>These functions effectively &ldquo;compress&rdquo; the map in a specified direction. What this means is that if we are going Up, it will start from the top row, and moving downwards it will move each cell up as far as it can go, resulting in a new compressed map. You can think of this much like defragging memory, but with a direction bias. It&rsquo;s like applying gravity from different directions :)</p>
```fsharp
let merge direction data = 
  let moves = direction |> moves |> Seq.pairwise |> Seq.toList 
  (data,moves) 
  ||> List.fold( fun data ((x,y), (x',y')) -> 
    match Map.tryFind (x,y) data, Map.tryFind(x',y') data with 
    | Some first, Some second when first = second -> 
      data 
      |> Map.remove (x,y) 
      |> Map.remove (x',y') 
      |> Map.add (x,y) (first*2) 
      |_ -> data)
```

<p>This one is a little more fun :) The idea of the merge function is to, based on the direction, merge any pair cells that are touching and have the same value, replacing them with one cell (based on the &ldquo;gravity&rdquo; direction) that has double the value. This code uses pairwise to serve up each pair of locations &ndash; the order that the cells are generated from the moves function is critical here</p>
```fsharp
let step direction = (compress direction) >> (merge direction) >> (compress direction)
```

<p>Using function composition, I can now say that one step of the simulation consists of compressing the map in a certain direction, merging the resulting cells together where appropriate, and then compressing again to fill in any blanks that appeared from the merge step. I think this is pretty awesome :)</p>
<h2>Type Provider</h2>
<p>As mentioned before, this uses my Interactive Provider so there is no gnarly provided types code. Instead, I have a very simple state that gets passed back and forth</p>
```fsharp
type ``2048State`` = 
  | NewGame 
  | GameOn of Map<int*int, int> 
  | GameOver of bool 
    interface IInteractiveState with 
      member x.DisplayOptions: (string * obj) list = 
        match x with 
        | NewGame -> ["Begin Game", box ""] 
        | GameOn(data) -> ["# Show Grid", box "show";"Up", box "up";"Down", box "down";"Left", box "left";"Right", box "right";] 
        | GameOver(true) -> [] 
        | GameOver(false) -> [] 
      member x.DisplayText: string = // omit drawing code for brevity 
```

<p>Very simple .. at the start it shows &ldquo;Begin Game&rdquo; and from then on displays the directional choices as properties along with a &ldquo;# Show Grid&rdquo; property that shows the current state of the grid.</p>
```fsharp
type ``2048``() = 
  interface IInteractiveServer with 
    member x.NewState: IInteractiveState = NewGame :> IInteractiveState 
    member x.ProcessResponse(state: IInteractiveState, response: obj): IInteractiveState = 
      let (|Win|Lose|Continue|) (data:Map<int*int,int>) = 
        ((true,0),[for x in 0..3 do 
                     for y in 0..3 do 
                       yield x,y]) 
        ||> List.fold(fun (b,highest) k -> 
          match Map.tryFind k data with 
          | Some v -> if v > highest then (b,v) else (b,highest) 
          | None -> (false,highest)) 
        |> function 
          | (_, 2048) -> Win 
          | (true, _) -> Lose 
          | _ -> Continue data
 
      match (state:?>``2048State``), (response :?> String).ValueOption() with 
      | NewGame, _ -> 
        let x, y = rnd.Next(0,4), rnd.Next(0,4) 
        GameOn( Map.ofList[(x,y),2]) 
      | GameOn(data), Some "show" -> GameOn(data) 
      | GameOn(data), dir -> 
        let dir = 
          match dir with 
          | Some "left" -> Left 
          | Some "right" -> Right 
          | Some "up" -> Up 
          | Some "down" -> Down 
          | _ -> failwith "" 

        match step dir data with 
        | Win -> GameOver true 
        | Lose -> GameOver false 
        | Continue data -> 
          let rec aux () = 
            let x, y = rnd.Next(0,4), rnd.Next(0,4) 
            if Map.containsKey (x,y) data then aux() 
            else x,y 
          GameOn(data.Add(aux(),2)) 
      | _, _ -> failwith "" 
      |> fun x -> x :> IInteractiveState
```

<p>There is really not a lot to this. The active pattern at the top cycles through all the possible grid states, collecting the highest cells and whether or not all the cells are populated. With this information it can return if the game has been won, lost, or should continue.</p>
<p>When the game is running, it simply looks at the direction that was selected, and pattern matches on the results of calling the composed step function using the active pattern above. Assuming the game is still running, it finds a random location to put a new 2 in, and returns the new data map.</p>
<p></p>
<h2>Conclusion</h2>
<p>Now you can really spend time in Visual Studio playing games instead of working, because this is a lot more fun than minesweeper! 2048 Type Provider Edition for the win!</p>