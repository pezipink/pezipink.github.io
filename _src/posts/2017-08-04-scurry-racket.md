    Title: Racket Macros : Scurry
    Date: 2017-08-04T14:12:48
    Tags: racket, scurry, drey, compilers, programming languages, macros

This post will be about [Racket macros](http://docs.racket-lang.org/guide/macros.html).  As a delivery mechanism, I will be using bits of my latest project, which is a virtual machine designed for emulating multiplayer networked board games via [ZeroMQ](http://zeromq.org/).  drey-vm executes compiled bytecode files - much like the CLR or JVM - using an instruction set of my design.

The design of the computer is out of the scope of this post (maybe another post if people are interested in it - let me know in the comments!?) suffice to stay it is written in D and is mostly a stack based computer, supporting functional and imperative programming.  It has a string/object dictionary as its primary type,like lua and js.

Much like [asi64](http://pinksquirrellabs.com/blog/2017/05/30/asi64/), I wrote an assembler for the virtual machine in Racket.  On top of that assembler I can now build a language (scurry).  To start with, I am just creating a Racket language that is still a lisp - Racket has the fantasic power of being able to build macros on macros, gradually introducing higher level syntatic forms.  In this way, I don't really need write an actual compiler.  Racket gives me the front end of the compiler for free (being lisp), and the macros-over-assembler method is a very fun and flexibile way to build up a language.

<!-- more -->

# Macros

This post is about macros.  I am still new to them and have learnt a lot, but am many many leagues from being an expert.  Macros can be hard to get your head around and I spent many hours flailing around not knowing how to do stuff I wanted, so hopefully some of things I discovered might help someone else out.

It is recommended you have already read Greg Hendershott's excellent [fear of macros](http://www.greghendershott.com/fear-of-macros) post, which I read many times!.

Let's look at the basics.  The VM has a special object with an id of -1 that represents the game's global state.  In scurry, I would like to write this to retrieve it

```racket
(get-state)
```

This should be rewritten to the op-codes that load the game object of id -1 and leave it on the stack. (this will be used in expressions or bound to some identifier later.)  Eventually, when this program actually executes and assembles, it is expecting a big list of symbols that represent the program. Thus:

```racket
(define-syntax (get-state stx)
  (syntax-parse stx
    [(_)
     #''((ldval -1)
         (getobj))]))
```

stx is the syntax object that repesents the Racket expression that has been passed in, including the "get-state" itself.   We are then using syntax-parse to pattern match on this syntax object - we expect one value between parens which I have wildcarded out as I know it is "get-state" and don't care about it.   This means if you try and use this macro with an argument, you'll get a compile error.

Finally we return a syntax object using the shorthand form #'(  ).  Since what I actually want to return is a list of symbols, we are quoting the response list (otherwise it would try and evaluate (ldval -1) and (getobj) as functions, which don't exist!).

#eval-arg

Now let's tackle the first major problem.  In practially all other macros from here on out, they are going to accept some inputs.  The inputs could be constant strings, numbers, bools, an identifier, or another scurry expression.  To illustrate, let's say we wanted to write this

```racket
(def x 42)
```
 x here is the name of a variable or binding.  In drey-vm terms, it has to translate the usage of an identifier like this to storing or retrieving a named variable currently in scope on the call stack.  The question is, how can we tell the difference between them and generate the correct code?  Let's look at macro that will be used heavily from this point onwards.

```racket
(define-syntax (eval-arg stx)
  (syntax-parse stx
    [(_ ident:id)
     (with-syntax ([name (symbol->string (syntax-e #'ident))])
     ; if this is an identifier then it will be a string table lookup
     ; to a variable       
     #''((ldvar name)))]
    [(_ expr:str)     
     #''((ldvals expr))]
    [(_ expr:integer)
     #''((ldval expr))]
    [(_ expr:boolean)
     #''((ldvalb expr))]
    [(_ expr)
     ;otherwise let it pass through
     #'expr ]))
```

Several new and interesting things are happening here.  Firstly, we are making use of [Racket's syntax classes](https://docs.racket-lang.org/syntax/Library_Syntax_Classes_and_Literal_Sets.html?q=syntax%20class).  These are the annoataions you see in the pattern match, :id :str :integer and :boolean.  These are standard syntax classes supplied by the Racket language to help match on common things.

The first case will only match if the passed argument is an identifier.   Then, we introduce a new piece of syntax *name*, that is produced by unwrapping the syntax object *ident* and turning it into a string.   We can then use it directly in the returned syntax block,  '((ldvar name)).   If *ident* was the identifier *x*, it would produce the code '((ldvar "x")) which is what the assembler expects.  Pretty cool huh!

The subsequent 3 matches simply produce the correct op code to load the given int, string or bool constant.

The last match catches all other expressions - in this case we do not want to directy output bytecode - we want to continue evaluating whatever it is. All we have to do here is simply return the bit of syntax as it is and let the Racket compiler continue its expansion of it where neccesary.

The with-syntax part takes some getting your head around.  It tends to be used so often that if you need just a single bit of extra syntax like we had in this case, there is a shortcut you can use:

```racket
    [(_ ident:id)
     #:with name (symbol->string (syntax-e #'x))
     #''((ldvar name)))]
```

In fact, since we will need to do this kind of thing all the time (turning an identifier into a name) we can even write out very own syntax class to help out!

```racket
(begin-for-syntax
  (define-syntax-class binding
    (pattern x:id #:with name  (symbol->string (syntax-e #'x)))))
```

Notice how this uses both the :id syntax class and the #:with shorthand to introduce a new piece of syntax.  Now in the original code we can write this to get at it

```racket
    [(_ ident:binding)
     #''((ldvar ident.name)))]
```

Very nice, yes?

#Level up

Now let's see how we can use this previous macro in the context of other macros.   Let's re-visit def.  We wanted to write

```racket
(def x 42)
```

The macro pretty much writes itself

```racket
(define-syntax (def stx)
  (syntax-parse stx             
    [(_ id:binding expr)
    #'`( ;expr should calculate some value on the stack, or load a constant
       ,(eval-arg expr)
       (stvar id.name)))))
```

So, we tell the macro to expect two bits of syntax, the first must match our handy *binding* syntax class from before, and some *expr*.  Now, we don't know what *expr* is - it could be anything.  Thankfully, our equally-handy *eval-arg* macro will sort it out for us.  Notice here we are using a slightly different syntax to return the syntax object, usually we use #''(  ) but here we are using a backtick instead.  This is because we are returning a quoted list, but there's one part of the list we *don't* want quoted, because we want racket to expand it - that is of course the *eval-arg* macro, which you can see is prefixed with , - a shorthand for *unquote*.  If this doesn't make sense, study and play with it until it does, since it is very important and part of the secret sacue that makes writing these so nice.

As an illustrative example, if we had not used the unquote, it would have simply returned a list of symbols ((eval-arg expr) (stvar "x")).  This is not what we want, eval-arg is not an opcode - what we wanted is for it to expand out to  ((ldvals "hello world")(stvar "x")) or similar.

Let's actually see this in action.  First let's write a cool macro that will produce the assembly code for the VM to add two numbers together.  It needs to load the numbers onto the stack and call the add instruction.

```racket
(define-syntax-parser add     
  [(_ left right)
    #'`(,(eval-arg left)
        ,(eval-arg right)
        (add))])
```

Easy! Here we are using the shorthand *define-syntax-parser* so we don't have to keep writing the redundant *define-syntax*, *syntax-parse* stuff. The nested macros make short work of this task and our wonderful add macro will work with any constants *or nested expressions* just as you would expect it to.

```racket
(def x 42)
; this works!  
(def y (add x 58))
; so does this!!
(def z (add y (add z x))
```

What about if we want our add macro to take more than two numbers, in the typical lisp style?  For example, I'd like to write (def z (add y z x)).  Enter the ellipsis!

```racket
(define-syntax-parser add     
  [(_ left right ... )
    #'`(,(eval-arg left)
        ((,(eval-arg right)
         (add)) ...))])
```

The use of the ellipsis here means the argument *right* can represent any amount of arguments.  Then, in the syntax that is returned, we wrap the expression we want to repeat for each instance of *right* in some more parens and place the ... after.  The result of this is the following;  If you call (add 1 2), after its nested macro expansion, you would get

(ldval 1)
(ldval 2)
(add)

And if you call (add 1 2 3 4) you will get

(ldval 1)
(ldval 2)
(add)
(ldval 3)
(add)
(ldval 4)
(add)

Very awesome, yes?   (n.b.  actually, there would be a few more parens in there due to the extra ones we had to put in to get the ellipsis pattern to work, I left them out here for readability - the assembler removes any excess)

#Further adventures in macro land

Let's try something else.  The language is pretty terrible with no conditionals and logical functions in it.   We will try and implement *and*.  The *and* macro should take any amount of expressions, evaluate them, and if any return *false* then we short-circuit out and load the constant value 0 on stack.  If all return true, we load the constant value 1 on the stack.

This needs some new bits to help it along - the assembler and bytecode can only deal in jumps to explict locations in memory, so we will need a way to label instructions so they can be used as jump targets.  The assembler itself already supports this, and the details are out of scope of this post - let's look at the macro

```racket 
(define-syntax-parser and
  [(_ expr ...)
   (with-syntax*
     ([when-false (new-label)]
      [end (new-label)]
      [(cases ...)
       #'`((,(eval-arg expr)
            (ldvalb 0)
            (beq when-false))...)])
     #'`(,(cases ...)
         (ldvalb 1)
         (branch end)
         (when-false ldvalb 0)
         (end)))])
```

Hmm, what's going on here!  A bunch of new stuff.  Firstly, the _with-syntax*_ form alows you to introduce several bits of syntax, where later ones are able to reference the earlier ones.

You can see two labels are introduced *when-false* and *end* (the details are not important, its basically just a unique string).  The we have some magic in *cases ...*.  You can see on the left hand side of the expression we have an ellipsis - this says that we are creating several bits of syntax here that we can refer to later, based on the ellipsis in the parent syntax, in this case our sequence of *expr* expressions.

So, the implementation of a *case* is to evaluate the expression, load 0 on the to stack and then call the branch-equal (beq) opcode with the jump target of *when-false*. This is basically going to check each expression result against 0 (false) and if it is, then jump to the *when-false* label, short-circuiting the evaluation of the rest of the exressions.

You can see then in the actual syntax that is returned, the first thing that happens is that the *cases ...* syntax is spliced in using the unuqote mechanism.  We know that if we reach the code beyond the cases, they must have all returned true - so we load 1 onto the stack and uncondtionally branch to the *end* label. Next, you can see the *when-false* label which loads 0 onto the stack and continues.  Finally. the *end* label which signifies where whatever the next code to be assembled is.

(note - the *cases ...* did not have to be introduced in the *with-syntax*, it could have just been in the body like before - I was showing alternate ways to include ellipsis based syntax)

# Macros in macros in macros in macros ...


Let's take this to the next level by introducing a truly higher level macro, that is defined almost entirely using other macros.   drey-vm has the concept of what I have called _flowroutines_ TM.  As their name indicates, they are similar to co-routines, and seamlessly handle getting a response from a particular client.   To use them, you create a *flow* object and add a bunch of "choices" into it - these are basically a string and an index.  Upon calling the opcode *suspend*, the VM will turn the flow object into a json request, send it to the client over ZMQ and suspend execution until it gets a result.  When the client responds, the VM can assert the response is a valid one and then load the client's choice index on to the stack and continue executing.   Here is an example on how it looks in a game I am writing.

```racket
 (def-λ (enter-temple player location)
   ;player can donate up to three coins to the temple
   (extract ([(clientid coins) player])
     (def-λ (coins->temple n)
       (prop-= player "coins" n)
       (prop+= player "points" n)
       (prop+= location (concat clientid "-coins") n))
     (when (gt coins 0)
       (flow clientid "donate to the temple"
         ([#t           "1 coin"  (~ coins->temple 1)]
          [(gt coins 1) "2 coins" (~ coins->temple 2)]
          [(gt coins 2) "3 coins" (~ coins->temple 3)]
          [#t           "do not donate" '()])))))
```

There's several other scurry things you have not seen yet, including lambda functions with closures,  *extract* which is a form of pattern matching, and function application ( ~ ).   *flow* is what we are interested in here, though.  You can see it accepts a clientid and some title, then it is followed by a list of expressions.  Each expression starts with a predicate that determines if this choice shoud be included, then a name for the choice, and finally, an expression that will be evaluated if the user chooses that particular choice.

They way I wrote this (and all the other higher level macros) is to simply write the syntax how I would like to use it (as above), then re-write what it will need to look like expanded, and finally attempt to write a macro to convert from one to the other.  So, in this case, I would like the (flow ...) expression above to be re-written to the following, rather boring and full of boiler-plate code, that has nothing do with the actual itent of what is happening:

```racket
(def-flow some-generated-name "enter temple")
; add cases
(when #t
  (add-flow-action some-generated-name 0 "1 coin"))
(when (gt coins 1)
  (add-flow-action some-generated-name 1 "2 coins"))
(when (gt coins 2)
  (add-flow-action some-generated-name 2 "3 coins"))
(when #t
  (add-flow-action some-generated-name 3 "do not donate"))
; suspend to client
(def another-generated-name (flow some-generated-name clientid))
; dispatch based on response
(cond
    [(eq another-generated-name 0) (~ coins->temple 1)]
    [(eq another-generated-name 1) (~ coins->temple 2)]  
    [(eq another-generated-name 2) (~ coins->temple 3)]
    [(eq another-generated-name 3) '()])

```

I think we can agree, the proposed syntax is way nicer than having to manually write that lot all the time! However, it sems like a tall order - somehow we have to generate those index numbers, and although we are repeating code for each input case, we need several pieces of generated code in different places - how is it  going to work?  Amazingly, it is not that diffcult - other than a trick to generate the index numbers.

Let's start with the flow clauses.  They consist of three expressions each, and there may be several of them.   Racket allows us to define a *splicing syntax class* that is exactly the same as the syntax classes we've already seen except it works with ellipsis patterns.  Here it is:

```racket
(begin-for-syntax
  (define-splicing-syntax-class flow-clauses
    [pattern {~seq [eq-expr case-title-expr true-expr] ...}
             #:with [n ...] (map ~a (range (length (attribute eq-expr))))]))
```

Here you can see we expect a *flow-clause* to contain three expressions in brackets, and indicate there may be many of them with the ellipsis at the end.  The #:with is some magic I struggled to get to work - however it is very similar to the *cases* from earlier -  it indicates there will be one *n* for each pattern, and we produce a list of numbers from 0 to the amount of patterns that exist by using the *range* function and testing the *eq-expr* attribute's length from the syntax.

Phew, that was a bit of a hard one.   Thankfully, with that bit written and functioning, the rest is actually fairly striaght forward.  Notice how it closely resembles the manually written out version above.


```racket
(define-syntax-parser flow
  [(_ client title-expr (expr:flow-clauses))
   (with-syntax
     ([flow-name (string->symbol (new-var))]
      [resp-name (string->symbol (new-var))])
   #'`(,(def-flow flow-name title-expr)
       ((,(s-when expr.eq-expr
          (add-flow-action flow-name expr.n expr.case-title-expr))...))  
       ,(def resp-name (flow flow-name client))
       ,(s-cond
          [(eq resp-name expr.n) expr.true-expr] ...)
       ))]
  [(_ req client)
   #'`(,(eval-arg req)
       ,(eval-arg client)
       (suspend))])
```

Notice here how we use the ellipsis *twice* in this to generate code for the whens and the cond - racket is perfectly happy with this and pretty much does exactly what you expect! Fairly amazing stuff!  The case at the bottom is an overloaded *flow* that does the actual work of loading the flow object, client, and calling *suspend* with it.

# End

 I hope this post has given you some idea on the power of these racket macros!  I still have a long way to go, no doubt - I am constantly surprised with the new stuff I learn.   If you want to see more of these macros for scurry (there's over 1k lines of them) you can have a look in my [github here](https://github.com/pezipink/scurry/blob/master/asm.rkt).  If you are interested in seeing more of these or would like to know more about the virtual machine itself, please let me know in the comments!

Happy macroing !