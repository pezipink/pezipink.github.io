
open System

type Picture = char list list

let horse =
            @".......##...
              .....##..#..
              ...##.....#.
              ..#.......#.
              ..#...#...#.
              ..#...###.#.
              .#....#..##.
              ..#...#.....
              ...#...#....
              ....#..#....
              .....#.#....
              ......##...."

let pictureOfString (input:string) : Picture =
    input.Split([|'\n'|], StringSplitOptions.RemoveEmptyEntries) 
    |> List.ofArray
    |> List.map( fun s -> s.Trim().ToCharArray() |> List.ofArray )
   
let printPicture picture =    
    let printer line =
        line |> List.iter (printf "%c")
        printfn ""
    picture |> List.iter printer    

// flip a picture along the horizontal plane
let flipH = List.rev

// flip a picture along the vertical plane
let flipV = List.map List.rev

// rotate a picture
let rotate = flipH >> flipV

// create one picture by putting two pictures side-by-side
let sideBySide = List.map2 (@)

// create one picture by stacking a picture on top of another picture
let stack = (@)

horse
|> pictureOfString
|> fun pic -> sideBySide (rotate pic) pic
|> fun pic -> stack pic (flipH pic)
|> printPicture