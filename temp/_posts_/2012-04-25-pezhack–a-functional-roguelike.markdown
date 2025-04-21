    Title: PezHack–A Functional Roguelike
    Date: 2012-04-25T08:37:00
    Tags: F#, game programming, roguelike
<!-- more -->

<h2>Introduction</h2>
<p>In my quest to learn the functional paradigm, one thing I have struggled with is game development. Assuming I mostly stick to the functional style of having little to no mutable state, how do you go about writing games? Games are pretty much ALL mutable state. Obviously, in a multi-paradigm language like F# you can have mutable state - and if used judiciously this can be very effective (not to mention offer some significant performance improvements). The blend of imperative and functional styles can indeed work well, I wrote a few small games with XNA and F# using this approach. However, I am still more interested for educational value to stick with a more pure functional approach. Along they way I have wrote a few small games such as a functional console based Tetris in about 300 lines, and a two player console based PONG clone (using the libtcod library as I will introduce in a bit) that uses the Windows Kinect as the input (was cool!) In these programs I would tend to have the game state formed with an F# record that is copied/modified on each game cycle, passing the new state back into the loop. This works well and both of these games used no mutable state at all. This approach soon falls down with something more ambitious though, you can't realistically propagate the entire game state through one loop.</p>
<h2>The Roguelike</h2>
<p>I decided to create something a lot more complex whilst attempting to stick with the functional guns. If you don't know what a roguelike is you can read all about them <a href="http://en.wikipedia.org/wiki/Roguelike" target="_blank">here</a> and a great set of development resources with links to many on-going roguelike efforts <a href="http://roguebasin.roguelikedevelopment.org/index.php/Main_Page" target="_blank">here</a>. I used to play these games a lot. Stemmed from D&amp;D back in the days where the only computers were the terminal sort in colleges in universities (before I was alive!), a roguelike traditionally uses just ASCII characters as its graphics, leaving the rest to the player's imagination. If you haven't tried this before I highly recommend you try one, Nethack is probably the biggest most complicated one out there, but you can also play the original Rouge (where the <em>roguelike</em> genre name comes from) online <a href="http://www.hexatron.com/rogue/" target="_blank">here</a>. A few things that make a roguelike;</p>
<ul>
<li>Random procedurally generated dungeons - every time you play there is a new dungeon</li>
<li>Randomness in item drops - until things have been identified, you don't know what they are. The "clear potion" might be a healing potion in one game and a potion that causes severe hallucinations in the next</li>
<li>Peramadeath and hard difficulty. These games are <em><strong>hard.</strong></em> Expect to die lots, and when you die you have to start again. Often the objective isn't to finish the game, but just see how long you can survive</li>
<li>Roguelikes are usually turn-based affairs - although there is some variation with this</li>
<li>Complexity - these games are amazingly complex and deep. The developer(s) don't have to worry about impressive graphics engines, so they can focus a lot more on interesting game mechanics. You will be amazed at some of the stuff you can do in a game like Nethack.</li>
</ul>
<p>Here's a picture from Nethack to illustrate how a typical roguelike looks :</p>
<p><a href="http://www.pinksquirrellabs.com/image.axd?picture=Nethack-kernigh-22oct2005-25_thumb2.png"><img style="background-image: none; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; padding-top: 0px; border: 0px;" title="Nethack-kernigh-22oct2005-25_thumb2" src="http://www.pinksquirrellabs.com/image.axd?picture=Nethack-kernigh-22oct2005-25_thumb2_thumb.png" alt="Nethack-kernigh-22oct2005-25_thumb2" width="609" height="447" border="0" /></a></p>
<h2>&iquest; What Am I Trying To Achieve ?</h2>
<p>First and foremost this is another learning exercise. Programming games in a functional style is hard. There is Functional Reactive Programming which bases a lot of things on time, I have yet to try this. My approach will be to isolate the various subsystems and allows them to communicate only using Erlang style message passing. The F# mailbox processor is an awesome tool in the box to achieve this, and it also gives a way for each sub system to cycle and keep its own state whilst preventing anything else even seeing it unless expressed through messages.</p>
<p>As far as I can see there are virtually no RL's completed or in development using functional languages (except <em>LambdaHack, </em>a RL engine written in Haskell), which is surprising because there are literally hundreds and hundreds out there. Some of the things I am hoping to achieve with my approach :</p>
<ul>
<li>I have written a lot of game code of all kinds, from text based things, to 2D and 3D, on Amigas, Phones and PCs. I worked on a MMO Ultima Online server for 5 years. 90% of bugs in all games come from complex shared mutable state and huge object hierarchies. I am aiming to almost entirely remove this class of bug.</li>
<li>Performance is not a concern, I am not worrying about memory footprints or speed. The game is turn based anyway. However, the systems are designed in a way where I could switch to using some faster imperative data structures in areas without compromising the safety provided by the message passing style.</li>
<li>The ability to use discriminated unions, pattern matching, active patterns, higher order functions and other functional features to (hopefully) greatly ease the work required to add new features, items, or change existing mechanics.</li>
<li>Produce a pretty complex RL with relatively little code. A game like Nethack is 145,000 lines of C (!!). Whilst this has been in development for about 25 years from lots of people, the staggering amount of code (given what the game is) can soon become all sorts of problems when you try to change or add anything.</li>
</ul>
<p>To attempt this I will be using the very awesome <a href="http://doryen.eptalys.net/libtcod/features/" target="_blank">libtcod</a><em></em> which is a free library that provides a SDL based console capable of true colour. It has some nifty RL features built in such as Field of View calculators, Map generators, path finding algorithms and so on - I probably won't be using these bits as I would prefer to write my own, but may well take advantage of some to get the thing off the ground. I use this console for all my little games, simulations and demos these days - very cool!</p>
<h2>Initial Agent based systems</h2>
<p>Before anything interesting can happen I am going to need a way of rendering basic graphics. For this I will use a dedicated Agent that has isolated state from the rest of the system (as explained earlier.) In order to display graphics on the console I will need to know the following bits of information that represent how something is displayed:</p>
<pre class="brush: fsharp; auto-links: true; collapse: false; first-line: 1; gutter: true; html-script: false; light: false; ruler: false; smart-tabs: true; tab-size: 4; toolbar: true;">type Point = { X:int; Y:int }
type VisualData = {Text:string; ForeColour:TCODColor; BackColour:TCODColor Option}
type EntityDisplayData = { Position:Point; Visual:VisualData; ZOrder:int }
```

<p>Point is self explanatory, is used everywhere and is not specific to graphics. The VisualData record determines what should be displayed; Text is the string which should be displayed &ndash; this mostly always going to be just a single character but may occasionally be a string. The two colours are self explanatory, except the back colour is an Option type &ndash; this is so you don&rsquo;t have to specify a back colour and it will be rendered with whatever the current backcolour at that cell is. I don&rsquo;t think I will need this functionality for the forecolour as well but it will be easy to add later if required. Finally the EntityDisplayData record is what the graphics processor will care about &ndash; this defines everything it needs to know about how to render something, with it having no idea what that something is. These three records are defined in the Common module where they can be accessed by the various other subsystems. The graphics processor itself is formed of a MailboxProcessor that takes a GraphicsMessage type, and internally cycles a state.</p>
<pre class="brush: fsharp; auto-links: true; collapse: false; first-line: 1; gutter: true; html-script: false; light: false; ruler: false; smart-tabs: true; tab-size: 4; toolbar: true;">type private GraphicsMessages =
    | Render                of UnitReply
    | ClearWorld            of UnitReply
    | UpdateWorld           of EntityDisplayData list * UnitReply      
    ....
   
type private GraphicsState =
    { worldBuffer          : TC
      primaryBuffer        : TC                  
      entityDisplayData    : Map&lt;Guid,EntityDisplayData&gt;
      ... }

type GraphicsProcessor(width,height,fontFile) =
   
    do if System.IO.File.Exists(fontFile) = false then failwith &lt;| sprintf "Could not find font file at location %s" fontFile
    do TCODConsole.setCustomFont(fontFile,int TCODFontFlags.Grayscale ||| int TCODFontFlags.LayoutAsciiInRow)
    do TCODConsole.initRoot(width,height,"Pezi - Pink Squirrel from the Abyss", false, TCODRendererType.SDL)

    let agent = Agent&lt;GraphicsMessages&gt;.Start( fun inbox -&gt;
        let rec loop state = 
            async { let! msg = inbox.Receive()
                        match msg with
                        | Render(reply) -&gt;  
			  reply.Reply()
			  return! loop state
                    ...             }
        loop 
             { worldBuffer           = new TC(width,height);
               primaryBuffer         = new TC(width,height);
               entityDisplayData     = Map.empty})
    
    member x.Render()                          =  agent.PostAndReply Render
    member x.UpdateWorld displayData           =  agent.PostAndReply(fun reply -&gt; UpdateWorld(displayData,reply))
    member x.ClearWorld()                      =  agent.PostAndReply ClearWorld
```

<p>(syntax highlighter messed up some of the indentation there..)</p>
<p>The messages and state are both private, consumers can post a message via members on the type that provide Post-And-Reply only functionality. That is, all the calls are effectively synchronous in as much as all calls wait for a reply from the agent before handing execution back to the calling thread. UnitReply is simply an alias for AsyncReplyChannel&lt;unit&gt;, Agent is MailboxProcessor&lt;'T&gt; and TC is the TCODConsole, all defined in the common module. This is the general approach that will be used for all the subsystems allowing me to maintain a high degree of isolation and separating concerns as much as possible. The only place state can ever be modified and selected elements of immutable state can be accessed is within or through these agent loops.</p>
<p>Obviously, I have not shown any of the actual implementation here and the graphics agent is substantially more fleshed out, currently having 10 messages and a state about double the size of the one shown here. Here is a pic of what it currently looks like (still very young!)</p>
<p><a href="http://www.pinksquirrellabs.com/image.axd?picture=image_thumb3.png"><img style="background-image: none; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; padding-top: 0px; border: 0px;" title="image_thumb3" src="http://www.pinksquirrellabs.com/image.axd?picture=image_thumb3_thumb.png" alt="image_thumb3" width="785" height="421" border="0" /></a></p>
<p>The total thing is about 1500 lines of F# at the moment, and it is somewhat operational with the main systems being in. These are comprised of :</p>
<ul>
<li><strong>Graphics agent</strong> &ndash; handles maintaining all the state to do with drawing stuff on the screen, including the player stats and bits n the left, the messages at the bottom, any menus, the title screen, and so on.</li>
<li><strong>World agent</strong> &ndash; handles the actual world state itself, including accessing, adding, removing and updating map tiles, monsters, items, the player itself, field-of-view calculations, the current turn and so forth</li>
<li><strong>Player action agent</strong> &ndash; handles the input from the player and does stuff. This was one of the trickiest parts because many actions are split across input cycles, and some actions might fail but default to another action depending on some outcome. As an example of the former, if a player wants to throw something the game will ask what they want to throw and then which direction in which to throw it &ndash; the agent must remember where it is in the cycle and be able to exit the cycle at any time (the player might press an invalid key or want to cancel). Depending on these choices the player&rsquo;s turn might end, possibly for more than one turn, or not. Or the game might need to pass a few cycles without progressing the game state whilst it &ldquo;animates&rdquo; a flying projectile. In addition to this many of these sub-actions such as choosing something from the inventory or accepting a direction are shared by many different actions. (fun fun!). As an example of the latter, if a player tried to move into a door or a monster the state might change to ask if they want to open the door (switching into the open action) or automatically attack the monster. I plan to write a post about this one at a later date as it is quite an interesting problem to address. I wanted to address all of this in a general re-usable manner and not fully hard-code each action which would have been next to impossible without totally destroying my nice agent based approach.</li>
<li><strong>Monster action agent</strong> &ndash; similar to the player action agent except this obviously doesn&rsquo;t require input, but might still need to perform &ldquo;animation&rdquo; and so on. The monster AI is executed here which will be fairly general so monsters can share common bits of AI and / or provide their own special bits.</li>
<li><strong>Event processor agent</strong> &ndash; this agent holds a list of events that are going to happen on a pre-determined turn, and on each turn anything up for action executes a function that it has been passed. This is used for all sorts of thing such as health re-generation, poison, spell effects, ominous messages, hunger, etc.</li>
</ul>
<p>In addition to this lot the basic concepts of combat are in along with the beginnings of an item and inventory system &ndash; you can currently pick stuff up and use some of it, throw it around (and kill stuff in the process). Then there is the dungeon generator which is currently a fairly crude affair that I will focus on a lot more later.</p>
<p>I have already scrapped two approaches, this is my third go and its not coming on too badly so far, the agent system is very manageable and difficult to accidentally let it get out of control. The whole thing is likely still miles from being decent but it&rsquo;s been a good learning experience so far.</p>
<p>Hopefully I will continue these posts, detailing some of the systems and sharing progress, but then I always post part 1 of articles and never get round to writing another part before I get distracted by something else. Comments welcome&hellip;</p>