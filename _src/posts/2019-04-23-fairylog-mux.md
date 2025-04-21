    Title: Fairylog : Multiplexer Macros
    Date: 2019-04-23T21:27:04
    Tags: fairylog,fpga,digital logic,racket

Multiplexers and demultiplexers are common tools in digital logic design.  In Verilog, they are fairly simple to create whilst the amount of signals are small.  In this post we'll look at how Fairylog's macros can make short work of generating mux/demux of any complexity, greatly reducing the amount of work and scope for hard-to-find bugs

![](../../../../../img/fairylog/mux.png)

<!-- more -->

### MUX

A mutliplexer in its most basic form takes in a bunch of inputs, and based on a selection input, outputs one of them.

```racket
#lang fairylog
;2:1 mux
(vmod mux
  ([sel #:input #:wire]
   [in_a #:input #:wire]
   [in_b #:input #:wire]
   [out #:output #:wire])
  (assign out
    (case sel
      [0 in_a]
      [else in_b])))

```

This is about as simple as it gets.  A 1 bit selection line that toggles the output between `in_a` and `in_b`.  It produces this code and synths correctly to a MUX.

```verilog
module mux (
  input wire  sel ,
  input wire  in_a ,
  input wire  in_b ,
  output wire  out );
assign out = ((sel == 0) ? in_a : in_b);
endmodule


```

In real world scenarios, we are typically moving lots more data around.  Often it is nice to compact the data in arrays (vectors).

```racket
#lang fairylog
;2:1 mux
(vmod mux
  ([sel #:input #:wire [1]]
   [in  #:input #:wire [2]]
   [out #:output #:wire [1]])
  (assign out
    (case sel
      [0 (in 0)]
      [else (in 1)])))

```

Here we pass the bits in using the same port, and then select them out again in the `case` using the range selection syntax.  You might think you can replace `case` with `(assign out (in sel))` but alas, this will not syth correctly to a MUX.  For the tools to correctly infer a MUX you must supply a terminal case such as the `else` here, which Fairylog compiles out to a ternary expression.

```verilog
module mux (
  input wire sel ,
  input wire in ,
  output wire  out );
assign out = ((sel == 1'b0) ? in[0] : in[1]);
endmodule


```

This is ok for small switches, but in reality we often need much larger multiplexers.  Not only will the sizes of the data be greater, but we often want to swtich multiple signals at the same time based on the same selector.  Verilog also has no way of passing two-dimensional data, so if you want to switch between multiple sets of 8 bits of data, you have to either pass each set in explicitly, or "flatten" it all out into one vector and then switch it out appropriately using ranges. Writing all that code is very time consuming, error prone and hard to maintain.  *note: Verilog does have some rudimentary generation capabilities of its own*

### MUX macros

Let's see if we can automate the process somewhat.  The ony important pieces are the names and sizes of the signals, and then how to map the output based on the selector.  For example, what if we could write this to generate a mux:

```racket
(mux-gen 
  my-mux
  [sel 1] ;1 bit selector
  [in 2]  ;2 bit input
  [out 1] ;1 bit output
  ;output mapping
  ([0 (in 0)]
   ; more cases here as you need them ...
   [else (in 1)]))
   
```

Let's have a first go at it.  `macro` is an alias for [define-syntax-parser](https://docs.racket-lang.org/syntax/Defining_Simple_Macros.html?q=define-syntax-parser#%28form._%28%28lib._syntax%2Fparse%2Fdefine..rkt%29._define-syntax-parser%29%29) and provides all of Racket's [amazing macro system](https://docs.racket-lang.org/syntax/stxparse-patterns.html).

```racket
(macro mux-gen
  #:datum-literals (else)
  [(_ name:id
      [selector:id sel-size]
      [in:id in-size] ...
      [out:id out-size]
      ([case-n:integer res:expr] ...
       [else else-res:expr]))
  #'(vmod name
      ([selector #:input #:wire [sel-size]]
       [in #:input #:wire [in-size]] ...
       [out #:output #:wire [out-size]])
      (assign
       out
       (case selector
        [case-n res] ...
        [else else-res])))])

(mux-gen 
  my-mux
  [sel 1] ;1 bit selector
  [in 2]  ;2 bit input
  [out 1] ;1 bit output
  ;output mapping
  ([0 (in %0)]
   [else (in %1)]))
 

```

This works and produces the same MUX as before.  You can give it any amount of mappings in the case expression, as long as it ends with an `else` to ensure it gets synth'd to a MUX correctly. (in this case it doesn't make sense to, since the input selector is only 1 bit)

Since we allow multiple `in` expressions you can also write this, like the first example. 

```racket
(mux-gen 
  my-mux
  [sel 1] ;1 bit selector
  ; multiple inputs
  [in_a 1]  ;2 bit input
  [in_b 1]  ;2 bit input
  [out 1] ;1 bit output
  ([0 in_a] 
   [else in_b]))

```

This is not very exciting at this stage.  What we want is to be able to supply multiple input -> output mappings for different signals based on the same selector. 

```racket
(mux-gen 
  my-mux
  [sel 1] ;1 bit selector

  ;first input -> output mapping
  ([a_signals 2]  ;2 bit input
   [a_out 1]      ;1 bit output
   ;output mapping
   ([0 (a_signals 0)]
    [else (a_signals 1)]))
    
  ;another mapping
  ([b_data 16] ;two sets of flattened 8 bit data
   [b_out 8]   ;8-bit output
   ([0 (b_data 7 0)]
    [else (b_data 15 8)]))
    
  ; ... as many mappings as we like
)

```

Really all we added here were some extra parens around the block of data after the `sel` so we can identifty it is a group, then added a new block of mappings.  The change to the macro is quite simple:

```racket
(macro mux-gen
  #:datum-literals (else)
  [(_ name:id
      [selector:id sel-size]
      ([in:id in-size] ...
       [out:id out-size]
       ([case-n:integer res:expr] ...
        [else else-res:expr])) ...)
  #'(vmod name
      ([selector #:input #:wire [sel-size]]
       [in #:input #:wire [in-size]] ... ...
       [out #:output #:wire [out-size]] ...)
      (assign
       out
       (case selector
        [case-n res] ...
        [else else-res])) ...)])

```

We have put in the extra parens and slapped in some additional `...` to indicate the block can be repeated.  Then, a few more `...` in the syntax output where we'd like the generated code to be repeated, and we are done. Take special note of the `... ...` that now appears for the `in` ports.  This is because we now have two nested layers of input ports and we wish to unwrap both of them into the port list. It produces this Verilog code:

```verilog
module my-mux (
  input wire [0:0] sel ,
  input wire [1:0] a_signals ,
  input wire [15:0] b_data ,
  output wire [0:0] a_out ,
  output wire [7:0] b_out );
assign a_out = ((sel == 0) ? a_signals[0] : a_signals[1]);
assign b_out = ((sel == 0) ? b_data[7 : 0] : b_data[15 : 8]);
endmodule

```

This little macro is now capable of generating huge custom MUX's to your specifications.  Rejoice!

### DEMUX

The demultiplexer is essentially the reverse - it takes in one input, and dispatches it to one of multiple outputs based on a selector.  Like the multiplexer, we'd typically need to do this for several signals at the same time. This is silghtly more tricky to write, and there are a few ways to do it.  Here is what we are aiming at:

```racket
(vmod demux
  ([sel #:input #:wire [1]]
   [in #:input #:wire [1]]
   [out #:input #:reg [2]])
  (always *
    (set out 0)
    (case sel
      [0 (set out 0) in]
      [else (set out 1) in])))
      
```

As you would imagine, we'd like to support mutliple distinct output signals as well as combined signals like in the above example.  An important point here is that we must make sure all outputs are defaulted to something (zero) otherwise it won't synth properly.

```racket
(macro demux-gen
  #:datum-literals (else)
  [(_ 
    name:id
    [selector:id sel-size:expr]
    ([in:id in-size:expr]
     [out:id out-size:expr] ...+)
    ([case-n:integer targ:expr source:expr] ...+
     [else else-targ:expr else-source:expr]))
   #'(vmod name
       ([selector #:input #:wire [sel-size]]
        [in #:input #:wire [in-size]] ...
        [out #:output #:wire [out-size]] ... ...)
      (always *
        (begin
          (set [out 0] ...)
          (case selector
            [case-n (set targ source)]
            [else (set else-targ else-source)]))) ...)])
```

This is similar to the MUX implementation.  Here we have used `...+` meaning "at least one of the preceding" rather than `...` that means "zero or more of the preceding".   The MUX macro would also benefit from this change.  Other than the actual syntax generated, the only change here is that we have multiple `out` signals for one `in` signal.

### Custom code

Whilst the generated `case` expressions are sufficient for most implementations, sometimes you might want something more custom.  For example,  let's say we wanted to determine the output of the MUX by looking at a range of the input bits, or we wanted to perform some other custom comparison.

It would be nice then if we could choose to override the normal `case` or `always` generation and supply, *for that mapping*, our own expression to use instead. Something like this:

```racket
(mux-gen 
  my_mux
  [sel 8] ;8 bit selector

  ;first input -> output mapping
  ([a_signals 8]  ;8 bit input
   [a_out 4]      ;4 bit output
   #:custom
   ;check if any of the upper 4 bits are set
   (assign a_out           
     (if (| (sel 7 4))               
         (a_signals 7 4)
         (a_signals 3 0))))

  ; ... as many mappings with auto or custom as we like

)

```

We can achieve this fairly easily by introducing a helper macro that deals with the body of the expression, simply passing along whatever was passed in when `#:custom` is detected:


```racket
(macro
 mux-gen-inner
 #:datum-literals (else)
 [(_ ... #:custom custom-expr)
  #'custom-expr]
 [(_ out selector
     ([case-n:integer res:expr] ...+
      [else else-res:expr]))
  #'(assign
     out
     (case selector
       [case-n res] ...
       [else else-res]))])

(macro
 mux-gen
 [(_ name:id
     [selector:id sel-size]
     ([in:id in-size] ...+
      [out:id out-size]
      body-expr ...) ...)
  #'(vmod name
      ([selector #:input #:wire [sel-size]]
       [in #:input #:wire [in-size]] ... ...
       [out #:output #:wire [out-size]] ...) 
      (mux-gen-inner out selector body-expr ...) ...)])

```

And we are done. You could take this any way you like, even creating whole new languages over the top of Fairylog.

### Conclusion

This post intended to show some of the ways that Fairylog can help generate tons of boilerplate code for you - it is merely scratching the surface, however, as the Racket macro system is vast, and definitely something you should look into!

