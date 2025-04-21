    Title: on drey-vm and a remote debugger part 1
    Date: 2017-10-12T09:27:13
    Tags: DRAFT, fsharp, drey, dlang, scurry, racket
 
My current project involves a networked multiplayer game server written in D, that communicates with clients over ZeroMQ and interprets a bytecode compiled from my language Scurry, that is written using Racket. This series of posts will describe the path to building a a remote debugger for it in F#, and some of the internal design of the virtual machine itself.


## THE STONE AGE

Back in the stone age, there was no way at all of debugging at runtime, short of writing stuff to the console from inside the virtual machime.  Combine this with (at the time) lacking any kind of semanttic analysis of the language, and you were oft left with infuriating runtime bugs such as incorrect variable names and other silly things.

Scurry and the programs written in it were growing in complexity quickly, and even with some vogue semantic analysis in, it was clear a debugger was going to be necessary.  Thus, in the dawn of the bronze age, a debugger was born as a front end to the virtual machine itself,  necessarily written in the very awesome D programming language.  Since I like a UI for my debuggers, I chose a D library imaginatively named dlangui.  The debugger featured the diassembly of the program highlighting the current instruction, the ability to set breakpoints (in software and in the debugger at runtime), stepping, running, various navgation features, a mostly complete view on all the various parts of the machine itself, and a way to emulate responses from clients that the program is waiting upon.

Here's a picture restored from the ancient galleries of the beast in the field.

<pic>

<!-- more -->

## THE AGE OF BRONZE
All was not well in the virtual machine kingdom, however.  The UI framework, whilst getting the job done, was not a very nice experience.  I had to write some custom controls, there were problems when throwing too much data at it, and I never did find or fix the mysterious bug that caused the whole thing to crash sometimes.  Now, I don't want to paint dlangui in a bad light - mostly I just wanted to get something to work, and did not take the time to learn the intricaces of the library properly or put the time into having basically any kind of sensible design for it.  Mixing into this soup the multi-threaded requirement that the ZeroMQ messaging layer demanded, resulted in a recipe - which although fed the masses - wasn't very tasty.
 
A revolution was stirring.  "Out with the debugger!"  The opcodes cried. "Burn the parasite!" yelled the eval stacks, angry at the tendrils that had spread throughout it.  So, with a heavy heart, I ripped out the debugger, restoring the virtual machine to its former glory.  It was decided that what was really needed was a way of debugging *remotely*.   This would completely decouple the two things, and since I already have a communictions channel to the server with ZeroMQ, half the story was already in place.  And, I'd be able to debug the server OVER THE INTERNET!

## IRON AGE PRECURSOR

I decided to choose F# and WinForms for the debugger client, for the following reasons:

* I haven't wrote anything substantial in F# for ages
* WinForms is battle-tested, no-frills, hardened, I know it well and it works great programmatically with an interactive F# session during development
* Rabbit stuff
* MailboxProcessors and the fszmq library give a relatively painless multihreaded abstraction to deal with the messaging concurrency
* Squirrels
* With some explict design, the mailboxes can also be used in a way whereby modifing the mssage handling can happen during an interactive F# session, greatly improving the development experience and cycle.


## Virtual Machine Essentials

To write a debugger, one must first understand how the machine it is debugging operates.  Drey is a high-level virtual machine, similar to the CLR or the JVM. It interprets instructions loaded from a bytecode file that has been compiled, typically by Scurry. At the time of writing, the machine has about 110 different opcodes, which are a mix of low level boring stuff and higher level cool things.  Here are some of the features that Drey supports, some of which are domain-specific in supporting networked board game (or similar) emulation;

* Lambda functions as first class values
* Closures
* Strings
* Integers 
* Booleans
* Resizeable arrays 
* string => object dictionaries
* Locations and LocationRefs
* Flowrouties TM
* Random number generator
* Chat system
* Data Hiding
* Mutable state everywhere
* Global mutable state!


### Design goals

The design primarily supports what I like to call *Funperative* programming, which is an odd mixture of functional and imperative programming using mutable state. Scurry is dymically typed, but you could of course write a statically typed language for it.  The core building blocks and abstraction mechanisms of the machine are functions, closures and dictionaries (ala JavaScript and Lua). It is completely possible to build an entire OO system with virtual dispatch and inheritance using what is provided, if desired, but typically objects in the OO sense are not really required.

An important design decision is that, largely speaking *performance does not matter*.  The types of games this is targetted at writing typically do not need mass amounts of CPU power, so even the most intense parts of them execute pretty much instantly without having to worry about optimising code too much (of course, it is not completely silly!), and allows some other tradeoffs to ease the development, debugging and usability of the machine.

Central to Drey is the concept that *networking should be transparent* that is, the programmer should not be bogged down with the complexities of how to send and recieve messages from clients, what happens when they disconnect/reconnect, making sure they are not sending invalid packets or trying to cheat, and do not have any data sent to them that they should not be able to see.  Instead, the programmer should be able to focus purely on the game logic and mechanics whilst expressing things like data visibitly and client communication as simply as possible.  To this end, the virtual machine supports *Flowrouties* (more on this in a bit) and first class data visiblity concepts.


### Execution specifics

Drey uses an evaluation stack for performing useful work.  For example, to add two numbers together and store the results in variable *sum*, the sequence of opcodes would look like this

```drey
ldval 10 ; push 10 onto the stack
ldval 20 ; push 20 onto the stack
add      ; pop 2 values form the stack, add them together and push the result on the stack
stvar 0  ; pop value from stack and store it in the current scope.as "sum"
```

the opcodes *ldval* and *stvar* require an operand that is written into the bytecode file.  For ldval, it is a 32bit integer representing the constant number in question;  the operand to stvar is an index into a string table that appears at the start of the bytecode file - at compile time, the assembler collects all the strings in the program and rewrites their usage into this form, thus saving tons of space, espeically since strings are heaviliy used given the dynamic nature of the machine.

A good question to ask at this point would be, "where is the variable 'sum' stored!".  To answer that question, the current "stack frame" has a dictionary of variant objects (stored on D's runtime garbage collected heap) which is indexed by the variable name in question.  This also makes it easier to mentally translate the high level source code into what is happening inside the machine, since the names of things like variables are preserved.  In a sense, this dictionary is a bit like "registers" for the current scope.

### Function application and closures

Whilst Drey of course supports jumping around to arbitary locations in its program, Scurry does not really support the notion of a *procedure call*.  Instead, everything except the main function itself is a *lambda function*.  A lambda function in Scurry only ever takes one argument, maybe does some work and maybe "returns" some value (leaving it on the eval stack), which could well be another function.  A function definition in Scurry that looks like this 

```racket
(def-λ (test-function a b c d)
    (add a b c d))
```

which is actually de-sugared via macros to

```racket
(def test-function 
  (λ (a) 
   (λ (b) 
    (λ (c) 
     (λ (d) 
       (add a b c d))))))
```
 
This is common practice in functional languages ("curried" functions) and allows you to easily apply only some of the arguments, pass the function around a bit and give it the rest of the arguments later. 

At compile time, the assembler collects all the lambdas up and places their actual implementation at the end of the file, then goes an re-writes the function applications to jump to their final locations.  let's look at a complete example with a silly wrapper around the add macro.

```racket
(scurry
    (def-λ (adder input input2)
        (add input input2))
    (dbgl (adder 10 20))
```
        
When compiled/assembled, the bytecode looks like this (replacing string table indexes with the actual strings)
 
```drey
;main program
1    lambda    1E
6    stvar     adder
B    ldvar     adder
10   ldval     10
15   apply
16   ldval     20
1B   apply
1C   dbgl
1D   ret

;main program ends here, function implementations follow.

;the first lambda simply stores the input value in its scope
;and leaves another lambda on the stack
1E   stvar     input
23   lambda    29
28   ret

;the second lambda is able to access the first's input value,
;perform the processing and leave the result on the stack.
29   stvar     input2
2E   ldvar     input
33   ldvar     input2
38   add
39   ret
```

Here you can see the *lambda* opcode which is assembled with an operand that is the location of the function's executable a code.  It's a 32 bit integer, which means drey is currently limited to a program space of ~4 gb, which should be plenty!  ha!

When Drey encounters *lambda* it creates a special function object holding the return address and pushes it on the stack - it can then be stored and passed around / manipulated like any other object.  You can even add them together (function composition). 

Upon executing an *apply* instruction, Drey will pop 2 values off the stack, expecting one to be a function and the other to be some argument value.  It then pushes the argument back on the stack, and changes the machine's instruction pointer to the function's code location.  At the same time, a new "stack frame" is introduced.  I use the term loosely because it is not quite the same as a typical stack frame.

```d
struct Scope
{
  HeapVariant[string] locals;
  int returnAddress;
  Scope* closureScope;
}
```

*locals* here is the dictionary mentioned earlier for storing variables in.  Notice *closureScope* here is a pointer to another scope.  This will always point at parent scope from which it was created.  This is important because it provides a simple way to implement closures, without having to resort to packaging "captured" variables in some object and pass it around;  instead, when a variable load is requested, the machine will check if it exists in the current scope, if not then it recursively walks up the scopes looking for the variable in question *even if the scope was popped from the general scope stack*.  This also allows the language to implement shadowing of variable names from higher scopes for free.

You  might notice that you can access *all* the variables in scope above you - it is left to the language compiler to enforce access to only things that are semantically in scope via closures and lexical scoping.

# Return values

The functions must accept one argument - and it is not enforced that it should "return" anything.  Languages like F# that have types use a special value *unit* to call a function with "no arguments".  Since Scurry doesn't have any types, it simply provides a default argument (-42) to functions declared with no parameters, transparent to the programmer.  Unfortunately it is not currently possible for the compiler to check that results from functions are not ignored (and left on the stack!) or even worse, attempting to store or use the results of  a function that doesn't return anything.  Both of these scenarios is an almost guranteed crash and burn - for now it is reasonable that the programmer (me) understands what they are doing and try not to make these mistakes. 

Of course, if the value returned from a function is another function, it can be immediately applied.  Here is some Scurry samples that use tables of functions, and a very simple "virtual dispatch" type mechanic.

```racket
(scurry
 (def-obj function-table
   (["square" (λ (add _ _))]
    ["cube" (λ (add _ _ _))]
    ["twice" (λ (f x) (f (f x)))]))

 (dbgl "10 squared :" (function-table.square 10))
 (dbgl "10 cubed :" (function-table.cube 10))
 (dbgl "10 squared squared :"
       (function-table.twice function-table.square 10))

 (def-obj override-functions
   (["square"
     (λ (begin
          (dbgl "inside debug square with " _)
          (add _ _)))]))
  
 (def-λ (virtual-dispatch object default-object function-name arg)
   (if (contains object function-name)
       ;call speciailised method
       ((get-prop object function-name) arg)
       ;call default
       ((get-prop default-object function-name) arg)))   

 (dbgl "dispatch:"
       (virtual-dispatch
        override-functions
        function-table
        "square" 10)))
```

Finally, some examples of closures as a data hiding mechanism, via the classic "let-over-lambda" technique,  and the ability to mutate everything if ya like ;) 

```racket
(scurry
 (def-λ (test-closure n)
   (let ([n2 7]
         [f (λ (x y)
              (begin
                (def ret (add n n2 x y))
                ; mutation inside closure
                (n2 += 1)
                (return ret)))])
     (return f)))

 (def test-closure-f (test-closure 37))
 (assert (eq (test-closure-f 10 20) 74))
 (assert (eq (test-closure-f 10 20) 75)))
```

# Flowrouties

Flowroutines are essentially remote co-routines. They enable a seamless request-response model to obtain some data from a client. The basic *Flow* goes as follows

1. A special *Flow* object is created
2. The object is populated with the availabe choices for the client. A choice consists of a key and a description (and later, probably some extra arbitary data).
3. The object is serialized to json and the data sent over ZeroMQ to a specific client. At this point, the VM suspends execution of the program and waits.
4. The client eventually responds with one of the choice keys.  Since Drey knows it is waiting for the client to respond,  and remembers the available choices, it is able to verify a valid choice has been selected.
5. The choice key is pushed onto the stack, and execution of the program is resumed.

The creation of the flow object and adding choices into it is quite imperative in nature - Scurry gives you various declartive and functional ways to create a bunch of choices for a client with the minimum amount of ceremony possible.  Since you can write macros, you can also introduce your own flow syntax depending on your game's needs.

To illustrate this properly, here is a function that has two remote players perform a game of rock, paper, scissors.  It returns a string indicating which player won.

```racket
(def-λ (rock-paper-scissors)
   (let
     ([p1 (nth players 0)]
      [p2 (nth players 1)]
      [f (λ (clientid)
           (flow clientid "Choose a move!"
                 (["rock" "Play the rock!"]
                  ["paper" "Play the paper!"]
                  ["scissors" "Play the scissors!"])))]
      [r1 (f p1.clientid)]
      [r2 (f p2.clientid)])
     (cond
       [(r1 = r2) "you tie!"]
       [(or
         (and (r1 = "rock")     (r2 = "scissors"))
         (and (r1 = "scissors") (r2 = "paper"))
         (and (r1 = "paper")    (r2 = "rock")))
         "player 1 wins!"]
       [else "player 2 wins!"])))

```

Notice in this example there is no noise around communications at all.  The flow function is reused for both the players and the results are compared to each other directly.  Scurry also has matching forms to reduce this further, and other flow forms that can automatically execute a function that is paired with a choice when it is selected.
 
But that's not all!  Often in these games, the player will make a series of choices, based on each other. When Drey executes the *suspend* instruction, before it does anything else, it makes a copy of the entire machine state.  If it sees this has happened at least once before, it automatically includes a special choice to the client called *undo*.  If the user selects this, the machine reverts back to the previous cloned state.  Effectively, this gives you the completely ability to unwind multiple levels of flowrouties for free!  There is a special commn *flow-end* that immediatley culls any cloned machines.  Therefore, it is up to the programmer to decide when to disallow undoing of previous messages - typically this will be when some object / global state has changed or a choice reveals previously unkown information to the client.


## Objects, Locations and Data Visiblity.

Every GameObject (the string => object dictionary) has an id and a data visiblity tag.

Locations are areas of the game and can be nested within and connected to one another in various ways.  Locations represent areas where the clients can see objects.

When a GameObject is moved into a Location, the object is added to the "visible universe" if it isn't already, and then all the clients are sent delta update messages about the new object and its contents.  The data visiblity tags are processed recursively as a hierarchy and clients not eligble to see the data will not get the announce messages.  

The details of the data visiblity  are not fleshed out, but that is the general principle.

##THE IRON AGE

Finally, we are in a position to talk about the remote debugger.  


