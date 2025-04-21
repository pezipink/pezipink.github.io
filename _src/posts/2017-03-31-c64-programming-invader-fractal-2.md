    Title: C64 Programming - Invader Fractal #2
    Date: 2017-03-31T12:23:17
    Tags: C64,6502

In the [last post](http://pinksquirrellabs.com/blog/2017/02/28/c64-programming-invader-fractal/) we discovered how to decode 15-bit symmetric invaders into a chacrater set.  This time around we will make them fractal!

The invaders from the last post occupy exactly one character each.  The aim is to now select an invader at random, and then draw it one size bigger, using other random invaders as its "pixels" like this

![](../../../../../img/invaders/7.png)

Then, the process can be repeated again, drawing an even bigger invader which is formed by drawing even bigger "pixels" which are composed of the size 2 invaders, like this!

![](../../../../../img/invaders/8.png)

<!-- more -->

### A plan of action

Writing in ASM is very different to high level languages.  You can't simply jump in and start writing code with only a very loose idea, at least some stuff has to be planned out up ahead if you want to get anywhere without wasting time. (Well, I guess with more experience this get easier ... )

In order to draw the first level of invader, we are going to need the following high-level elements

* A way of selecting a random invader and calculating where in character set memory it sits
* An algorithm that takes each bit of each row of the invader in turn, and draws another invader into video memory if the bit is set
* Another alogrithm that does a very similar thing to the previous one, on a bigger scale

I have a small not-very-random number generator routine originally called `rng`.  Its implementation is out of the scope of this post, but you can call it as follows

```ca65
jsr rng		
```


`jsr` is an opcode that jumps to the named routine, after first pushing the current location on the stack, then when it encounters an `rts` it returns execution to the calling code.  The `rng` routine will leave a not particularly random number inside the accumulator ready for use.


### Choosing an invader

The program I wrote populates a character set at memory location $2000 with 254 sort of unique invaders, using the technique described in the previous post and the `rng` routine.  I defined characters 0 and 1 in the set manually, where 0 is blank and 1 has every pixel set.  A character in memory is layed out as 8*8 contiguous bytes, each one represeting the pixels that are switched on for each row.  Therefore, the location in memory of character `n` is `$2000 + (n * 8)`.  The 6502 does not have any multiplication instructions - although you can of course write your own by using a combination of adding and bit shifting, which would be fast.  However, for this task we will just write a loop that adds 8 n times.

```ca65
draw_invader: subroutine
    ; store the address at $3E and 3F
    ; starting at $2000 where the character memory begins
    lda #$0
    sta $3E
    lda #$20
    sta $3F
    ;; pick random invader number
    jsr rng
    sta $3D
    ;; add 8 for each count
.calc_address
    clc
    lda #8
    adc $3E
    sta $3E
    bcc *+4
    inc $3F
    dec $3D
    bne .calc_address
```


This is fairly straight forward -  the address is 16 bits, and we store it in location $3E and $3F in the zero-page (an arbitary choice) so that it can be used with the indirect addressing mode later, as discussed in the previous article. A random number from 0-255 is generated and stored in $3D, and then we enter a loop that adds 8 to the address, decreases $3D and loops until $3D is zero.  One new thing here is the operand following the `bcc` instruction, `*+4`.  The * tells the assembler to use the current location, which you can then offset by some number.  Effectively this skips the `inc $3F` instruction, which saves having to use a label to do it. Oviously, this technique is error prone and hard to read, but it makes sense for stuff you have to do all the time, like this addition of an 8 bit number to a 16 bit one.  The "subroutine" keyword tells the assembler to uniquely name any labels following it that start with a ".", this is so you don't have to keep dreaming up globally unique label names for your different routines.


### Drawing an Invader

Now we have the correct address, we can work out how to draw the invader. The C64 screen is 40*25 characters.  A character is 8*8 pixels, although the invaders are only actually using 5*5 of them, which will be useful later since it means we can fit one massive invader on the screen, just!

```ca65
    lda #$00
    sta $44
    lda #$04
    sta $45
```


The video memory starts at $0400, and clearly we are going to need to keep track of where we are in order to draw the invader, so another 16 bit address ($0400) is placed in the zero-page at locations $44 and $45 (more random locations).

Now the algorithm can begin:

* Read the current byte of the invader
* For each bit of the byte, determine if it is set
* If it is, pick a random invader character and store it at the current video memory location
* Increase the video location by one
* At the end of the byte, move down to the next row in video memory, at column 0
* Loop 5 times

```ca65
    ldx #5                      ; 5 lines
.draw_line
    ldy #0                      ;clear y, not using it here
    lda ($3E),y                 ;load current invader byte
.loop
    pha                         ;preserve it on the stack
    and #1                      ;check current bit for 1 or 0
    beq .skip
    jsr rng                     ;pick random invader
    sta ($40),y                 ;draw to screen offset by y
.skip
    pla                         ;restore from stack
    iny                         ;increase y offset
    cpy #5                      ;are we finished yet? 
    beq .finish_line            ;(invaders are 5x5 so no point looking at remaning bits)
    lsr                         ;if no, then bit shift one right
    clc                         ;to process next bit, and loop
    bcc .loop
.finish_line
    dex                         ;move to next line if x still > 0
    beq .finish 
    lda #39                     ;add 39 to the video address 
    adc $40                     ;which will move to the next row
    sta $40
    bcc *+4
    inc $41

    inc $3E                     ;add 1 to the invader address to
    bcc *+4                     ;move to its next byte
    inc $3F    
    clc    
    bcc .draw_line              ;loop
    
.finish
```


The indirect addressing is used nicely here, with the Y register effectively offseting the video location and acting as a counter at the same time, whilst X is used to count the rows.   The rest of the stuff should be pretty self explanatory by now - the only new thing here is the slighly odd looking

```ca65
clc
bcc .label
```

All this does is force a branch, so it is similar to `jmp` although my reading indicates it is better to use this style since it makes the code more re-locatable.

### The Final Invader!

The final piece is to draw one massive invader composed of smaller invaders from the previous step across the whole screen.

* Choose an invader at random
* Process the bits as per the first algorithm
* For a set bit, store the desired video memory location and call the other algorithm
* Update the video memory to start at the next location
* Loop

Essentially this routine will seed the other one - the code that sets the video memory location of $0400 is removed from the previous routine, and instead the new routine sets this up before calling it.  Because the first routine modifies those numbers, I have chosen to store the actual location in another zero-page address so that is easier to reason about in the top level routine.  This isn't really necessary but it simplies the problem.

Since the algorithm is mostly the same, I will just highlight a few of the different parts:

```ca65
.loop
    pha                         ;preserve on stack
    and #1                      ;check current bit for 1 or 0
    beq .skip
    lda $44                     ;copy video memory location
    sta $40
    lda $45
    sta $41
    txa                         ; preserve registers
    pha
    tya
    pha
    jsr draw_invader            ; call other routine
    pla                         ; restore registers
    tay
    pla
    tax
```

Notice here we copy the data from the addresses $44 and $45 into $40 and $41 where the `draw_invader` routine reads from. Then, since both routines use the X and Y registers, we have to push the current contents onto the stack so that they can be recovered after the subroutine has executed.  Then we simply generate a random invader number, call the other routine and then restore the status of the registers from the stack.

```ca65
.skip
    iny                         ;increase y offset
    cpy #5                      ;are we finished yet? 
    beq .finish_line            ;(invaders are 5x5 so no point looking at remaning bits)
    lda #8                      ;update video memory location
    adc $44
    sta $44
    bcc *+4
    inc $45
    pla                         ;restore from stack
    lsr                         ;if no then shift one right
    clc                         ;to process next bit and loop
    
    bcc .loop
```

Once again, this is mostly the same as before, except now we update the video address by 8 bytes to move into the next "mega pixel" or "grid location".

```ca65
.finish_line
    pla
    dex
    beq .finish
    lda #167        ;update video memory location to next row
    adc $44
    sta $44
    bcc *+4
    inc $45
    inc $42
    bcc *+4
    inc $43
    clc
    bcc .draw_line
```

Again, very similar to before, and now we also move down 5 rows and back to column zero.  This is what the magic number 167 is doing.

And as if by magic!

![](../../../../../img/invaders/9.jpg)


###Conclusion
You might be thinking, Ross! Why are you repeating yourself in the code!! And that would be a good question.  Unlike higher level languages, quite often it doesn't pay to re-use code in asm.  It is always slower for a start, since you are introducing more subroutine calls, more stack usage and so forth.  Of course I have no doubts that there are a million other ways to write this, in various degrees of cleverness, but all in all I am happy that I got it to work, and the result is quite pleasing!  I have learnt quite a lot from this mini-project, and have bunch of ideas on what to do next :)

