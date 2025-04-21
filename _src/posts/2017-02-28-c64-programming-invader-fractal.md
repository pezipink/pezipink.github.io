    Title: C64 Programming - Invader Fractal
    Date: 2017-02-28T17:31:32
    Tags: C64,6502

I have recently been getting into programming the Commodore 64.  It's lots of fun to work in such a restricted environment, where you have to use various hardware tricks to achieve your goals. Every machine cycle and byte of memory counts!

I have not programmed in 6502 assembler or the C64 before.  I am using the popular C64 emulator [WinVice](http://vice-emu.sourceforge.net/) and an assembler called [DASM](http://dasm-dillon.sourceforge.net/). Having messed around a bit and learnt the basics of the instruction set and the hardware/memory layout, I programmed a couple of effects such as the basic text scroller.  I have an idea in mind for my first demo, and part of it revolves around the [*invader fractal*](http://levitated.net/daily/levInvaderFractal.html).  I have implemented this in various langauages (F# and D being the most recent) and figured it would be a nice fit for the C64.

### Invaders

The invader fractal is a very simpe idea, based on the observation that the classic space invaders are symmetrical.  Given a 5x5 grid, we can observe that the middle column is static, whilst columns 4 and 5 are columns 1 and 2 flipped around:

![](../../../../../img/invaders/1.png)

This means we can store the information for each row in just 3 bits, multiplied for each row gives us 15 bits to encode an entire invader.  15 bits gives a total of 2^15=32,768 unique invaders.  "Real" space invaders are a little bit bigger than 5x5, but we will stick with their smaller cousins.

<!-- more -->

### C64 Limitations
We will be using the most basic video mode of the C64, which is text mode.  In this mode, a character is represented by an 8x8 bitmap.  The video memory for each 8x8 location of the screen stores a byte value that is an offset into the character set determining which character to draw.

For example, the video memory is mapped to adresses $0400 - $07F7.  If the value of $0400 is set to $01, then the screen will render character 1 from the character set (which happens to be the letter A) into the top left of the screen.  

$ denotes a hexadecimal number, and % binary.  The # prefix indicates a literal value rather than a memory location.

It is not my intention to explain every op-code, most should make sense from context and the comments, but you can view a fulll list [here](http://6502.org/tutorials/6502opcodes.html)

```ca65
lda #$01	; load 1 into the accumulator (A) 
sta $0400	; store A into the first byte of video memory
```

![](../../../../../img/invaders/2.png)

A nice thing about these characters sets is that you can define them yourself, and then you can change them in realtime to affect the screen.  Therefore, what I would like to do is work out how to take a 15 bit encoded invader and decode it into a position in the character set.

The C64 only has 3 registers (A, X and Y) and they are all 8 bit.  This means we cannot store the entire encoded invader in a register for processing, instead some of it will have sit elsewhere.

### The first bits

Let's forget the problem of 15 bits for now and concentrate on how to decode a single row of three bits into the 5 bit destination.  Since a character row is 8 bits, we will consider the three most significant bits to not be used.  We further consider that bit 3 is the centre pixel, while bits 1 and 2 form the right hand side of the invader, to be mirrored onto the left hand side.

Here are some examples (using an amazing new gfx tech):

![](../../../../../img/invaders/3.png)

Since we will have to be moving and masking bits around, we will need working areas for the finished product.  We could use the registers, an area in memory or the stack for this purpose.

let us assume we have the value %00000101 at memory location $42 for processing.

```ca65
		lda $42     ; load the encoded invader into A
		sta $43     ; copy A into $43 - $43 is now %101, the right side of the invader
		and #%11    ; mask out the third (centre) bit of A, leaving %01
```


$42 and $43 are a special memory addresses I have selected.  Because they are 8-bit adrresses, they are known as *zero-page* addresses and have some special properties - they can be accessed much faster from the CPU - almost as fast as the registers themselves.

At this stage we have built the right side of the invader, dumped the centre bit and left ourselves with the two bits that will need mirroring and shifting into place on the left hand side of the invader.  There are various ways to approach this.  Given we only ever need to mirror 2 bits, and there are only 4 possible combinations, for the sake of 4 bytes of memory we can easily encode this into a lookup table.

```ca65
		tax		; copy the value into the X register
		lda lookup,x 	; read the value from the table offset by X into A (%10)
		asl 		; shift the result left 3 bits (%10000)
		asl
		asl
		ora $43		; OR the resulting bits with our work area (%00010101)
		sta $43		; store the result

;store the lookup table after the program data
lookup:	DC %00,%10,%01,%11	
```


The 6502 has several addressing modes, the one shown here is *absolute indexed*.  This means it can take any absolute address (16 bits) and then it will add the contents of the X or Y register to it and return the byte from that location.

In this example I have told the assembler to label a location of memory *lookup* and then told it to store four consequtive bytes there.

Thus, the `lda lookup,x` instruction will return the data from the table depending on the value of the X register.  Since we know it is only two bits, it has the following effect:

![](../../../../../img/invaders/4.png)

We then take the mirrored value and shift it left 3 bits, so that %10 becomes %10000.  Finally we take our result, OR it together with the stored right hand side and store the result of %10101.


Success!  this is the first row of an invader complete.

### The rest of it..

Now we have an algorithm to decode an invader row to a characer row, it should be easy to repeat process for all 5 rows, right?

First, there are going to be a couple of problems to solve.  To start with, the row has been built at $43, but we don't actually want it there. Where we really want it is the memory location where the character set starts.  Let's say this is $2000.  Now, using the zero-page is very fast, but since we only read/write the intermediate invader a couple of times, we might as well just place it where it needs to end up.

A character set can hold 256 characters, formed of 8 bytes each (as explained earlier) for a total of 2kb.  If we are going to generate 256 unique invaders, the *absoulte indexed* addressing mode is not going to work out too well for us.  Since the X register can only contain a single byte, we can offset a known 16 bit address by at most 256 bytes, which will only stretch out to 32 invaders.  Clearly some other method will be required.  

There are a couple of solutions to this problem.  The first is more fun but easy to mess up which is *self modifiying code*.

Let's take the instruction `lda $2000,x`.  The assembler of course simply turns this into some bytes - one that represents the opcode with the addressing mode, and two bytes that represent the address of $2000.  Since we can write to whatever memory we like however we want to, there is nothing stopping us simply modifying the assembled address that follows the opcode.  I will leave this for another post. 

The 6502 provides another addressing mode to perform a similar function, which is called *zero paged indirect indexed*.  As the name indciates, this can only be used with the zero page.  It looks like this:

```ca65
	lda #$20	;target address most significant byte
	sta $41		
	lda #$00	;least signficant byte goes first
	sta $40
	ldy #$F 	;some index value
	lda #$FF	;some value we wish to store
	sta ($40),y	;stores $FF into $200F
```


First, we place the target address across two bytes at $40 in the zero page. Notice the address is stored backwards - that is - least significant byte first - $0020.  This is because the design of 6502 means it is quicker to load addresses this way.  If you look at the assembled instruction of `lda $2000` you will see the address is backwards there too.

Next we just load some values into Y and A, and the final instruction, denoted by the parens, causes the CPU to construct a 16-bit address from $40 and $41, add the contents of Y onto it, and then finally write the value in the accumulator to this new address.  Pretty cool!  This means we can store a 16-bit address and change it however we like from the zero page, AND have an index offset as well! Note - this addressing mode can ONLY be used with Y as the index register!

The other problem that needs solving is the fact the invader is 15 bits but we only have 8 bits.  Clearly, once we have decoded a row and stored it, we will want to move on to the next 3 bits and repeat this process until all 5 rows are complete.  The solution to this is to shift three bits out of the remaining byte and into the accumulator.  Let's say we have the first 8 bits in the accumulator, and the remaining 7 bits are stored in a byte at location $39 (was supposed to be $3F but I had already drawn the table wrong :) ).

![](../../../../../img/invaders/5.png)

```ca65
	lsr $39		;shift bits one to the right
	ror 		;rotate the accumuator right
	lsr $39
	ror
	lsr $39
	ror
```


`lsr` (logical shift right) shifts the byte in question one to the right.	If the bit that "falls off" the end is set, then the processor's carry flag will be set.  `ror` (rotate right) shifts the byte in question (in this case the accumulator since no specfic addressing mode is specified) one to the right, and if the carry bit is currently set, then 1 will also appear at the most signficant bit.  In this way we are able to rotate bits out of one number and into another, giving us what we need.

With all being said and done, we can write a new routine that will decode an entire invader into a character, and advance the memory pointer to the next character memory location. 

```ca65
setup           lda #$20	; store the target charset memory location $2000 at $40
		sta $41
		lda #$00
		sta $40
		lda #$69	; some random 7 bits i made up for the rest of the invader
		sta $39
		lda #$D9	; a random 8 bits for the first half the the invader
		ldy #0		; make sure Y is clear and ready
loop 	        pha		; preserve the current state of the invader onto the stack
 		and #%111	; working with the first 3 bits only
		sta ($40),y	; store right side of the row
		and #%11	; mask out centre bit
		tax
		lda lookup,x    ; load mirrored bits from lookuptable
		asl
		asl
		asl
		ora ($40),y		
		sta ($40),y	; row is now complete!
		iny 		; increase Y by one
		cpy #5		; test Y against 5
		beq done	; branch to done if Y is 5		
		pla 		; restore the invader from stack
		lsr $39 	; rotate the next 3 bits in
		ror
		lsr $39
		ror
		lsr $39
		ror
		jmp loop	; loop
done 	pla 		; restore stack
		clc         ; add 8 to the target memory location
		lda #8		
		adc $40
		sta $40
		bcc skip	; if the carry bit is set, we overflowed from $FF to $00
		inc $41		; which means we increase the most significant bit as well
skip	...                     ; rest of program 


lookup	DC %00,%10,%01,%11	
```


Since the Y register is being used as the indirect index, and X is used for indexing the mirror lookup table, we need a new place to store the current invader byte.  Rather than have to copy stuff around, we simply push it onto the stack `pha` and then restrore it `pla` when ready to advance the next 3 bits.

The other part here worth mentioning is the slightly odd looking sequence of

```ca65
    clc
    lda #8
    adc $40
    sta $40
    bcc skip
    inc $41
skip ...
```


The 6502 can obviously only do 8 bit addition, and it does not have an instruction to add something ignoring the carry bit.  The first instruction `clc` simply clears the carry bit so we don't get an unexpected result when we do the addition.  The next three instructions add 8 to memory address $40.  If this caused $40 to go over $FF and wrap around, then the carry bit will be set.  The `bcc` instruction will branch to the given label when the carry bit is clear - so in the cases where the lower address byte of $40 overflowed, we also add one to the higher address byte at $41.  This is a shortcut way to add an 8 bit number to a 16 bit number.


![](../../../../../img/invaders/6.png)

My totally random numbers produced quite a nice invader! The routine above can definitely be further optimised.  For a start - once the extra 7 bits are shifted in, the `lsr $49` instructions are wasted.  We could drop the loop and just unroll the code to prevent this and make it faster withouht the `jmp` instructions, at the cost of more program space.

We still need a way to randomise the 256 invaders, but I will save that for another time!

(and no, this version has nothing at all to do with fractals!)
