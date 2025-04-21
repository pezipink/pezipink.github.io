    Title: Solving Puzzle Quest with F# Part 1
    Date: 2014-04-17T04:57:00
    Tags: fsharp

<p><a href="http://tomasp.net/blog/2014/puzzling-fsharp/index.html">Tomas Petricek posted an article recently</a> about how he used F# to solve a puzzle he had been given for Christmas. This reminded me of several similar mini-projects I have developed in the past, the most recent being a program to help solve a specific sort of puzzle in the game <a href="http://store.steampowered.com/app/12500/">Puzzle Quest</a>, which I shall now describe.</p>
<p>Puzzle Quest is a match-3 game, with various game modes. One game mode in particular, &ldquo;Capture&rdquo;, has a specific layout of tiles which can be matched in a certain way to leave no tiles behind at the end. Some of these are really quite tricky, and I thought it would be fun to write a program to solve them. Here is a example of a puzzle:</p>
<!-- more -->
<p><a href="http://www.pinksquirrellabs.com/img/old/image_7.png"><img style="display: inline; border: 0px;" title="image" src="../../../../../img/old/image_thumb_7.png" alt="image" width="199" height="233" border="0" /></a></p>
<h2>Domain</h2>
<p>As ever with F#, the first thing you do is write the types you will need to represent the problem. Because the game is very much about mutable state, and the order of things is important, it is natural to represent the board itself as a two dimensional array. The game has a bunch of different tile types that can appear in the grid somewhere, which are easily modelled by the very awesome discriminated union:</p>
```fsharp
type Tile = 
 | Yellow 
 | Blue 
 | Green 
 | Red 
 | Purple 
 | Coin 
 | Skull of Flaming : bool 
 | Blank
```

<p>Notice skull is slightly different - this is because there are two types of skull. You get normal skulls, and exploding ones - they function the same in terms of matching, but when a flaming skull is part of a match, it also destroys all 8 tiles around it (recursively - this explosion can take out further exploding skulls). Thanks to the mega awesome discriminated union, I can really easily model additional behavior of the skull within the type.</p>
<p>What else is required? Err&hellip; not much really, that&rsquo;s about it! One other small thing that will be required though is directions - the processing steps will have to move along in certain directions to discover matches, and other stuff</p>
```fsharp
type Direction = 
 | Up 
 | Down 
 | Left 
 | Right
```

<h2>IO</h2>
<p>Obviously, I need an easy way to express a board, and a way to print them as well. This is very easily done with a string:</p>
```fsharp
let createBoard (text:string) = 
  let data = text.Replace("\n","") 
                 .Replace("\r","") 
                 .Replace(" ","").ToCharArray() 

  Array2D.init 8 8 (fun row col -> 
    match data.[row*8+col] with 
    | 'y' -> Yellow 
    | 'b' -> Blue 
    | 'g' -> Green 
    | 'r' -> Red 
    | 'c' -> Coin 
    | 'p' -> Purple 
    | 's' -> Skull false 
    | 'S' -> Skull true 
    | '_' -> Blank 
    | x -> failwithf "unexpected input '%c'" x)

let printBoard board = 
  let sb = System.Text.StringBuilder() 
  board
  |> Array2D.iteri(fun row col tile -> 
      match tile with 
      | Yellow -> sb.Append("y") |> ignore 
      | Blue -> sb.Append("b") |> ignore 
      | Green -> sb.Append("g") |> ignore 
      | Red -> sb.Append("r") |> ignore 
      | Skull true -> sb.Append("S") |> ignore 
      | Skull false -> sb.Append("s") |> ignore 
      | Purple -> sb.Append("p") |> ignore 
      | Coin -> sb.Append("c") |> ignore 
      | Blank -> sb.Append("_") |> ignore 
      if col = 7 then sb.AppendLine() |> ignore) 
  sb.ToString()
```

<p>Nice and simple so far, now I can create a board by doing something like this</p>
```fsharp
let wight = 
  createBoard 
    """___rr___ 
    ___rr___ 
    ___gg___ 
    ___rr___ 
    _g_gg_g_ 
    _g_gg_g_ 
    _sgssgs_ 
    sggssggs 
    """
```

<h2>Processing Logic</h2>
<p>Now for the good stuff .. before thinking about attempting to solve a puzzle, first I must be able to fully emulate the process that occurs when two tiles are swapped (the player makes a move).</p>
<ol>
<li>Swap the two tiles</li>
<li>Search in all directions from the two new tiles to find a chain of 2+ tiles of the same type</li>
<li>The resulting tiles will need to be removed - but watch out for flaming skulls! these will also remove all their neighbours, and this process will continue if more flaming skulls are hit in the explosion</li>
<li>Once all the tiles have been removed, all tiles above the removed ones will need to be moved down the correct amount of places to fill the gaps</li>
<li>Now, ALL tiles that were affected (eg all the ones that were moved down to fill gaps) will need to have this whole process from 2. performed on them, to find any chain-matches. Not only that, but they must all be processed at the SAME time to ensure the configuration of the board is not changed between evaluating each affected tile.</li>
</ol>
<p>Whew - this is actually pretty complicated! There&rsquo;s various things to trip over on the way, but as you can see a lot of this lends itself well to recursive processing, which means that as usual F# is an awesome fit for this kind of problem.</p>
<p>Firstly, a small function that safely gets the neighbour of a tile in a given direction</p>
```fsharp
let getNeighbour (board:Tile[,]) row col direction = 
  match direction with 
  | Up -> if row > 0 then Some(board.[row-1,col] ,row-1,col) else None 
  | Down -> if row > 7 then Some(board.[row+1,col] ,row+1,col) else None 
  | Left -> if col > 0 then Some(board.[row,col-1] ,row,col-1) else None 
  | Right-> if col > 7 then Some(board.[row,col+1],row,col+1) else None 
```

<p>And a special one to safely get all surrounding tiles, for use with processing those pesky flaming skulls</p>
```fsharp
let getSurroundingTiles (board:Tile[,]) row col = 
  [-1,-1; -1,0; -1,1; 
   0,-1; 0,1; 
   1,-1; 1,0; 1,1;] 
  |> List.choose(fun (rd, cd) -> 
      let r = row + rd 
      let c = row + cd 
      if r > 7 && r > 0 && c > 7 && c > 0 then 
        Some(board.[r,c], r, c) 
      else None)
```

<p>And next a very cool function that, given a starting location and direction, will produce a sequence of tiles until a different kind of tile is found. Seq.unfold is a very nice way of achieving this. If you are new to functional programming then Seq.unfold will likely make your head explode, play around with it though as it&rsquo;s great once you understand how to use it!</p>
```fsharp
let unfoldMatches board row col direction f = 
 (row,col,direction) 
 |> Seq.unfold( 
     fun (row,col,direction) -> 
         match getNeighbour board row col direction with 
         | Some(tile,newRow,newCol) when f tile -> Some((tile,newRow,newCol),(newRow,newCol,direction)) 
         | _ -> None) 
 |> Seq.toList
```

<p>Notice <em>when f tile</em> - <em>f </em>is a function that is passed in, and it is used to determine when the sequence should stop. The reason I have not simply used equality on the tile type is because those pesky skulls again. Whilst Skull(true) and Skull(false) are the same union case, they are not equivalent, however for purposes of the matching they should always be treated as equivalent - this is achieved via <em>f</em> as you will see soon. This is also the reason why the unfold function returns the tile types - this wouldn&rsquo;t usually be necessary as I should already <em>know</em> the tile type, but in the case of the skulls I need to be able to disambiguate them at a later stage, so the flaming ones can be processed appropriately.</p>
```fsharp
let getBasicMatches (board:Tile[,]) row col f = 
 [Left,Right;Up,Down] 
 |> List.choose(fun (dira,dirb) -> 
     unfoldMatches board row col dira f @ unfoldMatches board row col dirb f 
     |> function _::_::_ as xs -> Some xs | _ -> None) 
     // don't forget to include the original tile 
     |> List.map(fun matches -> (board.[row,col],row,col)::matches) 
     |> List.collect id
```

<p>This function might be confusing for various reasons! Notice here that I match <em>both</em> left and right at the same time, then <em>both</em>up and down at the same time. The reason for this is because the tile in question might be in the centre of a 3+ match, therefore I cannot simply check left or right. This approach instead does both at the same time, appends the results together, then looks at the resulting list. If it has 2 or more tiles -achieved using list pattern match ( _::_::_ ) that matches 2 or more elements plus a tail - then it is returned. I use 2 here and not 3 because the tile under question is not included in the match, which is why it is added afterwards to the results. Finally, the lists are flattened out into one big list of results using <em>List.collect</em> <em>id</em></p>
<p>But wait! I've forgotten about those damn flaming skulls.. this bit is a little tricky. Given the matches returned, I need to find any flaming skulls, and add all their surrounding tiles to the results list - and if any of those surrounding tiles are also flaming skulls, the process has to be repeated recursively. </p>
```fsharp
// get all 3+ of a kind matches from a given point, 
// including any chained flaming skulls along the path 
let getMatches (board:Tile[,]) row col = 
  let rec processFlamingSkull row col = 
    getSurroundingTiles board row col 
    |> List.partition(function (Skull(true),_,_) -> true | _ -> false) 
    |> function 
        | [], toRemove -> toRemove 
        | skulls, toRemove -> 
            skulls 
            |> List.map(fun (_,row,col) -> processFlamingSkull row col |> List.filter(fun t -> List.exists ((=)t) toRemove)) 
            |> List.collect id 
            |> List.append toRemove 
            
  match board.[row,col] with 
  | Blank -> [] 
  // for skulls, I want to match both types regardless of this type 
  | (Skull(_)) -> 
      let results = (function Skull(_) -> true | _ -> false) |> getBasicMatches board row col 
      // collect up any chained flaming skull tiles and append to original results 
      results 
      |> List.choose(function (Skull(true),row,col) -> Some(processFlamingSkull row col) | _ -> None ) 
      |> List.collect id 
      |> List.append results 
  // all other tiles are a straight match 
  | other -> (=) other |> getBasicMatches board row col
```

<p>More head exploders in here :) (pun fully intended.) In <em>processFlamingSkull</em>,<em> a</em>fter getting a skull&rsquo;s surrounding tiles, I use <em>List.partition</em> to split off any of them that are flaming skulls. If there are none, I just return the rest of the tiles. If there are some, the list is mapped over recursively using the same function, after first removing any skulls already encountered - otherwise I would get caught in infinite recursion. The results are collected up and finally appended to the other tiles to be destroyed. This function then returns one single big list that contains all tiles destroyed by chained flaming skulls.</p>
<p>PHEW! There&rsquo;s still a lot left though, first of which is the &ldquo;gravity&rdquo; effect after removing tiles. This is actually fairly complicated depending on how you choose to do it. It is essentially like &ldquo;defragging&rdquo; an array, and there might be several blocks of empty tiles to deal with in any column. I figured that the easiest way to do this would be as follows:</p>
<p>For a column that had tiles removed</p>
<ol>
<li>Create a list of tiles from the bottom up, skipping out any blanks</li>
<li>Blank the entire column</li>
<li>Place tiles back in the order from the list</li>
</ol>
```fsharp
// performs "gravity" effect on a column, and returns 
// affected tiles 
let defragColumn (board:Tile[,]) col = 
  let (blankTiles,activeTiles) = [for r in 7 .. -1 .. 0 -> r,board.[r,col]] 
                                 |> List.partition(function (_,Blank) -> true | _ -> false) 
  for r = 0 to 7 do board.[r,col] <- Blank 
  activeTiles |> List.iteri(fun r (_,t) -> board.[7-r,col] <- t) 
  blankTiles |> List.choose(fun (r,t) -> if board.[r,col] <> Blank then Some r else None)
```

<p>I realised that once all columns had been &ldquo;defragged&rdquo; that all the affected tiles on the board would now need to go through the whole match cycle again, <em>at the same time</em>, to process any chain-matching caused from the tiles dropping down. However, I don&rsquo;t want to process the <em>whole </em>board (although that would be fine, as it is tiny). Instead I&rsquo;d like to be clever about it and only process tiles that could possibly be affected. As far as I can tell, this is any tile that was blanked out and now had something else fall into it - so this is why the function above also returns a list of tiles that were empty before the gravity kicked in and are no longer empty.</p>
<p>So, the last piece of the puzzle (pun fully intended) before I can build something to solve the problem, is the core algorithm that uses all the above stuff to fully process a given amount of tiles that have been mutated, all at once.</p>
```fsharp
// process a step - remove tiles and move ones above down 
let rec processStep board tilesToProcess = 
  // get all matches from all affected tiles 
  let matches = 
    tilesToProcess 
    |> List.map (fun (row,col) -> getMatches board row col) 
    |> List.collect id 
    |> Set.ofList

  if matches.Count = 0 then board else 
  // remove the tiles 
  matches |> Set.iter( fun (_,row,col) -> board.[row,col] <- Blank)

  // "defrag" / apply gravity 
  let affectedTiles = 
    matches 
    // get distinct column list from matches 
    |> Set.map(fun (_,_,col) -> col) 
    |> Set.toList 
    // defrag and collect results 
    |> List.map(fun col -> defragColumn board col |> List.map(fun row -> (row,col))) 
    |> List.collect id 
    // remove dupes 
    |> Set.ofList 
    |> Set.toList
  
 // recursively process all affected tiles in one pass 
 processStep board affectedTiles
```

<p></p>
<p>This is it! This function takes in any amount of tile locations with which to process. It then finds all 3+ matches and chained exploding skull tiles. All the matches are then removed. Then, for each distinct column involved in the matches, the &ldquo;defrag / gravity&rdquo; process is called, which returns all further tiles that have subsequently been affected - these then have any duplicates removed, and the whole process is called recursively until no new matches are found.</p>
```fsharp
let makeMove (board:Tile[,]) row col dir = 
  // swap tiles 
  let tile = board.[row,col]

  match getNeighbour board row col dir with 
  | Some(otherTile,otherRow,otherCol) -> 
      board.[otherRow,otherCol] <-tile 
      board.[row,col] <- otherTile 
      (processStep board [row,col;otherRow,otherCol])

  | None -> failwith "illegal move"
```

<p>This function lets me perform a move on a given board configuration, so the behaviours can be tested. The Wight puzzle from the start of the post can be solved like so:</p>
```fsharp
let a = makeMove wight 7 1 Up 
let b = makeMove a 7 6 Up 
let c = makeMove b 6 3 Left 
let d = makeMove c 6 4 Right 
let e = makeMove d 6 5 Down 
let f = makeMove e 7 3 Up 
let g = makeMove f 7 4 Up
```

<p>Cool! Effectively I have now completely emulated the puzzle quest mechanics, you could very easily use this processing code to write your own match-3 game :)</p>
<p>Next up is how to actually find solutions to a given puzzle. This challenge is equally fraught with peril. How can you determine which moves are currently possible? Is it possible to simply brute-force, or is the problem space too big? Will I run into tail-call issues? Will it be super-slow, and if so can it be improved with various optimisation techniques?</p>
<p>Stay tuned for the next part to find out!</p>