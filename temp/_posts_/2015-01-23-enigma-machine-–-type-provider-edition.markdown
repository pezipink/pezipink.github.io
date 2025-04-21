    Title: Enigma Machine – Type Provider Edition
    Date: 2015-01-23T07:28:00
    Tags: F#, type providers

<p>Following up from <a href="https://twitter.com/isaac_abraham">@isaac_abraham&rsquo;s</a> awesome <a href="https://cockneycoder.wordpress.com/2014/12/24/demystifying-the-enigma-machine-with-f/">F# Enigma machine emulator</a>, I decided it would be 10x cooler if it was in a type provider, because let&rsquo;s face it, everything is 10x cooler once it&rsquo;s in a type provider.</p>
<p>Here a some pictures of it in action!</p>
<p><a href="http://pinksquirrellabs.com/image.axd?picture=enigma1.jpg"><img style="display: inline; border-width: 0px;" title="enigma1" src="http://pinksquirrellabs.com/image.axd?picture=enigma1_thumb.jpg" alt="enigma1" width="538" height="338" border="0" /></a> <a href="http://pinksquirrellabs.com/image.axd?picture=enigma2.png"><img style="display: inline; border-width: 0px;" title="enigma2" src="http://pinksquirrellabs.com/image.axd?picture=enigma2_thumb.png" alt="enigma2" width="604" height="338" border="0" /></a></p>
<!-- more -->

<p>As you can see, it uses an extensive property system that presents a menu along with the various controls to setup your enigma machine, and then finally to translate some text. This TP is written with my <a href="https://github.com/pezipink/InteractiveProvider">InteractiveProvider </a>as per usual. On my first attempt at this during my lunch break today, I did succeed but I was growing increasingly frustrated with the somewhat contrived mechanism with which to process responses from properties, the text to display in intellisense, and the properties to show.</p>
<p></p>
<h2>The Old System</h2>
<p>The InteractiveProvider (henceforth known as IP) presents a very flexible yet slightly complicated interface with which to generate types. Essentially, you implement <em>IInteractiveState</em> on some state object of your design, and <em>IInteractiveServer</em> on another type, which deals with processing responses. The IP will display intellisense and options via the state, and when the user selects a property, the server decides what to do with it and returns some new state.</p>
<p>This is very cool as your state object can be whatever you like &ndash; record types, DU&rsquo;s, or full classes. Responses to property access can similar be of any type you like, and can be different on each property if you like. The problems with this system are</p>
<ol>
<li>The creation of the text and properties is separate from the response handling of that property. This can make it hard to read and reason about.</li>
<li>It is a bit unsafe because everything gets boxed, unboxed, and each time the server has to deal with a response, based on the current state you have to make sure you are dealing with the correct types coming back from the TP which can be a hit messy and detract from what you are really trying to do</li>
<li>Although each state might require its own unique data, there is not any way to represent one thing without threading all the previous state through. For example, if I want the user to enter a bunch of letters via properties until they press the [End] property, I can&rsquo;t do this with just a string, Id have to carry the rest of whatever data I was using as well. This gets unwieldy quickly. There is no way to separate concerns.</li>
<li>Again on with point 3, because of this it is not really possible to create re-usable chunks of state that perform common functions such as accepting input.</li>
</ol>
<p></p>
<h2></h2>
<h2>The new system</h2>
<p>In order to address this, I introduced another layer of abstraction, &lsquo;cos you can never have too many layers of abstraction right? :)</p>
```fsharp
type InteractiveState<'a> = 
 { displayOptions : 'a -> (string * obj) list 
 displayText : 'a -> string 
 processResponse : 'a * obj -> IInteractiveState 
 state : 'a } 
 member x.ProcessResponse o = x.processResponse (x.state, o) 
 interface IInteractiveState with 
 member x.DisplayOptions = x.displayOptions x.state 
 member x.DisplayText = x.displayText x.state
```

<p>This new record type is essentially a super-duper state object. It brings together the creation of stuff and the processing of responses into the same place. You can see it takes 3 functions and a bit of state, &lsquo;a.</p>
<ul>
<li><em>displayOptions </em>will be called with the current <em>&lsquo;a</em> and is expected to generate a list of properties to display and a boxed version of some type that will be passed back to the server when the user selects that property.</li>
<li><em>displayText</em> will be called with the current <em>&lsquo;a</em> and used to generate what appears in intellisense when this type is currently selected (more on this later)</li>
<li><em>processResponse </em>will be called with the current &lsquo;<em>a</em> and is expected to return a new <em>IInteractiveState</em></li>
</ul>
<p>Because this is a generic type, it does mean when you start to use these together, they are going to need the same <em>&lsquo;a</em> which is a bit of a pain, but it is readily solved by creating a DU of all the possible types that the various states in your system need.</p>
```fsharp
type EnigmaTypes = 
 | Core of EnigmaCore.Enigma 
 | Strings of string 
 | Rotors of MachineRotor 
 with 
 member x.Enigma = match x with Core e -> e | _ -> failwith "" 
 member x.String = match x with Strings s -> s | _ -> failwith "" 
 member x.Rotor = match x with Rotors r -> r | _ -> failwith ""
```

<p>This is not very nice but a very reasonable trade off for the power attained. Now the server object itself becomes very simple (infact this can be generalized as well)</p>
```fsharp
type Enigma() = 
 interface IInteractiveServer with 
 member x.NewState: IInteractiveState = start() :> IInteractiveState 
 member x.ProcessResponse(state: IInteractiveState, response: obj): IInteractiveState = 
 let state = (state:?>InteractiveState<EnigmaTypes>) 
 state.ProcessResponse(response)
```

<h2></h2>
<h2>Start Your Engines!</h2>
<p>Now the system is ready to rock! You will notice when the server starts it calls <em>start()</em> to obtain its first state. All the states are now just instances of record types. <em>start()</em> looks like this</p>
```fsharp
let start() = 
 { displayOptions = fun _ -> ["Begin!",box ()] 
 displayText = fun _ -> "Welcome to the type provider Enigma machine!" 
 processResponse = fun (e,_) -> mainMenu(e) :> _ 
 state = Core defaultEnigma }
```

<p>this is as simple as it gets and not doing much interesting, you can see it returns one property &ldquo;Begin!&rdquo; along with a boxed unit type. I don&rsquo;t care about the response type as there is only one property so I know it must be that being selected.</p>
<p><em>processResponse</em> simply creates the next state using the function <em>mainMenu( .. )</em> which it passes the current state, in this case the default version of the enigma machine.</p>
<p><em>mainMenu(..)</em> is much more interesting and too long to show here, so I will show some extracts / condensed versions</p>
```fsharp
type MainMenuResponses = 
 | Nothing 
 | SelectLeftRotor 
 | SelectMiddleRotor 
 | SelectRightRotor 
 | SelectReflector 
 | SetWheelPosition 
 | SetRingPosition 
 | CreatePlugMapping 
 | Translate

let rec mainMenu(enigma:EnigmaTypes) =

{ displayOptions = fun _ -> ["# ",box Nothing; 
 "Select a new left rotor",box SelectLeftRotor 
 "Select a new middle rotor",box SelectMiddleRotor ... ] 

 displayText = fun _ -> printMachine enigma.Enigma 
 processResponse = fun (e,r) -> 
 match unbox&lt;MainMenuResponses> r with 
 | Nothing -> mainMenu(e) 
 | SelectLeftRotor -> 
 enterText("",[for i in 1..8-> sprintf "Rotor %i" i, box (string i)], 
 (fun s -> "Choose the rotor to place on the left"), 
 (fun s -> 
 let e = { e.Enigma with Left = getRotor s , WheelPosition 'A' } 
 mainMenu(Core e) :> IInteractiveState), 
 (fun _ -> false) )
```

<p></p>
<p>The first bits are pretty straight forward, it shows the various menu options and boxes which one was selected using the DU defined above. <em>processResponse</em> then unboxes the return value, matches on it, then does something with the result.</p>
<p>In this case, it is calling another function called <em>enterText &ndash;</em> and this is where it gets really cool! <em>enterText</em> is defined as follows</p>
```fsharp
let rec enterText(state:string,options,genDisplayText,continuation,repeatCondition) = 
 { displayOptions = fun _ -> options 
 displayText = fun d -> genDisplayText d 
 processResponse = fun (current:EnigmaTypes,c) -> 
 let s = current.String + string c 
 if repeatCondition s then enterText(s,options,genDisplayText,continuation,repeatCondition) :> _ 
 else continuation s 
 state = Strings state }
```

<p>This function is designed based on the observation that when I want to accept an arbitrary amount of text from the user, the following is required</p>
<ol>
<li>A list of what inputs to be shown</li>
<li>A way of knowing when to stop accepting more inputs (and recursively creating more types)</li>
<li>A continuation function, that accepts the completed output and then generates some other state.</li>
</ol>
<p>What is really awesome with is is that the <em>enterText</em> function simply takes a string &ndash; it doesn&rsquo;t know or care about the Enigma object &ndash; this is made possible by the fact that we can now create a closure over the previous state&rsquo;s data within the continuation lambda function, allowing us to decouple the recursive-text-entering portion of the type system. Very nice!</p>
<h2>Engage Turbo Mode!</h2>
<p>Great! now I can create re-usable state chunks and control stuff via closures. However, there is one more usually contrived problem that this system solves very well. Let&rsquo;s take the menu function <em>Adjust Wheel Position</em>. This one is a little bit of a pain because it requires several steps &ndash; first you must pick which wheel you want to manipulate, then you choose the letter you wish to set it to. Usually you would have to model these as separate states, which would be confusing if you wanted to do the same thing somewhere else - but now you can actually <em>compose</em> these functions and closures together so that the whole intent and flow is clear within the same definition. For example :</p>
```fsharp
let selectMachineRotor(continutation) = 
 { displayOptions = fun _ -> 
 ["Left Wheel", box LeftRotor 
 "Middle Wheel", box MiddleRotor 
 "Right Wheel", box RightRotor] 
 displayText = fun d -> "Select a rotor." 
 processResponse = fun (current,c) -> 
 continutation (c:?>MachineRotor) 
 state = Rotors MachineRotor.LeftRotor }
```

<p>This function accepts a continuation function and asks the user to select a wheel, and calls the continuation function with their choice</p>
```fsharp
| SetRingPosition -> 
 let apply l = function 
 | LeftRotor -> { e.Enigma with Left = { fst e.Enigma.Left with RingSetting = l }, snd e.Enigma.Left } 
 | MiddleRotor -> { e.Enigma with Middle = { fst e.Enigma.Middle with RingSetting = l }, snd e.Enigma.Middle} 
 | RightRotor -> { e.Enigma with Right = { fst e.Enigma.Right with RingSetting = l }, snd e.Enigma.Right }

 selectMachineRotor(fun rotor -> 
 enterText("",[for i in 'A'..'Z' -> sprintf "%c" i, box (string i)], 
 (fun s -> "Choose a letter"), 
 (fun s -> 
 let e = apply (RingSetting s.[0]) rotor 
 mainMenu(Core e) :> IInteractiveState), 
 (fun _ -> false) ) :> IInteractiveState )
```

<p>When the <em>SetRingPosition</em> menu item is selected, it returns the <em>selectMachineRotor</em> function, and the continuation function passed to uses the <em>enterText</em> function allowing the user to pick a letter, and finally the result is applied to the Enigma object and the whole thin is returned back to the main menu. Very cool!</p>
<p>Straight away this is useful as the <em>AdjustWheelPosition</em> menu item has to do a very similar thing</p>
```fsharp
| SetWheelPosition -> 
 let apply l = function 
 | LeftRotor -> { e.Enigma with Left = (fst e.Enigma.Left, l) } 
 | MiddleRotor -> { e.Enigma with Middle = (fst e.Enigma.Middle, l) } 
 | RightRotor -> { e.Enigma with Right = (fst e.Enigma.Right, l) }

 selectMachineRotor(fun rotor -> 
 enterText("",[for i in 'A'..'Z' -> sprintf "%c" i, box (string i)], 
 (fun s -> "Choose a letter"), 
 (fun s -> 
 let e = apply (WheelPosition s.[0]) rotor 
 mainMenu(Core e) :> IInteractiveState ), 
 (fun _ -> false) ) :> IInteractiveState )
```

<p></p>
<h2>Conclusion</h2>
<p>The IP has had a bit of a face-lift which makes it easier to write and read what is going on. Plus you can have an Enigma machine in a type provider. Who wouldn&rsquo;t want that! The code is a little bit of a mess at the moment, but I should clean it up soon and move the new super-state into the common interfaces project.</p>