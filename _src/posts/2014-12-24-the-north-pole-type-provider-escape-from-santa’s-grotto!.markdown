    Title: The North Pole Type Provider: Escape from Santa’s Grotto!
    Date: 2014-12-24T02:41:00
    Tags: fsharp, type providers

<p><em>This post is part of the <a href="https://sergeytihon.wordpress.com/2014/11/24/f-advent-calendar-in-english-2014/">F# advent calendar</a>, which is filled with all sorts of other cool blog posts, be sure to check it out. Thanks to <a href="https://twitter.com/sergey_tihon">Sergey Tihon</a> for organising!</em></p>
<h2>It&rsquo;s Christmas!</h2>
<p>Happy Christmas everyone! I have the honour of the Christmas Day F# advent calendar post 

<https://twitter.com/tomaspetricek/status/536992891949035520>

Thanks Tomas, ha!. I had a whole bunch of different ideas, and typically, I decided to choose the largest, most complicated one. Because of this, there is a <strong>lot </strong>of code written in just a couple of evenings. It is for the most part, badly designed, horribly written, nowhere near finished and should not be used as an example on how to write anything approaching nice F# code! /disclaimer</p>

<!-- more -->

<h2>Some Background</h2>
<p>If you know me at all you will know that I tend to write lots of crazy type providers. This will, of course, be no exception. In fact, even though I wrote this in just a couple of evenings, it&rsquo;s probably the most complex ridiculous TP to date. To achieve this, I have used my <a href="http://pinksquirrellabs.com/post/2014/05/01/BASIC%E2%80%99s-50th-Anniversary-%E2%80%A6-and-more-crazy-F-type-providers!.aspx">Interactive Provider</a> which is basically a type provider abstraction that lets you create crazy type providers without writing any provided types code at all! Rejoice.</p>
<p>The TP is a game, which is based on the legendary circa 1980~ game <em><a href="http://en.wikipedia.org/wiki/Rogue_%28video_game%29">Rogue</a>.</em> I realise that many people will not know about Rogue (although the term <em><a href="http://en.wikipedia.org/wiki/Roguelike">Roguelike </a></em>is used a lot more these days) so I will explain a few bits that characterise a roguelike game</p>
<ul>
<li>There are no graphics. The character is typically thrown into a dungeon which is rendered using ASCII characters only.</li>
<li>They are procedurally generated. Not just the dungeons themselves &ndash; a red potion in one game will be very different to your next.</li>
<li>They are <em>hard</em>. Although there is an end, usually you just see how far you can get before you die</li>
<li>They have <em>permadeath</em>. When you die, that&rsquo;s it &ndash; start over from the beginning</li>
<li>They are typically very complex. Modern roguelikes are <a href="http://www.nethack.org/">epic works of engineering</a> with vast amounts of crazy stuff you can do, cause, and interact with, often in strange and fascinating ways.</li>
</ul>
<h2>Escape from Santa&rsquo;s Grotto!</h2>
<p>So, Christmas Eve is finally over and Santa plus crew have been celebrating a bit too much in the grotto / workshop. One things leads to another, and everyone is smashed on all the sherry picked up from the previous night. Santa wakes up at the bottom of the grotto, hungover, and unfortunately discovers that most the Reindeer and Elves are either still on the sherries or have fallen asleep drunk. Now, everyone knows drunk Reindeer and Elves are incredibly violent, even to Santa. Can you help him navigate his way up through 5 levels of the grotto?</p>
<p><a href="http://www.pinksquirrellabs.com/img/old/image_14.png"><img style="display: inline; border-width: 0px;" title="image" src="../../../../../img/old/image_thumb_14.png" alt="image" width="965" height="730" border="0" /></a></p>
<p>Above shows a typical example of a starting position. <em><strong>Important note! </strong>For this to work you <strong>MUST</strong> use a <strong>fixed-width font</strong> for your editor tooltips! I recommend Consolas 10-12 (anyone would think type providers were not designed to run games!)</em></p>
<p>Here is a list of the possible characters and what they depict in the dungeon</p>
<ul>
<li><strong>@ </strong>This is Santa, the player</li>
<li><strong>= </strong>Horizontal wall</li>
<li>| Vertical wall</li>
<li><strong>#</strong> Corridors that connect rooms</li>
<li><strong>.</strong> Floor</li>
<li><strong>/</strong> Open door</li>
<li><strong>+</strong> Closed door</li>
<li><strong>*</strong> Piles of presents</li>
<li><strong>%</strong> Carrots of various types</li>
<li><strong>!</strong> Mince pies of various types</li>
<li><strong>E</strong> Elf</li>
<li><strong>R</strong> Reindeer</li>
<li><strong>&lt;</strong> Stairs leading downwards</li>
<li><strong>&gt;</strong> Stairs leading upwards</li>
</ul>
<p>As the game progresses, you gain experience by killing hostile elves / reindeer and will level up, which increases your maximum hit points, makes you hit harder and more often. Santa will heal over time, but he also gets hungry and must eat. Perhaps you can find some other items that can heal you?</p>
<p>A typical full level map might look something like this</p>
<p><a href="http://www.pinksquirrellabs.com/img/old/image_15.png"><img style="display: inline; border-width: 0px;" title="image" src="../../../../../img/old/image_thumb_15.png" alt="image" width="608" height="626" border="0" /></a></p>
<p>Notice you cannot see any items or monsters that are not in Santa&rsquo;s field of view (FOV). The FOV algorithm is different in corridors to when you are in rooms, and it is largely terrible due to time constraints, but it still works ok :) Each turn, you are presented with a list of properties in intellisense that represent the available actions for you to perform. Some of these require further input in the form of another list of properties. The available actions are as follows.</p>
<ul>
<li>Movement. Represented as N, NW, W, etc. This will move you one tile in that direction, if possible</li>
<li>Pickup. This will take an item at your feet and put it in your inventory. <em>Note: you will see in the status at the bottom of the screen when you are standing on something you can pick up</em></li>
<li>Use Item. This will present you with a list of things in your inventory which you can use. Only trial and error will tell you what a Blue mince pie does, be careful!</li>
<li>Drop Item. You can drop things. You might notice that Reindeer and Elves sometimes eat stuff they stand on. Perhaps this could be useful?</li>
<li>Open / Close. You will be asked to pick a direction, at which point the door (if it exists) will be opened / closed. Note that the monsters cannot open doors!</li>
<li>Wait . Waste a turn doing nothing. Santa gradually heals over time, be he also starves to death if you don&rsquo;t keep him fed!</li>
<li>Climb. This will take you to the next or previous level if you are standing on some stairs.</li>
</ul>
<h2>Strategy</h2>
<ul>
<li>Roguelikes are risk management games. Often a lot of chance is involved. Food should be a top priority as lack of that will kill you off given enough time, no matter what. To this end you should try to make sure that the monsters do not eat any food lying around on the floor by ensuring they don't walk on those tiles.</li>
<li>Similarly, eating mince pies is a game of chance, you could get lucky, or it might go horribly wrong. In any case, once you know what a specific colour pie does, you will know all other pies of that colour do the same thing.</li>
<li>Another way to identify mince pies is to drop them in the path of monsters and see what happens to them if they decide to eat it.</li>
<li>Use doors to your advantage - monsters cannot open them.</li>
<li>Sometimes you will be able to sneak past monsters without waking them - you must balance this with fighting and gaining experience, as the monsters will get tougher in each level.</li>
<li>Monsters cannot follow you up and down stairs.</li>
<li>It is easy to cheat as this is a type provider, you can just undo your steps. Don&rsquo;t do that, you are only cheating yourself!</li>
<li>There are (at time of writing!) 8(!!) different types of mince pie! be sure to experiment, there are some interesting ones such as this!</li>
</ul>
<p><a href="http://www.pinksquirrellabs.com/img/old/image_16.png"><img style="display: inline; border: 0px;" title="image" src="../../../../../img/old/image_thumb_16.png" alt="image" width="802" height="743" border="0" /></a></p>
<p>In order to attack something, simply move in its direction. At the end of your turn, any monsters that are awake will probably try and close in on, or attack you. You will see the results of this at the bottom of the screen in the status messages. If there are more than 2 status messages, you will be presented with a single property named &ldquo;More&rdquo; which will continue to cycle though the messages until they are exhausted. I didn't have time to write anything except very rudimentary AI so they are pretty dumb and you should be able to get them stuck on each other, in corridors, and all sorts. Actually! I have changed my mind. They are all still drunk, that is why their path finding is practically non-existent, and not because I had no time to do it!</p>
<h2>Some final notes!</h2>
<p>Well, I must admit I was furiously coding away just to get this to work at all. There are still some bugs in it, and balance is way <em>way</em> off. Roguelikes usually have an element of luck but this one more so than normal :) I only managed to get a small portion of what I would have liked in, but it still is quite playable (albeit incredibly hard sometimes) and I am fairly sure it is the only roguelike type provider in existence!</p>
<h2>To get it running</h2>
<p>Head on over to my github, clone and build the <a href="https://github.com/pezipink/InteractiveProvider">Interactive Provider</a>. The IP works by scanning assemblies in a given location to find types that implement <em>IInteractiveServer </em>which it then uses to provide the types. Because of this you will need to point the Interactive Provider to the location of the XMAS_2014.dll file, like so</p>
```fsharp
#r @"f:\git\InteractiveProvider\InteractiveProvider\bin\Debug\InteractiveProvider.dll"
type GamesType = PinkSquirrels.Interactive.InteractiveProvider&lt; @"F:\git\InteractiveProvider\XMAS_2014\bin\Debug\"&gt;
let games = GamesType() 
games.``Start The North Pole``.
```

<p></p>
<p>Please have a go, and let me know if you beat by tweeting @pezi_pink and / or #fsharp over on twitter! If you manage to escape, be sure to let us know how many presents you managed to pick up on the way!</p>
<p><strong>HO HO HO!</strong></p>