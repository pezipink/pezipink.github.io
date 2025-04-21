    Title: PezHack–Abstracting flow control with monads
    Date: 2012-07-13T23:10:00
    Tags: .NET, F#, game programming, roguelike
<!-- more -->

<p>It&rsquo;s been forever since I last posted, I worked quite a bit on PezHack and then stopped for a while. I&rsquo;m back to it now. In this post I will describe a technique I used to greatly reduce the amount of code and abstract away some repetitive imperative code.</p>
<h3>The Problem</h3>
<p>PezHack is a turn based game. The screen does not re-render itself all the time like a real-time game, but only when something changes and at the end of a the player&rsquo;s turn. The agent-based approach I used to separate the various sub systems of the game allow me to provide isolation around the graphics processing. The graphics agent is totally responsible for knowing what it needs to draw and will draw it on demand when it receives the relevant message. It does not really know anything else about the game state at all except the visual data of the various tiles that are visible, some player data that allow it to draw the various data about the player, and any menus / other UI that it needs to draw. Other systems can send it messages to inform it of some new visual state it should care about.</p>
<p>Most actions that Pezi can perform require some form of additional input and conditional flow logic. For example, when you press the &lsquo;e&rsquo; key to Eat, first a menu is displayed that shows the stuff in your inventory that is edible, if possible. If not it will display a message and not end the player&rsquo;s turn. Assuming the menu is displayed, the player then presses the key that relates to the item they wish to eat from the menu. If this key is invalid a relevant message is displayed, else the item in question is eaten. What then happens is dependent on the item, it might provide sustenance, or if it&rsquo;s a mushroom it could perform one of various kinds of effects, and then probably end the player&rsquo;s turn</p>
<p>This is a common pattern that appears everywhere, and at each stage the graphics need to be re-drawn to show the various messages, menus and so on. At each stage it might continue or it might not depending the player&rsquo;s input, and the returns differs at each stage (end the player turn, or not, for example). In an imperative language we would almost certainly model this control flow with a bunch of nested if/then/else statements which quickly gets ugly. Because I am using the agent based approach for the graphics, I would also need to post a request to the graphics agent each and every time I needed the changes to show on the screen, so suddenly the actions become a list of imperative statements peppered with common code to keep the screen updated.</p>
<h3></h3>
<h3>The Solution</h3>
<p>This can be drastically improved with the use of a fairly simple monad. The Maybe monad allows you to remove flow control elements such as nested if / then / else statements, so my monad is based around the Maybe monad with some extra stuff built in to help handle graphics and input. It have called it the Action monad and it works as follows.</p>
<ul>
<li>You supply it two functions and a tuple. <em>Before,<strong> </strong>InputPred </em>and <em>Fail</em></li>
<li><em>Before</em> is of type <em>(unit-&gt;&rsquo;e) </em>and it is immediately executed with its result bound to a value</li>
<li>A cycle is then entered that displays any messages in the queue and draws the screen. If there are more than three messages, it only shows three and prompts the player to press &lt;space&gt; to show more.</li>
<li>Next, <em>InputPred </em>of type <em>(&lsquo;e-&gt;&rsquo;f option)</em> is applied the the result of <em>Before</em></li>
<li>If this results in <em>Some &lsquo;f</em> then the monad binds successfully and will continue with the next expressions, passing along the <em>&lsquo;f</em> result.</li>
<li>Otherwise, it looks at the tuple <em>Fail</em> of type <em>(string option * &lsquo;g).</em> If a string is supplied it is passed to the graphics engine and the message cycle is entered until all messages are processed, and it finally returns <em>&lsquo;g</em> (in the future i might just make this a function, but at the moment all fail cases only need to show some message to the user rather than perform some other action)</li>
</ul>
<p>As you can see it is quite generic, and it turns out this monad is quite useful in a variety of areas of the game, not just for actions. I ended up replacing a large part of the core game loop and significantly simplifying it. Here is the code for it first :</p>
<pre class="brush: fsharp; auto-links: true; collapse: false; first-line: 1; gutter: true; html-script: false; light: false; ruler: false; smart-tabs: true; tab-size: 4; toolbar: true;">type ActionBuilder(graphics:Graphics.GraphicsProcessor) =
    member this.Delay(f) = f()
    member this.Bind((before, inputPred, fail), f) =
        let result = before()
        // cycle through any pending messages that might have been created in before() (or before before!)
        let rec pending state =
            let more =
                if state then graphics.ProcessStatusMessages()
                else true
            graphics.Render()
            if more then 
                let c = TCODConsole.waitForKeypress(true)
                if c.KeyCode = TCODKeyCode.Space then pending true
                else pending false
                
        pending true
            
        match inputPred result with
        | Some(x) -&gt; f x
        | None -&gt;
            if  Option.isSome(fst fail) then graphics.QueueStatusMessage ((fst fail).Value) Common.NormalMessageColour
            pending true
            snd fail
    member this.Return(x) = x
```

<p>Now let&rsquo;s see how this is used. First I will show the simplest action, which is quit. When the quit key is pressed, a message appears asking if they really want to quit, and if they then press &lsquo;y&rsquo; then a QuitGame response is issued.</p>
<pre class="brush: fsharp; auto-links: true; collapse: false; first-line: 1; gutter: true; html-script: false; light: false; ruler: false; smart-tabs: true; tab-size: 4; toolbar: true;">let QuitAction (action:ActionBuilder.ActionBuilder) p = action {
        let! _ = ((fun () -&gt; p.g.QueueStatusMessage "Are you sue you wish to quit? y/n" Common.NormalMessageColour),
                  (fun _ -&gt; let c = libtcod.TCODConsole.waitForKeypress(true)
                            if c.Character = 'y' then Some(true) else None),(None,End))
        return QuitGame}
```

<p>The <em>Before</em> function displays some text asking if the user really wants to quit the game. The next function then waits for a key press, and if it;s a &lsquo;y' character then Some is returned (with <em>true, </em>just because it needs to return something, even though we don&rsquo;t care about it). If they press anything else, then <em>None</em> is returned, which means the last parameter <em>(None,End)</em> is acted upon, which means it prints no text and returns the <em>End </em>message. This stops the action message at that point and <em>End</em> does not end the player&rsquo;s turn so they are free to do something else before the monsters move. Assuming they press &lsquo;y&rsquo;, the rest of the function executes and returns the <em>QuitGame</em> message which eventually results in the game ending.</p>
<p>Now I will return to the Eat action explained above as its significantly more complex:</p>
<pre class="brush: fsharp; auto-links: true; collapse: false; first-line: 1; gutter: true; html-script: false; light: false; ruler: false; smart-tabs: true; tab-size: 4; toolbar: true;">let EatAction (action:ActionBuilder.ActionBuilder) p = action {                
        let! items = ((fun () -&gt; p.d.Inventory.FilterType (function ItemData.Comestible(_) | ItemData.Mushroom(_) -&gt; true | _ -&gt; false)),
                      (fun comestible -&gt; if comestible.Count &gt; 0 then Some(comestible) else None),(Some "You have nothing to eat", End))        
        let! id = ((fun () -&gt; p.g.DisplayMenu "Eat what?" (ItemData.Inventory.ToMenu items) ""),
                   (fun () -&gt; let c = TC.waitForKeypress(true)
                              items |&gt; Map.tryFindKey( fun k v -&gt; v.Letter = c.Character)), (Some "The things squirrels will try and eat..", End))
        match items.[id].Type with
        | ItemData.Mushroom(data) -&gt;             
            p.w.AddOrUpdateEntity &lt;| (p.k,World.UpdatePlayerData p.p {p.d with Inventory = p.d.Inventory.RemoveItem id }) 
            Items.MushroomAction data &lt;| (p.e,p.w,p.g,p.k)
            p.w.IdentifyMushroom data
            return EndPlayerTurn 1
        | _ -&gt; failwith ""; return End }
```

<p>The monad is invoked, with the <em>Before</em> function which filters the players inventory to stuff that is edible. The results of this are then passed into the input predicate function (the wonders of type inference make this just work with no type annotations) and checks if the filtered items contain any data, if they don&rsquo;t it returns <em>None</em> and then finally the message is displayed indicating the player has nothing to eat, and execution halts there returning <em>End </em>(allowing the player to do something else this turn). Assuming there were items, they are now bound to <em>items</em>. Another action monad is then invoked that displays a menu containing the filtered items in the <em>Before</em> function. The input pred then takes player input, if it doesn&rsquo;t match a letter assigned to the item in the menu it prints a message and returns <em>End.</em> otherwise, <em>id</em> is bound to the id that the player selected. Finally, the item has some action invoked on it &ndash; in this case only mushrooms are implemented, and it removes the mushroom from the players inventory (sending commands to the World agent telling it to update the player data), invokes the mushroom&rsquo;s specific action, issues another message to tell the World agent that this type of mushroom has now been identified, and finally returns a message that says the player&rsquo;s turn ends for 1 turn.</p>
<p>Pretty cool! The code above is only interested in the higher level stuff that is going on and doesn&rsquo;t need to care about display and flow control. Data from the first function can be passed to the second function, and early exit of the function is easily possible. The monad significantly reduced the actions code from almost 1000 lines to less than 350, and that includes Eat, Pickup, Drop, Move, Attack, Throw, Descend Level, Quit, Open, Close, Inventory, Wait, plus functions to merge items that have been dropped or thrown with existing stackable items on the floor where they land, selection menus and &ldquo;modal&rdquo; choice menus, plus various other helper functions.</p>
<p>Some actions such as Throw are really quite complex, you have to pick an item to throw, choose a direction to throw it, then show it being &ldquo;animated&rdquo; as it moves along the screen, and then finally (maybe) hit something, and either drop to the floor or attack an enemy which may result in other things happening &ndash; now I can just look at the code and see what it&rsquo;s doing without having to dig about in a lot of essentially redundant nested code. Actions can also transfer execution to and from other actions.</p>
<p>Functional programming for the win <img class="wlEmoticon wlEmoticon-smile" style="border-style: none;" src="http://www.pinksquirrellabs.com/image.axd?picture=wlEmoticon-smile_1.png" alt="Smile" /></p>