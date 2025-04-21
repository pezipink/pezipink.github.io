    Title: The amazing Squirrelify type provider
    Date: 2014-02-01T01:26:00
    Tags: F#, squirrels, type providers
<!-- more -->

<p>Introducing my latest top-o-the-line type provider that everyone wants and needs. The Squirrelify provider! This very useful type provider will create an INFINITE type system and show you random pictures of ASCII art in intellisense.</p>
<p>It turns out that intellisense was not really designed for this and it struggles with various formatting and layout, but the provider tries the best it can. It also doesn't have many images as I couldn't find a webservice for them and had to do it manually.</p>
<p>"Wow Ross!" I hear you cry. "How can we get started with this??"</p>
<p>It's easy! simply go and build the source from <a href="https://github.com/pezipink/SquirrelifyProvider">here</a>, then reference the library it produces in a script file as shown in the picture below. Once you have done that, you will need to alias a type and call create() on it. The resulting type will have an infinite series of properties named``Squirrelify!``which will display said ASCII art.</p>
<p>Note that this is the LITE version of the type provider. They PRO pay-for version includes a static parameter "keyword" which is used to search google images, convert said images to ASCII and display them. </p>
<p>edit - this is not just squirrels. You can expect other delights such as unicorns, snowmen, and various other things you will not be able to identify</p>
<p><img src="/image.axd?picture=2014%2f1%2fSquirrelify!.png" alt="" /></p>