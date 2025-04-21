open System

////////////////////////////////////////////////////////
// Logic gates 
////////////////////////////////////////////////////////
let NOT = function
    | true -> false
    | false -> true

let AND a b =
    match a,b with
    | false, false -> false
    | false, true -> false
    | true, false -> false
    | true, true -> true

let OR a b =
    match a,b with
    | false, false -> false
    | false, true -> true
    | true, false -> true
    | true, true -> true

let XOR a b = 
    match a, b with
    | true, true -> false
    | true, false -> true
    | false, true -> true
    | false, false -> false

let NAND a b = AND a b |> NOT
let NOR a b = OR a b   |> NOT
let XNOR a n = XOR a n |> NOT

////////////////////////////////////////////////////////////
// Utility functions for converting between bool and binary
////////////////////////////////////////////////////////////

// takes a number and converts it into a binary list
let binOfInt input pad = 
    let rec aux acc pad n =
        match n,pad with
        | 0UL,pad when pad <= 0UL -> acc
        | 0UL,pad -> aux (0UL::acc) (pad-1UL) 0UL
        | n,pad -> aux ((n&&&1UL)::acc) (pad-1UL) (n>>>1) 
    aux [] pad input 

let decOfBin input =
    Convert.ToUInt64(input,2).ToString()

let stringOfList input =
    input 
    |> List.fold( fun (acc:System.Text.StringBuilder) e -> acc.Append(e.ToString())) (System.Text.StringBuilder())
    |> fun sb -> sb.ToString()

let createInputs start max pad =    
    [for x in start..max do yield binOfInt x pad]

let boolOfBin = function 0UL -> false | _ -> true
let binOfBool = function false -> 0UL | true -> 1UL
let bitsOfBools = List.map binOfBool >> stringOfList

////////////////////////////////////////////////////////////
// Testing framework 
////////////////////////////////////////////////////////////
let rnd = System.Random(DateTime.Now.Millisecond)

type InputType = | All | Range of uint64  
type TestTemplate =
    abstract member NumberOfBits : uint64
    abstract member Execute : (bool list -> unit)
    abstract member InputType : InputType

let test (template:TestTemplate) = 
    let max = uint64(2.0**float(template.NumberOfBits)) - 1UL
    match template.InputType with
    | All -> createInputs 0UL max template.NumberOfBits
    | Range(n) -> let n = if n >= max then max else n                  
                  let start = uint64(rnd.NextDouble() * float(max-n))
                  createInputs start (start+n) template.NumberOfBits
    |> List.map (List.map boolOfBin)
    |> List.iter template.Execute

////////////////////////////////////////////////////////////
// Circuit implementations
////////////////////////////////////////////////////////////

let halfAdder (a,b) =
    (AND a b,XOR a b)       

let fullAdder (a,b) c =
    let (c1,s1) = halfAdder (a,b)
    let (c2,s) = halfAdder (s1,c)
    (OR c1 c2,s)

let nAdder inputs =
    let rec aux c results = function        
        | ab::xs -> 
            let (c1,s) = fullAdder ab c
            aux c1 (s::results) xs
        | [] -> (results  , c)
    aux false [] inputs

////////////////////////////////////////////////////////////
// Test templates
////////////////////////////////////////////////////////////

let testHalfAdder() = 
    test
        { new TestTemplate with 
            member x.InputType = All
            member x.NumberOfBits = 2UL
            member x.Execute = function 
                | a::b::[] -> 
                    let (c,s) = halfAdder (a, b)
                    printfn "%i\t%i\t%s" (binOfBool a) (binOfBool b) (stringOfList([binOfBool c;binOfBool s]))
                | _ -> failwith "incorrect inputs" }

let testFullAdder() = 
    test
        { new TestTemplate with 
            member x.InputType = All
            member x.NumberOfBits = 3UL
            member x.Execute = function 
                | a::b::c::[] -> 
                    let (co,s) = fullAdder (a,b) c
                    printfn "%i\t%i\t%i\t%s" (binOfBool a) (binOfBool b) (binOfBool c) (stringOfList([binOfBool co;binOfBool s]))
                | _ -> failwith "incorrect inputs" }   

let testNBitAdder n = 
    test 
        { new TestTemplate with 
            member x.NumberOfBits = (n*2UL)
            member x.InputType = Range(32UL)
            member x.Execute = fun input ->                
                let rec buildInput acc = function
                    | a::b::xs  -> buildInput ((a,b)::acc) xs
                    | [] -> acc
                    | _ -> failwith "incorrect input"
                // exctract input and format the full A and B numbers ready for displaying
                let input = buildInput [] input
                let (A,B) = input |> List.unzip |> fun (A,B) -> (List.rev A |> bitsOfBools, List.rev B |> bitsOfBools )
                // perform the calculation and format result as a binary string
                let result = nAdder input |> fun (out,c) -> (binOfBool c)::(List.map binOfBool out) |> stringOfList
                // print input number A, B and the result, all in binary
                printf "%s %s %s"  A B result
                // print it again in decimal to show the equivalent sum
                printfn " : %s + %s = %s"  (decOfBin A) (decOfBin B) (decOfBin result) }
