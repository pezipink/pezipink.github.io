    Title: Choose Your Own Adventure Type Provider
    Date: 2013-07-29T01:21:00
    Tags: F#, type providers
<!-- more -->

<p>That's right.. the type provider everyone has been waiting for, the choose your own adventure type provider!</p>
<p>This is completely pointless and silly, something I just wrote this afternoon. I had various discussions with <a href="http://trelford.com/blog/">Phil Trelford</a> about this and finally decided to do it.</p>
<p>Unfortunately none of the real CYOA books are out of copyright and there appears to be very little in the way of other free ones that I can find. I did however find this one<a href="https://www.smashwords.com/books/view/108782">rather silly story on smashwords</a> which is free, it's not quite normal CYOA fare and you can't lose, but it serves to illustrate the provider.</p>
<p><img src="/image.axd?picture=2013%2f7%2fcyoa1.png" alt="" /></p>
<p><img src="/image.axd?picture=2013%2f7%2fcyoa2.png" alt="" /></p>
<p>The provider reads a data file which contains the contents of each page on a line, the index of the page and any choices the page has on it along with the index that each choice points to. This then creates a type system with properties that you can navigate via intellisense. </p>
```fsharp
#r @"F:\dropbox\CYOAProvider\CYOAProvider\bin\Debug\CYOAProvider.dll "
type adventure = CYOAProvider.CYOAProvider&lt; @"F:\dropbox\CYOAProvider\data.dat"&gt;

let a = adventure()

a.Intro  // start your adventure here!
```

<p>You can find this ridiculous type provider on GitHub <a href="https://github.com/pezipink/CYOA">here</a>.</p>
<p>To create the data file I wrote an amazing C# GUI, if you want to create a file yourself and would like the program to work with then mail me :)</p>