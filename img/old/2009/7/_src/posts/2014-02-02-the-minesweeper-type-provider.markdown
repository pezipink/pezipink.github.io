    Title: The MineSweeper Type Provider
    Date: 2014-02-02T05:06:00
    Tags: fsharp, type providers
<!-- more -->

<p>Being able to play Mine Sweeper inside your IDE via intellisense, that is what you have always wanted right? Well, I&rsquo;m always willing to lend a hand! With this fantastic new type provider you can pretend you are working when really you are avoiding mines. To get started simply clone and build the provider from <a href="https://github.com/pezipink/MinesweeperProvider">here</a>. Reference your new shiny type provider library from a script file and create a type alias and then an instance of it like so:</p>
<p><a href="http://pinksquirrellabs.com/image.axd?picture=image_1.png"><img style="display: inline; border: 0px;" title="image" src="http://pinksquirrellabs.com/image.axd?picture=image_thumb_1.png" alt="image" width="663" height="94" border="0" /></a></p>
<p>The three parameters you can pass determine the grid width, height and amount of mines.</p>
<p>Navigate to the &ldquo;Start&rdquo; property of your instance to begin the game. Intellisense will show a representation of the grid state on the top property marked &ldquo;# Mine Field&rdquo; and an additional property for each available move in the format of X:Y</p>
<p><a href="http://pinksquirrellabs.com/image.axd?picture=image_2.png"><img style="display: inline; border: 0px;" title="image" src="http://pinksquirrellabs.com/image.axd?picture=image_thumb_2.png" alt="image" width="797" height="237" border="0" /></a></p>
<p>To play the game simply navigate to the property that represents the tile you would like to reveal.</p>
<p><a href="http://pinksquirrellabs.com/image.axd?picture=image_3.png"><img style="display: inline; border: 0px;" title="image" src="http://pinksquirrellabs.com/image.axd?picture=image_thumb_3.png" alt="image" width="935" height="300" border="0" /></a></p>
<p>Unfortunately once again it seems that Microsoft simply didn&rsquo;t have this kind of thing in mind when they were designing intellisense, and it isn&rsquo;t very happy about making the text line up nicely. I have tried my best for it to not get too difficult.</p>
<p>Disclaimer: Development managers, I am not responsible for any loss of productivity from your staff that this type provider may cause.</p>
<p>EDIT: Props to <a href="https://twitter.com/ptrelford">Phil Trelford</a> for the original minesweeper code from <a href="http://fssnip.net/cc">fssnip</a>, which is largely intact here with a few modifications.</p>