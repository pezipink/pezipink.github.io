    Title: Fairylog
    Date: 2019-04-17T15:02:55
    Tags: fpga,racket,digital logic,fairylog

Over the last few months I have been working on yet another new language, this time for programming FPGAs.  This post will provide a short introduction to Fairylog by way of  building some custom hardware to read a pair of Nintendo pads.

<blockquote class="twitter-tweet" data-lang="en"><p lang="en" dir="ltr">Realtime 24bit colour rotozoomer controlled by a NES pad. CPU, video hardware and assembler built in my various <a href="https://twitter.com/racketlang?ref_src=twsrc%5Etfw">@racketlang</a> langs. Display from <a href="https://twitter.com/adafruit?ref_src=twsrc%5Etfw">@adafruit</a>. Debugger and supporting tools <a href="https://twitter.com/hashtag/csharp?src=hash&amp;ref_src=twsrc%5Etfw">#csharp</a> &amp; <a href="https://twitter.com/hashtag/fsharp?src=hash&amp;ref_src=twsrc%5Etfw">#fsharp</a>. Complete with dramatic music! 😃 <a href="https://twitter.com/hashtag/fpga?src=hash&amp;ref_src=twsrc%5Etfw">#fpga</a> <a href="https://t.co/8gVNfg4uLD">pic.twitter.com/8gVNfg4uLD</a></p>&mdash; Ross McKinlay (@pezi_pink) <a href="https://twitter.com/pezi_pink/status/1103716209537794048?ref_src=twsrc%5Etfw">March 7, 2019</a></blockquote>
<script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>


<!-- more -->

### FPGA

After my last foray back into some electronics (see the previous post) I decided I'd like to focus more on hardware again.  This time around, I thought it was high time I examined and learned about FPGAs properly - not sure why I didn't do this years ago, but here we are.

If you don't know what an FPGA is, it is essentially a huge grid of programmable logic cells that you can arrange in any way you like. This enables you to create a custom piece of hardware that is capable of becoming any digital system you can imagine. A good introduction can be found at [this EEVBlog video](https://www.youtube.com/watch?v=gUsHwi4M4xE)

In order to program one of these devices, you typically write code in a hardware description language like [Verilog](https://en.wikipedia.org/wiki/Verilog) or [VHDL](https://en.wikipedia.org/wiki/VHDL). Now, I have never wrote any HDL code before,  but after examining Verilog for a few hours I knew we were not going to be friends.

Instead, I decided I would learn Verilog by writing my own source-to-source compiler and language for it.  Probably not the best way to go about it, but it kept me out of trouble for a couple of weeks.

### Fairylog

And so [Fairylog](https://github.com/pezipink/fairylog) was born.  Fairylog is a [Racket](https://racket-lang.org/) language (of course) which aims to be quite like Verilog, with less redundant syntax, Racket macros, and several additional compile-time features that Verilog seems to be lacking.

Fairylog *extends* Racket rather than replaces it, which means you can use all of Racket wherever you like in Fairylog code to help you generate stuff.

Verilog is a fairly large language, which has two not-very-distinct sides to it.  Ultimately, Verilog is compiled into something that is either run through a *simulator*, or *sythesised* and programmed onto real hardware.  The language itself has some blurred lines around what is synthesisable and what is not, which is not at all obvious and differs between toolchains and chips.

Fairylog is aimed at *synthesisable* Verilog code.  I have not yet tried a simulator and don't plan on doing so for now, since I am much more interested in getting the stuff running straight on the hardware. For that reason, you won't find many of the simulation-only language constructs from Verilog in Fairylog. Since I didn't (and still don't) really understand which bits are relevant and which aren't, the language already has some redundant stuff in it and is still missing some areas of Verilog which I have not got around to playing with yet.

It is fully capable, however, if still somewhat of a rough personal experiment for mostly my own use.  I did manage to design and build a debuggable 32 bit computer with it, along with a 64x64 RGB LED matrix in 24 bit colour and dedicated video hardware - you can see a couple of videos here.

<blockquote class="twitter-tweet" data-lang="en"><p lang="en" dir="ltr">64x64 <a href="https://twitter.com/adafruit?ref_src=twsrc%5Etfw">@adafruit</a> RGB LED matrix running on an FPGA programmed with my new language <a href="https://twitter.com/hashtag/fairylog?src=hash&amp;ref_src=twsrc%5Etfw">#fairylog</a>  - watch this space !  // <a href="https://twitter.com/racketlang?ref_src=twsrc%5Etfw">@racketlang</a> <a href="https://t.co/ekaxKVZ5cZ">pic.twitter.com/ekaxKVZ5cZ</a></p>&mdash; Ross McKinlay (@pezi_pink) <a href="https://twitter.com/pezi_pink/status/1095403160284352513?ref_src=twsrc%5Etfw">February 12, 2019</a></blockquote>
<script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>

<blockquote class="twitter-tweet" data-lang="en"><p lang="en" dir="ltr">Realtime hardware plasma 😃3 bit colour, no cpu, no ram blocks <a href="https://twitter.com/hashtag/fpga?src=hash&amp;ref_src=twsrc%5Etfw">#fpga</a> <a href="https://twitter.com/hashtag/Fairylog?src=hash&amp;ref_src=twsrc%5Etfw">#Fairylog</a> <a href="https://t.co/9vQurYgMy2">pic.twitter.com/9vQurYgMy2</a></p>&mdash; Ross McKinlay (@pezi_pink) <a href="https://twitter.com/pezi_pink/status/1095807544457678853?ref_src=twsrc%5Etfw">February 13, 2019</a></blockquote>
<script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>

<blockquote class="twitter-tweet" data-lang="en"><p lang="en" dir="ltr">Remember the 3-bit hardware plasma? Here&#39;s a 24-bit colour version, this time in software. Running on a custom built CPU and video hardware, outputting to a 64x64 <a href="https://twitter.com/adafruit?ref_src=twsrc%5Etfw">@adafruit</a> RGB LED matrix. Built from the ground up in my forthcoming language <a href="https://twitter.com/hashtag/fairylog?src=hash&amp;ref_src=twsrc%5Etfw">#fairylog</a> // <a href="https://twitter.com/racketlang?ref_src=twsrc%5Etfw">@racketlang</a> <a href="https://twitter.com/hashtag/FPGA?src=hash&amp;ref_src=twsrc%5Etfw">#FPGA</a> <a href="https://t.co/YDGPol2ORV">pic.twitter.com/YDGPol2ORV</a></p>&mdash; Ross McKinlay (@pezi_pink) <a href="https://twitter.com/pezi_pink/status/1100132467707707395?ref_src=twsrc%5Etfw">February 25, 2019</a></blockquote>
<script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>

<blockquote class="twitter-tweet" data-lang="en"><p lang="en" dir="ltr">My cpu now has a working call stack and separate data stack. To celebrate, I have wrote this small but colourful and all-realtime chessboard zoomer, which uses neither of them <a href="https://t.co/txLiqY42ZH">pic.twitter.com/txLiqY42ZH</a></p>&mdash; Ross McKinlay (@pezi_pink) <a href="https://twitter.com/pezi_pink/status/1102937308771295232?ref_src=twsrc%5Etfw">March 5, 2019</a></blockquote>
<script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>


### Hello World

To show you how Fairylog works, we need a project.  Most hardware stuff gets big quite quickly, so as a small intro we'll write a piece of hardware that can read the data from two Nintendo gamepads.

But first, the absolute basics.  The hello world of the hardware world is, of course, flashing some LEDs.  Grab your favourite FPGA and download Fairylog from the [Racket package manager](https://pkgs.racket-lang.org/package/Fairylog). (note: it is out of scope to try and properly teach either Racket or Verilog here, hopefully you know some of both)

```racket
#lang fairylog

(vmod blinky
  ([clk_100mhz #:input #:wire [1]]  ; master clock signal
   [board_leds #:output #:reg [4] 0]) ; leds on FPGA board

  (locals
    [counter #:reg [24]])
    
  (always ([#:posedge clk_100mhz])
    (begin
      (set counter (+ counter 1))
      (when (& counter)
        (set board_leds (+ board_leds 1))))))

```

A Fairylog program is represented by a bunch of .rkt files, each of which may have several Verilog modules in it.  By default, when you compile the program (usually from the REPL) Fairylog will produce a .v file for each .rkt file that it finds containing module code, into the same directory. You can then use these files as part of your project in the huge toolchain from whichever vendor's FPGA you own.

As you can see, `vmod` creates a named module. It is followed by a list of input and output *ports*.  Each port must be marked as `#:input`, `#:output` or `#:inout`.  A port must also have a type - all of the standard Verilog ones are supported - here we are using the most common types of `#:wire` and `#:reg`.  Finally, you are able to specify the size of the port in bits, along with a default value if it is of type `#:reg`

Verilog requires that you specify sizes for everything in the awkward syntax of `[msb : lsb]`.  Since 95% of the time you simply want *n* bits, Fairylog provides the shorthand you see in the above code. However, it does support the full Verilog syntax as well should you want it:

```racket

;these definitions are equivalent and define a 4-bit reg
[board_leds #:output #:reg [4]]
[board_leds #:output #:reg [3 0]] ;verilog style

```

Next up is the module body itself.  The `locals` form introduces locally scoped wires and regs. The syntax is the same as the port syntax except you can't specify directions. 

The `always` block has a number of uses in Verilog, although it can be quite confusing at times as to which you should use.  Fairylog attempts to make this a bit simpler by not allowing code that doesn't make sense inside an `always` block.

```racket
(always ([#:posedge clk_100mhz])
  (begin
    (set counter (+ counter 1))
    (when (& counter)
      (set board_leds (+ board_leds 1)
```

In this example you see `always` is followed by a Verilog *sensitivity list*.  In this case it indicates that the code to follow should execute on the rising edge of the `clk_100mhz` signal.  Because of this, all *assignments* that happen in the body will implicitly be *non-blocking* (the `<=` operator in Verilog parlance) .  The sensitivity list works as you would expect it to if you know Verilog - you can `or` stuff together and use `#:negedge` as well as `#:posedge`.

If you want to use Verilog's other `always` form, `always @(*)`, you can use Fairylog's `(always * expressions ...)`.  When inside this form, all *assignments* are implictitly *blocking* (using the `=` operator)

**_It is not possible to use incorrect mixed blocking/non-blocking operators in a Fairylog program_**

Finally, the body contains some familiar Racket forms like `begin`, `when` and `set`. You can also use other Racket style flow control such as `cond` `if` `unless` `case` and so on, as we will see.  `(& counter)` is an example of using one of Verilog's special unary operators known as a *reduction operator*. This one in particular, `&`, tests if all the bits are set on the given operand.   The whole suite of Verilog operators are supported, but in the prefix style as preferred by Lisp.

*note: because Verilog and Racket share some forms with the same name, if you explicitly need a Racket one in some ad-hoc code, they have been renamed to have a r: prefix, eg _(r:begin ...) (r:case ...)_*

### Macros

```racket
(always ([#:posedge clk_100mhz])
    (begin 
      ...
      ))
```

This code is so common in Verilog that it quickly becomes tiring to write it all the time.  Thankfully we have Racket's macro system on hand to help:

```racket
(macro always-pos
  [(_ clock exprs ...)
   #'(always ([#:posedge clock]) 
       (begin exprs ...))])

```

`macro` here is simply [define-syntax-parser](https://docs.racket-lang.org/syntax/Defining_Simple_Macros.html?q=define-syntax-parser#%28form._%28%28lib._syntax%2Fparse%2Fdefine..rkt%29._define-syntax-parser%29%29) in disguise, provided to you by Fairylog.  You can of course bring in whatever Racket libraries you like and use the whole shooting match.  The `always-pos` macro is already provided to you out of the non-existent box by Fairylog.

```racket
#lang fairylog

(vmod blinky
  ([clk_100mhz #:input #:wire]  ; master clock signal
   [board_leds #:output #:reg [4]]) ; leds on FPGA board
  (locals
    [counter #:reg [24]])
  (always-pos clk_100mhz
    (inc counter)
    (when (& counter)
      (inc board_leds))))
    
```

That's a bit nicer.  In case you were wondering, the compiled code looks like this:

```verilog
module blinky (
  input wire  clk_100mhz ,
  output reg [3:0] board_leds );
reg [23: 0] counter ;
always @(posedge clk_100mhz)
  begin
    counter <= (counter + 1);
    if(&counter)
      begin
        board_leds <= (board_leds + 1);
      end 

  end 
endmodule

```


### Number literals, Ranges and Arrays

Verilog is very picky about its numbers. It does let you use decimal literals, which are always interpreted as 32 bit integers - you must be careful with these as they can lead to many bugs.  Fairylog also supports decimal literals in the same manner.  If you write a number simply as `42` then expect the same behavior.

Other than that, Verilog expects you to fully qualify every number with both its radix and amount of bits followed by the actual literal, eg `8'h7`  `8'b1` `10'd20` and so on.  Fairylog supports this full syntax with the slightly more sane `size_radix_literal` eg

```racket

(locals
  [a #:reg [8] 8_16_FF]
  [b #:reg [8] 8_2_10101010])
  
```  

These literals are checked at compile time and will error if they don't make sense.

Since you are often writing binary and hex values, Fairylog offers a shorthand literal syntax where the size will be inferred from the literal itself.  These are very handy and mostly eliminate the need for the fully qualified syntax:

```racket

(locals
  [a #:reg [8] $FF]
  [b #:reg [8] %1010_1010])
  
```  

For the hex literals, the inferred size will be the maximum based on the number of supplied characters, rather than the actual number.  For example, `$FF` `$1f` and `$0f` are all inferred as a size of 8.

Binary literals can have any amount of underscores in them to help visually separate numbers of bits.

In general, most assignment expressions are compile time checked.  Unlike most Verilog implementations, you will get warnings when stuff does not fit or would be truncated!  Be sure to check your warnings, they will catch many a hard to spot Verilog bug.  Not everything is checked yet, however ... 

To extract a range of bits from an operand, treat the operand as if it were a function:

```racket

(locals 
    [a #:reg [(r:* 4 2)]]     ; you can use any racket expressions in these!
    [b #:wire [1] (a 1)]    ; bit 1 of a
    [c #:wire [3] (a 7 5)]) ; bits 7 to 5 of a

```

And finally, use the following syntax to declare and access an array.  The range syntax will also work for arrays, you must specify all dimensions followed by either a single index or range pair as normal - this will be checked at compile time.

```racket

(locals
  [memory_array #:reg [32] (array 16)]
  [a #:wire [32] (memory_array 0)]  ; first 32 bits
  [b #:wire [1] (memory_array 0 2)] ; second bit of first 32 bits
  [c #:wire [1] (memory_array 0 7 0)]) ; low byte of first 32 bits
  
```

### Nintendo Pads 

With the basics out of the way, let's try to do something useful.  The NES pad is basically a [4021 shift register](https://en.wikipedia.org/wiki/Shift_register).  It takes a parallel interface (8 button signals) and turns them into a serial interface we can read using 3 pins rather than 8.  The important pins are **Latch**, **Clock**, and **Data**.  In order to read the state of the buttons, we must

1. Pulse the **Latch** pin  High to Low.  This takes the current button states and latches them into the register.
2. The state of button 0 will now appear on the **Data** line where we can grab it from.
3. Pulsing the **Clock** line High to Low causes the next button state to appear at **Data**
4. Repeat until all eight buttons have been read, resulting in a byte that represents the pad state.

First, we'll have a go implementing this largely in the way you would if you are from a software background.

Let's create a new file somewhere for this lovely re-usable module,  `lib-nes.rkt`

```racket

#lang fairylog

(vmod nes_pads
  ([clk_100mhz #:input #:wire] ; master clock signal
   [nes_data1 #:input #:wire]  ; data line from pad 1
   [nes_data2 #:input #:wire]  ; data line from pad 2
   [nes_data #:output #:reg [16]] ; results register, both data combined in 16 bits
   [nes_clk #:output #:reg 0]  ; the pad's CLK line
   [nes_lat #:output #:reg 0]  ; the pad's LATCH line
   )

  ; implementation ...
)

```

I have a small module that brings two NES pads togther under one interface.  That is, the **Clock** and **Latch** lines are tied together, and you can read the two values on **Data1** and **Data2**.  The idea will be to read both pads and produce one 16-bit number with the two pad states.

Digital electronic components are highly sensitive to accurate timing.  You can find the minimum and maximum timing requirements for different operations for the various devices in their [datasheets](http://www.ti.com/lit/ds/symlink/cd4021b-q1.pdf).  Most modern 4021 shift registers have a minimum response time of 180ns.  I am using a real NES pad from the early 80s and it doesn't seem to work at such high speeds - with some experimentation  640ns or more seems to work quite well - it can go faster but we are not in a rush.

Our first problem, then, is timing.  The board is providing a clock signal at 100mhz which is 1 cycle every 10ns - way too fast for what the poor pad can keep up with.  What we need is to divide the clock and create a new, slower signal to base our logic around.

```racket

  (locals
   [counter #:reg [6]]
   [clk_int #:reg])

  (always-pos clk_100mhz
   (when (& counter) 
       (set clk_int (~ clk_int)))
   (inc counter))
   
```

Since a 6-bit register can count up to 64 before wrapping around, this will give us the delay we need (64 * 10ns).  The `clk_int` register is inverted each time the counter rolls over, giving us the new clock signal (we'll see another way to achieve this shortly)

```racket

  (enum nes-state
        ready
        latching
        next-bits
        read-bits
        finish)
        
  (locals
   [state #:reg [3] nes-state.ready]
   [bits_shifted #:reg [4]]
   [data1 #:reg [8]]
   [data2 #:reg [8]])

  (always-pos clk_int
    (match state nes-state
      [ready 
       (set [nes_lat 1]  ; latch high to grab new data
            [data1 0]    ; zero out everything else
            [data2 0]
            [bits_shifted 0]
            [state nes-state.latching])]
      [latching ;first bit will be available
       (set [nes_lat 0]  ; latch low
            [data1 nes_data1] ;grab first bits of data 
            [data2 nes_data2]
            [nes_clk 1]  ;begin clocking 
            [state nes-state.read-bits])]
      [next-bits
       (set [nes_clk 1] ;end clock
            [state nes-state.read-bits])]
      [read-bits
       (set [data1 (concat (data1 6 0) nes_data1)] ;concat new data in
            [data2 (concat (data2 6 0) nes_data2)]
            [nes_clk 0]
            ;check to see if we have finished
            [bits_shifted (+ bits_shifted 1)]
            [state (if (< bits_shifted 6)
                       nes-state.next-bits
                       nes-state.finish)])]
      [finish
       ;done, assign the final value and reset FSM
       (set [nes_data (concat data2 data1)]
            [state nes-state.ready])])
            
```

Coming from a software perspective, in hardware code there's no such thing as loops, so we have to encode everything in state machines.  Fairylog has a compile-time feature called `enums` .  As you can see here, you can define an `enum` with a bunch of names (and optionally integer values).   Then, `match` can be used with an enum to provide a switch-case style dispatch on the items.  Most importantly, **_the compiler exhaustively checks match cases_** - that is, it will error if you do not supply all the of cases, which proves invaluable for maintenance since you'll be using a lot of state machines.

It is also easy to fall into the trap here of reading the chains of `sets` as happening procedurally like they would in a language like C.  This is not the case, since in hardware everything generally happens in parallel, at the same time.  In this case, we are using an `always` block with our new slow clock signal `clk_int`, and everything inside is based on flip-flops that work in sync with the clock signal.  New values are not available to read until the *next* clock cycle.   That is why the conditional in `read-bits` that checks `(if (< bits_shifted 6))` is not `7`, because the line above that increments `bits-shifted` hasn't actually happened yet!

To use the module we'll have to instantiate it from somewhere else.  Let's create a file called top.rkt

```racket

#lang fairylog
(require "lib-nes.rkt")
(vmod top
  ([clk_100mhz #:input #:wire]
   [nes_clk #:output #:wire]  ;NES pad connections
   [nes_lat #:output #:wire]
   [nes_data1 #:input #:wire]
   [nes_data2 #:input #:wire]
   [board_leds #:output #:wire [4]])

  (locals
   [nes_data #:wire [16]])

  (vmod nes_pads
   [clk_100mhz clk_100mhz]
   [nes_clk nes_clk]
   [nes_lat nes_lat]
   [nes_data1 nes_data1]
   [nes_data2 nes_data2]
   [nes_data nes_data])

  (assign board_leds (^ $f (nes_data 3 0))))

```


You'll see here we include the `lib-nes.rkt` file just like you would in a normal Racket program.   The ports for the `top` module include the hard signals from the FPGA board for the various pins of the NES controller (inputs and outputs), the main system clock, and 4 LEDs that I happen to have on the board.

The inner `vmod` form is used for module instantiation.  You must supply a list of port mappings for the target module's ports.  In this case, they all happen to be called the same thing.

You'll notice here that `nes_data` is defined as a `wire` type.  This is because the data is actually stored in the other module - we only want a connection to it.   

The last form here is `assign` which we have not seen yet.  This is Verilog's *combinatorial* logic - that is, stuff that happens outside of clock signals.  These signals update as soon as something that affects them changes.  In this case we connect our `board_leds` output wire to the last four bits of the `nes_data` wire, and XOR it with $f.  In other words, the LEDs on the board will directly represent the last four buttons from pad 1.  We do not need to explictly update anything, it will happen automatically.  

The XOR $f inverts the bits, since the pad outputs 0 for pressed and 1 for not pressed, and I'd like the LEDs to light up as the buttons are pressed.

```verilog
module nes_pads (
  input wire  clk_100mhz ,
  output reg  nes_clk  = 0,
  output reg  nes_lat  = 0,
  input wire  nes_data1 ,
  input wire  nes_data2 ,
  output reg [15:0] nes_data );
reg [2: 0] state  = 0;
reg [3: 0] bits_shifted ;
reg [7: 0] data1 ;
reg [7: 0] data2 ;
reg [5: 0] counter ;
reg  clk_int ;
always @(posedge clk_100mhz)
  begin
    if(&counter)
      begin
        clk_int <= ~clk_int;
      end 

    else
      begin
        counter <= (counter + 1);
      end 

  end 
always @(posedge clk_int)
  begin
    case (state)
      0 : // ready
      begin
        nes_lat <= 1;
        data1 <= 0;
        data2 <= 0;
        bits_shifted <= 0;
        state <= 1;
      end 

      1 : // latching
      begin
        nes_lat <= 0;
        data1 <= nes_data1;
        data2 <= nes_data2;
        nes_clk <= 1;
        state <= 3;
      end 

      2 : // next-bits
      begin
        nes_clk <= 1;
        state <= 3;
      end 

      3 : // read-bits
      begin
        data1 <= {data1[6 : 0], nes_data1};
        data2 <= {data2[6 : 0], nes_data2};
        nes_clk <= 0;
        bits_shifted <= (bits_shifted + 1);
        state <= ((bits_shifted < 6) ? 2 : 4);
      end 

      4 : // finish
      begin
        nes_data <= {data2, data1};
        state <= 0;
      end 

    endcase
  end 
endmodule

```

Notice how, in the compiled Verilog code, Fairylog has added helpful comments related to the states from the enum. I did this because you'll be spending a lot of time wondering what is going on in your state machines!

### NES++ Paradigm Shift!

Whilst the above design works, it is not very idiomatic.  In hardware you generally want to stay away from lots of flip-flop based state and complex conditional logic (not that this example is very complex!).  Instead, we shoud aim to exploit the massively parallel nature of the circuitry and combine the combinatorial (pun intended) and clock-based approaches.  This will lead to far fewer FPGA resources being consumed and better optimisation potential.

Let's first see a different way of generating the divided clock signal:

```racket
  (locals
   [counter #:reg [7]]
   [clk_int #:wire [1] (counter 6)])

  (always-pos clk_100mhz (inc counter))

```

Here we have changed counter to be 7 bits instead of 6, and `clk_int` is no longer a reg but instead a wire that is high when bit 6 (0 based, the MSB) of `clk_int` is set and low if not. *(note: as in Verilog, using a default expression on a wire declaration like this is the same as later using `assign` with it)*

Because a 7 bit number can hold exactly double what a 6 bit number can, this has the affect of the clock signal being low right up until `%011_1111` and then high from `100_0000` to `111_1111`, effectively giving us the same clock period as before, without the extra logic to control it.

```racket
  (locals
   [phase #:reg [4]]
   [phase_latch #:wire [1] (== phase 0)]
   [phase_end   #:wire [1] (== phase 7)]
   [phase_data  #:wire [1] (! phase_latch)])
  
  (assign
   [nes_clk (&& phase_data clk_int)]
   [nes_lat phase_latch])

  (always-pos clk_int
    (inc phase)
    (cond
      [phase_latch
       (set [data1 0]
            [data2 0])]
      [phase_end
       (set [nes_data (concat (concat (data2 6 0) nes_data2)
                              (concat (data1 6 0) nes_data1))]
            [phase 0])]
      [phase_data
       (set [data1 (concat (data1 6 0) nes_data1)]
            [data2 (concat (data2 6 0) nes_data2)])]))

```

This next set of changes removes the old state machine completely.  It is based on the observation that we need to do 8 things in sequence, and the state of the **Latch** and **Clock** pins can be determined by a combination of the current phase, and the clock.

First, we setup some wires that state `phase_latch` is high when `phase` is equal to zero.  `phase_end` is high when `phase` is equal to 7, and finally, `phase_data` is high whenever `phase_latch` isn't.

The actual NES ports have been changed from `#:reg` to `#:wire` (not seen here) and are now driven by combinatorial expressions.  `nes_lat` is now high only when `phase_latch` is.  `nes_clk` is high only when both `phase_data` is AND the `clk_int` is high.

If you think about this you will realise it has the effect on automating the signals completely for us, cycling in each bit of data as long as it is not latching.

All that remains then, is to continually increase `phase`, assigning the results to `nes_data` during `phase_end` and concatenating new intermediate data during `phase_data`.

Setting the data back to zero in `phase_latch` could also be removed completely since it will be overwritten anyway.

This design leads to a 30%~ reduction in FPGA resources!  Of course, in such a small design it is negligible but it soon mounts up in larger and more complex modules.


### Final Thoughts

Now you can write Verilog code with **RACKET MACROS** what more needs to be said?   As an example of what is possible, the CPU I designed that was programmed to produce the videos at the top of the post, was able to *mostly implement its own instruction set* through the use of macros. 

This led to way less problems, less code, and most importantly a lot more fun for me to program, givng me more time to work on the cool stuff (apologies Verilog programmers, if you enjoy writing all that then good for you!).

Fairylog is far from complete however, and there are no docs.  It does not yet support Verilog Tasks, and some other stuff which I don't know anything about yet.  There might be other mistakes as well since I am new at all these HDL shenanigans. 

At this point it is still a tool mostly designed by me, for me, so you can expect it might change quite a bit at any time. Testament to Racket though, the original working version of Fairylog took me just 2 weeks to get going.

As a parting entertaining anecdote,  I have still managed to never write an actual Verilog program from scratch!  Achievement unlocked :)
