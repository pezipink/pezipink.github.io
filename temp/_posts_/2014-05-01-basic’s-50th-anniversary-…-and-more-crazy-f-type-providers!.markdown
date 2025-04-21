    Title: BASIC’s 50th Anniversary … and more crazy F# type providers!
    Date: 2014-05-01T08:56:00
    Tags: fsharp, type providers
<!-- more -->

<p>Did you know that it is the <a href="http://time.com/69316/basic/">50th anniversary of the BASIC programming language today?</a> (1st May) No? Well why not! BASIC is the language that brought computers to the mainstream. Back in the day, if you had a computer, you learnt how to program it in basic. Infact, when you switched it on, that&rsquo;s what you were presented with. These computers <em>wanted</em> to be programmed. Thousands of people purchased these expensive computers purely to learn how to program them.</p>
<p>I figured I should do something to mark the anniversary. Many of you will already know about my various crazy type providers, including <a href="http://pinksquirrellabs.com/post/2014/02/02/The-MineSweeper-Type-Provider.aspx">MineSweeper</a> and <a href="http://pinksquirrellabs.com/post/2013/07/29/Choose-Your-Own-Adventure-Type-Provider.aspx">Choose Your Own Adventrue</a>. So, I thought to myself, wouldn&rsquo;t it be great if we could write some equivalents of the very early BASIC games via a re-usable and extensible type provider? One which doesn&rsquo;t require you to write any type-providing code, but is abstracted away from all that &hellip;</p>
<h2>InteractiveProvider</h2>
<p>Enter <em>InteractiveProvider</em> (which I just wrote this afternoon). Unfortunately, it doesn&rsquo;t yet support any BASIC (although <a href="http://trelford.com/blog/post/interpreter.aspx">Phil Trelford&rsquo;s Small Basic interpreter</a> is looking like a good fit). What it does do is abstract away all the voodoo magic of infinitely-recursive-type-providers, and allow you to write some fairly sophisticated type provider games by implementing a couple of interfaces. The type provider itself will scan assemblies in a given location to find types that implement the interfaces it uses, and the rest happens JUST LIKE MAGIC! There&rsquo;s not much content yet, I have converted some simple BASIC games from <a href="http://www.atariarchives.org/basicgames/">the legendary 101 BASIC games book</a>, and some of the my other type providers to work with it as examples, although I have some special plans up my sleeve for new games !</p>
<p>To use it, grab and build the source from my <a href="https://github.com/pezipink/InteractiveProvider">github</a> and do something like the following in your friendly FSI session</p>
```fsharp
#r @"F:\git\InteractiveProvider\InteractiveProvider\bin\Debug\InteractiveProvider.dll"

open PinkSquirrels.Interactive

type GamesType = InteractiveProvider< @"F:\git\InteractiveProvider\BASIC_Anniversary\bin\Debug\">

let games = GamesType()
```

<p>The static parameter here is the directory to search for assemblies containing compatible games. Now when you press dot on the <em>games </em>value, you will be presented with a series of properties named &lsquo;<em>Start <game>&rsquo;</em>, one for each type it found implementing said interfaces (more on that below)</p>
<h2></h2>
<h2>THE GAMES!</h2>
<p>There are currently a massive 3 games.</p>
<h3>MineSweeper</h3>
<p>Ronseal. See <a href="http://pinksquirrellabs.com/post/2014/02/02/The-MineSweeper-Type-Provider.aspx">this post </a>for more informations</p>
<p><a href="http://pinksquirrellabs.com/image.axd?picture=image_8.png"><img style="display: inline; border: 0px;" title="image" src="http://pinksquirrellabs.com/image.axd?picture=image_thumb_8.png" alt="image" width="793" height="263" border="0" /></a></p>
<h3></h3>
<h3>Rock Paper Scissors</h3>
<p>Try your hand against the computer in this classic game!</p>
<p><a href="http://pinksquirrellabs.com/image.axd?picture=image_9.png"><img style="display: inline; border: 0px;" title="image" src="http://pinksquirrellabs.com/image.axd?picture=image_thumb_9.png" alt="image" width="642" height="190" border="0" /></a></p>
<h3>Chemistry</h3>
<p>My personal favourite, this introduces another ground-breaking type provider mechanic, the ability to input any sized number via successive properties! In this game you are a chemist and you must correctly dilute the fictional KRYPTOCYANIC acid.</p>
<p></p>
<p><a href="http://pinksquirrellabs.com/image.axd?picture=image_10.png"><img style="display: inline; border: 0px;" title="image" src="http://pinksquirrellabs.com/image.axd?picture=image_thumb_10.png" alt="image" width="837" height="218" border="0" /></a></p>
<p></p>
<p></p>
<h2>Writing Games</h2>
<p>By this point you must be just crying out to write your own games. And I too am crying out for your pull requests. The way it works is as follows.</p>
<p>There are two interfaces. IInteractiveState and IInteractiveServer</p>
```fsharp
type IInteractiveState= 
  abstract member DisplayText : string 
  abstract member DisplayOptions : (string * obj) list

type IInteractiveServer = 
  abstract member NewState : IInteractiveState 
  abstract member ProcessResponse : IInteractiveState * obj &ndash;> IInteractiveState
```

<p>The type provider will pick up on any types that implement the server interface. Upon doing so, it will call Activator.CreateInstance and create an instance of the server type. From this type it will get an initial state from the <em>NewState</em> property.</p>
<p>The state interface returns information to the type provider via <em>DisplayOptions </em>about what choices it should surface as properties, along with objects to pass back to the server when a property is selected. It also has a property <em>DisplayText</em> which is a string that will be placed on the property that leads to this state. In some games this is not desirable as it gives the player a preview of the next state, however there is a way around that (see <em>Rock Paper Scissors</em>)</p>
<p>The objects themselves that implement these interfaces can be literally anything you like at all. The only caveat is that you must store all the information in the state which you require. When a property is accessed, the old state gets passed to the <em>ProcessResponse </em>function along with the object representing the selection, which can be anything, as previously defined in the <em>DisplayOptions</em> . In <em>MineSweeper </em>this is the int * int tuple of the grid square that was selected. In <em>Chemistry</em> it is a discriminated union type. <em>Rock Paper Scissors </em>uses something different again.</p>
<p>Often it is a good choice to use a record type or discriminated union for your state. <em>Rock Paper Scissors </em>simply holds everything it needs to know about the game in one record type which both interfaces use to define their next behaviour. <em>Chemistry </em>uses multiple discriminated union cases and lots of pattern matching to work out the behaviour.</p>
<h2></h2>
<h2>Example</h2>
<p>Here&rsquo;s a very simple example that has the player guess a number from 1 to 100. It uses a discriminated union to model the different states. There is no fail condition, you can just keep guessing until you win :)</p>
```fsharp
type ExampleState = 
  | Start of target: int 
  | Guess of lastGuess : int * target : int 
  | Success 
    interface IInteractiveState with 
      member this.DisplayText = 
        // create the text that will appear on the property 
        match this with 
        | Start _ -> "I HAVE PICKED A NUMBER FROM 1 TO 100! SEE IF YOU CAN GUESS IT!" 
        | Guess(last,targ) -> 
          if last > targ then "WRONG!! MY NUMBER IS LESS THAN THAT! GUESS AGAIN FOOL!" 
          else "WRONG!! MY NUMBER IS MORE THAN THAT! GUESS AGAIN FOOL!" 
        | Success -> "YOU WIN!!" 
      member this.DisplayOptions = 
        match this with 
        | Start _ 
        | Guess(_,_) -> 
          // in all cases except for a win, show 1 - 100 properties 
          [for x in 1..100 -> (x.ToString(),box x)] 
        | Success -> [] // game over

type ExampleGame() = 
  interface IInteractiveServer with 
    member this.NewState = // create the inital state 
      Start (Utils.rnd.Next(1,101)) :> IInteractiveState 
    member this.ProcessResponse(state,choice) = 
      let newGuess = unbox<int> choice 
      match state :?> ExampleState with 
      | Start target 
      | Guess(_,target) when target = newGuess -> Success :> IInteractiveState 
      | Success -> failwith "this case is not possible" 
      | Start target 
      | Guess(_,target) -> Guess(newGuess,target) :> IInteractiveState
```

<p></p>
<p>That's it! No ProvidedTypes or any other craziness in sight. Now if I fire up the type provider, I can play this smashing game as follows, it seems the computer picked exactly 50 on this first go!</p>
<p><a href="http://pinksquirrellabs.com/image.axd?picture=image_11.png"><img style="display: inline; border: 0px;" title="image" src="http://pinksquirrellabs.com/image.axd?picture=image_thumb_11.png" alt="image" width="986" height="141" border="0" /></a></p>
<p>Watch this space for more exciting, much more complex games in the future! In the meantime, please have a go and write your own games, and submit me pull requests!</p>