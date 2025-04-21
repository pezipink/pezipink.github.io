    Title: C64 Sprite Previewer
    Date: 2018-09-17T21:05:58
    Tags: C64, asi64, 6502, racket

In this post we will see how [asi64](http://pinksquirrellabs.com/blog/2017/05/30/asi64/) is more than your average macro assembler, by combining arbitrary disk io, functional programming techniques and code generation alongside your typical 6502 assembly code.  The aim is to create a very simple sprite animation viewer, that writes the resulting C64 program by interleaving file parsing and machine code generation.

Here's the program displaying some sprites that [@silverspoon](https://twitter.com/silverSpoon) has started working on :)  (in different, single colours for the fun of it)

![](../../../../../img/asi/tc.gif)


<!-- more -->

To read this post it would probably help to know some 6502 (though not essential), you can read the assembler syntax over at [asi64's github](https://github.com/pezipink/Asi64) or from an [older post on it](http://pinksquirrellabs.com/blog/2017/05/30/asi64/)

## Sprites

The C64 has 8 hardware sprites it can utilise. This means the video hardware can display up to 8 sprites at once.  To animate something, you would use a series of sprites that you change between. To design these sprites there are various tools available (not like the 80s where you had to draw them manually on graph paper!).  I have been using [spritemate](http://www.spritemate.com/) which is a nice online tool.

Spritemate is able to save the sprites in a variety of formats, including assembly code output for two of the popular C64 assemblers (KickAss and ACME).

What I would like is a way whereby I can design a series of sprite animation frames in the tool, save a number of these files  (each file containing n sprite-frames of animation for some entity), then have the assembler read them from disk and automatically display and animate each sprite on the C64 screen.  This will provide a fast feedback loop to see what they look like on the machine, rather than having to mess around moving chunks of data and altering frame counts and animation code manually.

To display sprites on the C64 you need to have a number of things in place.  This post is not supposed to be a tutorial on how they work, so not everything will be explained. 

* The actual sprite data itself must live somewhere the VIC graphics chip can see it.
* The last 8 bytes of screen memory indicate an offset into the sprite data telling the VIC which sprite to display for each of the 8 sprites.
* Sprite colours and positions are set with bunch of memory-mapped registers on the VIC.

The details on how to configure the VIC for this are out of the scope of this post.  Suffice to say, for my needs,  all the sprite data will be stored at $2000, my screen data lives at $0400, the last 8 bytes of it (the sprite pointers) are at $7fe8.

The sprites are used in multi-colour mode, which means they each have 4 colours.  3 of the colours are shared by all sprites (background, colour 1 and colour 2) and the last colour is individual to each sprite, set within 8 more VIC registers.  For the sake of simplicty, this post ignores the individual colour, assuming they are all the same.

## File Formats

Since Asi64 extends Racket, it has the [full arsenal of Racket at its disposal](https://racket-lang.org/), including its ridiculous macro system, multiple programming paradigms, extensive libraries and packages.   We can quite easily mix this code in with 6502 assembler to help generate code in any way you would like.

Let's look at one of Spritemate's output formats, for [KickAss](http://theweb.dk/KickAssembler/Main.html#frontpage) (which is a fantasic assmembler, and partly the inspiration for writing Asi64!)

```
// 4 sprites generated with spritemate on 9/14/2018, 9:03:32 PM
// Byte 64 of each sprite contains multicolor (high nibble) & color (low nibble) information

LDA #$04 // sprite multicolor 1
STA $D025
LDA #$06 // sprite multicolor 2
STA $D026


// sprite 1 / multicolor / color: $0e
sprite_1:
.byte $0c,$00,$30,$0f,$00,$30,$0f,$ff
.byte $f0,$03,$7d,$f0,$03,$ff,$c0,$01
.byte $eb,$40,$00,$ff,$00,$01,$3c,$40
.byte $00,$74,$00,$00,$54,$00,$00,$74
.byte $00,$00,$fc,$00,$00,$fc,$00,$03
.byte $ff,$0c,$03,$ff,$0c,$03,$ff,$0c
.byte $0f,$ff,$cc,$0e,$fe,$cc,$3e,$fe
.byte $f0,$3e,$fe,$f0,$3e,$fe,$f0,$8e

// sprite 2 / multicolor / color: $0e
sprite_2:
```


The interesting bits of this file are

* How many sprite frames are in the file (each sprite is a frame of animation)
* The sprite data itself, which of course is just a bunch of bytes
* Additional colour data which we are ignoring for this post.

Since Asi64 is also Racket, we can write a function that will extract the contents of one of these files into a little structure:

```racket
#lang asi64
(struct sprite-data (frame-count data))

(define (extract-sprite filename)
  (let* (;read file as a sequence of lines
         [lines (file->lines filename)]
         ;count the amount of frames by looking at lines that end with :
         [frames (length (filter (λ (s) (string-suffix? s ":")) lines))]
         ; extract the raw data as one big lump
         [data (~>>
                lines
                ; filter to .byte rows 
                (filter (λ (s) (string-prefix? s ".byte")))
                ; clean up text leaving raw hex values
                (map (λ (s) (string-replace s ".byte " "")))
                (map (λ (s) (string-replace s "$" "")))
                (map (λ (s) (string-split s ",")))
                ; flatten into one big list of numbers
                (flatten)
                ; parse hex 
                (map (λ (s) (string->number s 16))))])

    (sprite-data frames data)))
              
```

And now we can quite easily scan the sprites directory for all files (we'll assume there's no more than 8) and pass them through this function to yield a bunch of structures containing the sprite data that can be used to help write the assembly code.

```racket

(define sprites
  (~>>
   (directory-list "..\\sprites" #:build? #t)
   (map path->string)
   (filter (λ (s) (string-suffix? s ".txt")))
   (map extract-sprite)))


```


## 6502

Now we can start to write the actual program. Before we do anything else, we want to dump the raw sprite data that was collected from all the files into memory starting at $2000.   


```racket
(C64{

     ; raw sprite data starts at $2000
     *= $2000
     (write-values
       (~>>
         sprites
         ; extract the "data" field from the struct
         (map sprite-data-data)
         (flatten)))


```

We simply extract out the "data" field from the structs created earlier, and flatten it all into one big list.  The "write-values" here is an asi64 form that simply instructs the assembler to write whatever numbers it is given to the current location.

The next part is a lot more interesting.  We need to setup each of the sprite pointers to point at the first frame of animation for each file that was loaded (we assume in this example there were a maximum of 8 sprites - there are multiplexing techniques you can use to display more)

With the way the VIC is currently setup, the 8 sprite pointers start at $07f8, and the correct index to store in the first one so that it will point at the first frame of data we stored at $2000 is $80.  Then, for each successive set of sprite data, we must increase the offset by the number of frames from the previous set, thus arriving at the first frame of the next set, and store that into the next pointer.

That is a bit of a mouthful, hopefully the code will help to make it clear.  In asi64, everything between curly braces is 6502 assembler which you can nest anywhere:

```racket
      ; start our actual program at $1000
      *= $1000

      ; enable all sprites
      lda @$FF
      sta $d015

      ; turn on multicolour mode for all sprites
      lda @$FF
      sta $d01c 
      lda @$04 ; sprite multicolor 1
      sta $D025
      lda @$06 ; sprite multicolor 2
      sta $D026

      ; set background colour to black
      lda @0
      sta $d021

      (for/fold (; points at $2000
                 [data-index $80] 
                 ; first sprite pointer 
                 [sprite-pointer $07f8]
                 [code (list)])
                ([s sprites])
        (let ([new-code
               {
                ; set the sprite pointer to the first frame
                ; for this animation
                lda @data-index
                sta sprite-pointer            
               }])    
          (values (+ (sprite-data-frame-count s) data-index)
                  (+ 1 sprite-pointer)
                  (cons new-code code))))
```      


The for/fold function builds up a list containing chunks of 6502 code, bringing along another 2 accumulators to track the sprite pointer and frame offset.

Racket's for/fold actually returns all three accumulators as Racket "multiple values".  However, because asi64 only cares about 6502 code, it simply ignores the first two results, but it will then see a list of 6502 code which it will happily assemble. 

Next up is to position the sprites on the screen.  To do this, you have to set some more VIC registers.  $d000 is Sprite 1's X, $d001 is Sprite 1's Y, and so on for each sprite.

We want to line the sprites up together at a fixed Y co-ordinate, but of course without overlapping on the X co-ordinates.  A sprite is 24 pixels wide, so we'll factor that in.

```racket
      ;position sprites
      (for ([x (in-range 8)])
        {
          lda @(+ $20 (* x 24))
          sta (+ $d000 (* x 2))  ; x
          lda @$D0
          sta (+ $d000 (+ 1 (* x 2))) ; y
        })


```

This is very easy since we can use all the bits of racket and the assembler together as one.

## Animation

Each animation has a set number of frames which we have extracted.   What we will need to do is the following:

* Wait for some time so the animations aren't ridiculously fast
* Animate each sprite by changing its sprite pointer to the next frame, or wrapping back to the start

How do we know where each animation currently is?  Well, we know which sprite we are dealing with, and we can read its current pointer value.  With a bit of maths we can work out where its base pointer is, therefore which frame it is currently in, and what the pointer value will be when it is at the last frame. 

```racket
         (define delay $5)
         lda @ delay
         sta $42

:loop
         ; wait for the raster to hit the bottom of the screen
         lda $d012
         cmp @$ff
         bne loop-
         ; decrease our delay by one
         ldx $42
         dex
         ; if it is zero, branch out
         beq change+
         ; otherwise store the new delay value and go back to waiting
         stx $42
         jmp loop-
:change
         ; reset delay
         ldx @ delay
         stx $42

         (for/fold ([base-offset $80]
                    [index 0]
                    [code (list)])                   
                   ([ s sprites])
           (let ([new-code
                  {
                   ;load sprite pointer value
                   ldx (+ $07f8 index)
                   ;is it on the final frame?    
                   cpx @(+ base-offset (- (sprite-data-frame-count s) 1))
                   bne skip+
                   ;reset to its first frame
                   ldx @base-offset
                   stx (+ $07f8 index)
                   jmp done+
                   :skip
                   ; move to next frame
                   inx
                   stx (+ $07f8 index)
                   :done                   
                   }])
             (values (+ base-offset (sprite-data-frame-count s))
                     (+ 1 index)
                     (cons new-code code))))
         
         jmp loop-
```


Again here we are using our old friend for/fold to generate the code for us.  Notice in this example, the generated code uses local labels of :skip and :done, it is able to do this since asi64 has a (fairly common) feature of being able to have many labels named the same thing.  You tell it to jump to the nearest one it finds either in front or behid the current location by suffixing with + or - respectively.



### Conclusion

The full porgram has a few more features, but hopefully has exemplified the idea of mixing racket and 6502 together to help generating code.  It is now very easy to dump some new files into the directory, regardless of how many frames they have, compile and run the program to see them animated in the emulator (or on the real machine!)

If you want to know more about how the sprites work on the C64, check out [codebase](http:///www.codebase64.org) which is chock full of great information. 









