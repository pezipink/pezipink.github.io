    Title: Fun with a toy robotic arm!
    Date: 2014-02-01T01:25:00
    Tags: fsharp

<p>This post is intended to show how easy it is to use the F# programming language in order to explore new libraries and get stuff going quickly. It also shows the usage of various great F# features such as Record Types, Discriminated Unions, Computation Expressions and Async workflows, whilst also using a bit of mutable state, integrating with various other 3rd party .NET libraries (including use of an XBOX pad and even a Kinect!), and even some low level bit shifting and masking stuff.</p>
<p>F# is definitely NOT just for number crunching programs!</p>
<p>So, the step-son got a toy robotic arm for Christmas (no, not from me), which he has largely ignored. Partly this was due to the lack of batteries in it, which we addressed recently. However, he still wasn't really interested in it, so I figured I would have a play with it instead (a couple of weeks ago). It was either that or write documentation for the <a href="https://github.com/fsprojects/SQLProvider">SQLProvider</a>.</p>
<p><a href="http://localhost:3000/img/old/b696_edge_robotic_arm_kit.jpg"><img style="display: inline; border: 0px;" title="b696_edge_robotic_arm_kit" src="http://localhost:3000/img/old/b696_edge_robotic_arm_kit_thumb.jpg" alt="b696_edge_robotic_arm_kit" width="244" height="187" border="0" /></a></p>
<!-- more -->
<p>This is it. It&rsquo;s just a toy one &ndash; I had a real one a few years back but I blew it up after making a rather silly mistake (electricity, how I love and hate thee.) The one in the picture has a controller &ndash; this one doesn&rsquo;t, it has a USB interface instead. It comes with some rubbish software which lets you control the various joints and gripper with the mouse, and a way to &ldquo;program&rdquo; it, which basically involves recording sequences and pauses. It doesn&rsquo;t have any sensors, and uses motors not servos so you are not able to accurately position it at all.</p>
<p>However, I figured it would still be fun to mess around with. Luckily, <a href="http://notbrainsurgery.livejournal.com/38622.html">someone had already reversed engineered the USB communications</a>, which saved me quite a bit of time. I grabbed <a href="http://sourceforge.net/projects/libusbdotnet/">libusb</a>, added the device to it, then used FSI to try and locate it.</p>
```fsharp
#r@"F:\utils\LibUsbDotNet\LibUsbDotNet.dll" 
open LibUsbDotNet 
let arm = LibUsbDotNet.UsbDevice.AllDevices.[0] 
let usb = LibUsbDotNet.UsbDevice.OpenUsbDevice(Main.UsbDeviceFinder(4711))
```

<p>Well, that was almost too easy. Found it straight away! Now to work out how to send the packets of data it is expecting. Evidently it expects three bytes, the bits of the first control the state of the various motors in the arm and gripper, and the second controls the base and the third the light. After quite a bit of messing about with the somewhat confusing libusb library,I managed to get something to work - switching the light on and off seemed the least destructive as I wouldn&rsquo;t be able to quickly stop the motors at this point!</p>
```fsharp
let mutable packet = Main.UsbSetupPacket() 
packet.RequestType <- byte 0x40 
packet.Request <- byte 6 
packet.Value <- int16 0x100

//on! 
usb.ControlTransfer(&packet,[|0b0uy;0b0uy;0b1uy|],3)

//off! 
usb.ControlTransfer(&packet,[|0b0uy;0b0uy;0b0uy|],3)
```

<p>woohoo, now I know it will work, time to model the various actions of the arm using F#&rsquo;s awesome discriminated unions!</p>
```fsharp
type Direction = 
  | Up 
  | Down 
  | Stop

type GripDirection = 
  | Open 
  | Close 
  | GripStop

type Rotation = 
  | Clockwise 
  | AntiClockwise 
  | RotationStop

type LightAction = 
  | On 
  | Off

type Action = 
  | Wrist of Direction 
  | Elbow of Direction 
  | Shoulder of Direction 
  | Grip of GripDirection 
  | Base of Rotation 
  | Light of LightAction 
 with 
   member x.ToCommand() = 
     match x with 
     // FOR BYTE 1 (ARM) 
     // STOP patterns are designed to be cleared to blank out existing 1's 
     | Grip(GripStop) -> 0b00000011uy 
     | Wrist(Stop) -> 0b00001100uy 
     | Elbow(Stop) -> 0b00110000uy 
     | Shoulder(Stop) -> 0b11000000uy 
     | Grip(Close) -> 0b00000001uy 
     | Grip(Open) -> 0b00000010uy 
     | Wrist(Up) -> 0b00000100uy 
     | Wrist(Down) -> 0b00001000uy 
     | Elbow(Up) -> 0b00010000uy 
     | Elbow(Down) -> 0b00100000uy 
     | Shoulder(Up) -> 0b01000000uy 
     | Shoulder(Down) -> 0b10000000uy 
     // BYTE 2 (BASE) 
     | Base(RotationStop) -> 0b00000011uy // Clear this 
     | Base(AntiClockwise) -> 0b00000001uy 
     | Base(Clockwise) -> 0b00000010uy 
     // BYTE 3 (LIGHT) 
     | Light(Off) -> 0b00000001uy // Clear this 
     | Light(On) -> 0b00000001uy
```

<p>As you can see I mapped the bit patterns for the various actions and including some special ones that can be used to clear the relevant bits and effectively stop that function. For example, the gripper can be opening, closing, or doing nothing. In order to make sure its doing nothing, you have to make sure both the relevant bits are 0 as either might be set.</p>
<p>The arm is fully capable of using any combination of all of its functions at the same time, so to facilitate that I wrote this small function that given a list of the above actions, will create and execute a command by masking the bit patterns together.</p>
```fsharp
let executeActions actions state = 
  (state,actions) 
  ||> List.fold(fun [|a;b;c|] action -> 
        match action with 
        | Wrist(Stop) as x -> [|a&amp;&amp;&amp;(~~~x.ToCommand());b;c|] 
        | Elbow(Stop) as x -> [|a&amp;&amp;&amp;(~~~x.ToCommand());b;c|] 
        | Shoulder(Stop) as x -> [|a&amp;&amp;&amp;(~~~x.ToCommand());b;c|] 
        | Grip(GripStop) as x -> [|a&amp;&amp;&amp;(~~~x.ToCommand());b;c|] 
        | Base(RotationStop) as x -> [|a;b&amp;&amp;&amp;(~~~x.ToCommand());c|] 
        | Base(_) as x -> [|a;b ||| x.ToCommand();c|] 
        | Light(Off) as x -> [|a;b;c&amp;&amp;&amp;(~~~x.ToCommand())|] 
        | Light(On) as x -> [|a;b;c ||| x.ToCommand()|] 
        | x -> [|a ||| x.ToCommand();b;c|]) 
  |> fun cmd -> 
       let _ = usb.ControlTransfer(&amp;packet,cmd,3) |> ignore 
       cmd
```

<p>Well, this was great! I created a load of mini functions in FSI and I control the arm by the power of F#. I quickly decided that greater things could be achieved though, next thing up was to get it to work with my XBOX pad! Once again, this was really very simple to do. I looked for a library, found <a href="http://brandonpotter.wordpress.com/2009/12/28/xbox-controller-in-net-3-5-with-3-lines-of-code/">this</a>, downloaded it, referenced it in my FSI project and I was reading data within about 1 minute after the download finished!</p>
```fsharp
#r@"X9Tech.XBox.Input.dll" 
open X9Tech.XBox.Input 
let xcm = XBoxControllerManager() 
let controller = xcm.GetConnectedControllers().[0]
```

<p>I then created a F# record type to hold the pad state, and a function to read it</p>
```fsharp
type ControllerState = 
  { LShoulder:bool; RShoulder:bool; LStickUp:bool; LStickDown:bool; RStickUp:bool; RStickDown:bool 
    DPadUp: bool; DPadDown: bool; StartPressed: bool; LTriggerPressed:bool; RTriggerPressed:bool }

let getState() = 
  { LShoulder = controller.ButtonShoulderLeftPressed 
    RShoulder = controller.ButtonShoulderRightPressed 
    LStickUp = controller.ThumbLeftY > 75.0 
    LStickDown = controller.ThumbLeftY < 35.0 
    RStickUp = controller.ThumbRightY > 75.0 
    RStickDown = controller.ThumbRightY < 35.0 
    DPadUp = controller.ButtonUpPressed 
    DPadDown = controller.ButtonDownPressed 
    StartPressed = controller.ButtonStartPressed 
    LTriggerPressed = controller.TriggerLeftPosition > 25.0 
    RTriggerPressed = controller.TriggerRightPosition > 25.0 }
```

<p>SUPER! at this stage I hit a bit of a problem. What I really need is to understand when something on the pad changes so that I can react to it. An event mechanism of some description. For that I would need to remember the old state, compare it to a new state then raise an event, call some passed-in function that is supposed to do something with the information, or simply return a list of stuff that has changed. Once again, F# discriminated unions are awesome for this as I can very easily represent all the possible events with one type</p>
```fsharp
type ControllerEvent = 
  | LShoulder of bool 
  | RShoulder of bool 
  | LTrigger of bool 
  | RTrigger of bool 
  | LStickUp of bool 
  | LStickDown of bool 
  | RStickUp of bool 
  | RStickDown of bool 
  | Start of bool 
  | PadUp of bool 
  | PadDown of bool
```

<p>I use a little trick to create the &ldquo;events&rdquo; .. first a tiny function that accepts a tuple of bool * ControllerEvent, if the bool true it returns Some(event), else None. This way I can create my &ldquo;events&rdquo; in a list, then use List.choose over them to extract all the things that have changed</p>
```fsharp
let getEvents oldState = 
  let f (b,r) = if b then Some r else None 
  let newState = getState() 
  let events = 
  [ 
    f(newState.LShoulder <> oldState.LShoulder, LShoulder newState.LShoulder) 
    f(newState.RShoulder <> oldState.RShoulder, RShoulder newState.RShoulder) 
    f(newState.LTriggerPressed <> oldState.LTriggerPressed, LTrigger newState.LTriggerPressed) 
    f(newState.RTriggerPressed <> oldState.RTriggerPressed, RTrigger newState.RTriggerPressed) 
    f(newState.LStickDown <> oldState.LStickDown, LStickDown newState.LStickDown) 
    f(newState.LStickUp <> oldState.LStickUp, LStickUp newState.LStickUp) 
    f(newState.RStickDown <> oldState.RStickDown, RStickDown newState.RStickDown) 
    f(newState.RStickUp <> oldState.RStickUp, RStickUp newState.RStickUp) 
    f(newState.DPadDown <> oldState.DPadDown, PadDown newState.DPadDown) 
    f(newState.DPadUp <> oldState.DPadUp, PadUp newState.DPadUp) 
    f(newState.StartPressed <> oldState.StartPressed, Start newState.StartPressed) 
  ] |> List.choose id 
  (newState,events)
```

<p>Piece of cake right! In order to use this, I will have to poll the pad at certain intervals &ndash; uh-oh &ndash; this is going to require threaded code to work properly without blocking the FSI process! No matter&hellip; F# Async to the rescue! I simply create a function <em>pollPad</em> that implements a recursive async function with a small delay in it. Each time the function calls itself, it passes the old state of the pad through. There is a small delay, and then a new state is generated, any changes that have happened are executed. Really it should use the function above to compose a single three byte command rather then sending each one individually, but who cares, it works :)</p>
```fsharp
let pollPad() = 
 let c = getState() 
 let s = [|0b0uy;0b0uy;0b0uy|] // initial state, everything off 
 let rec poll controllerState robotState = async { 
 do! Async.Sleep 100 // a wee delay 
 let (newState,events) = getEvents controllerState 
 let data = 
   (robotState,events) 
   ||> List.fold( fun acc a -> 
         match a with 
         | LShoulder (true) -> executeActions [Grip(Close) ] acc 
         | RShoulder (true) -> executeActions [Grip(Open) ] acc 
         | LTrigger (true) -> executeActions [Base(Clockwise) ] acc 
         | RTrigger (true) -> executeActions [Base(AntiClockwise) ] acc 
         | LStickUp (true) -> executeActions [Shoulder(Up) ] acc 
         | LStickDown (true) -> executeActions [Shoulder(Down) ] acc 
         | RStickUp (true) -> executeActions [Wrist(Up) ] acc 
         | RStickDown (true) -> executeActions [Wrist(Down) ] acc 
         | Start (true) -> executeActions [Light(On) ] acc 
         | PadUp (true) -> executeActions [Elbow(Up) ] acc 
         | PadDown (true) -> executeActions [Elbow(Down) ] acc 
         | LShoulder (false) 
         | RShoulder (false) -> executeActions [Grip(GripStop) ] acc 
         | LTrigger (false) 
         | RTrigger (false) -> executeActions [Base(RotationStop) ] acc 
         | LStickUp (false) 
         | LStickDown (false) -> executeActions [Shoulder(Stop) ] acc 
         | RStickUp (false) 
         | RStickDown (false) -> executeActions [Wrist(Stop) ] acc 
         | Start (false) -> executeActions [Light(Off) ] acc 
         | PadUp (false) 
         | PadDown (false) -> executeActions [Elbow(Stop) ] acc) 
 return! poll newState data } 
 poll c s

pollPad() |> Async.Start
```

<p>Awesome! At this stage I got all the kids playing with it via the pad, and we managed to pickup and deposit various toys, and even my nose :)</p>
<p>Well, cool as it was at this stage (not to mention the pad being very handy for easily resetting it to some good state) I was not to be deterred from my original aim which was to be able to program it in some way. So I figured, what would be really cool is if i could have a mini language that could control it, and chunks of control could be combined together to form bigger programs. For this I could use the very awesome F# computation expression.</p>
```fsharp
type ArmLanguage = 
  | Command of Action list 
  | Sleep of int

type RobotBuilder() = 
  let mutable state = [|0b0uy;0b0uy;0b0uy|] 
  member this.Delay(f) = f() 
  member this.Bind(x, f) = 
    match x with 
    | Sleep delay -> wait delay; f x 
    | Command actions -> state <- executeActions actions state; f x 
  member this.Return(x) = x

let robot = RobotBuilder()
```

<p>In just a few short lines I am able to define a new language construct that can be used to control the robot. It uses mutable state to remember the current state of the arm, this is so successive commands can be properly masked together and not overwrite any existing state with a load of zeroes. It can now be used like so:</p>
```fsharp
let closeGrip() = robot { 
  let! _ = Command[(Grip(Close))] 
  let! _ = Sleep 1700 
  let! x = Command([Grip(GripStop)]) 
  return x 
}

let openGrip() = robot { 
  let! _ = Command[(Grip(Open))] 
  `let! _ = Sleep 1700 
  `let! x = Command([Grip(GripStop)]) 
  `return x 
}`
```

<p>But what&rsquo;s better than that is that these can now be combined together to form more complex behaviours !</p>
```fsharp
let flickVs() = robot { 
  let! _ = openGrip() 
  let! _ = Command([Wrist(Up)]) 
  let! _ = Sleep 1500 
  let! _ = Command([Elbow(Up);Shoulder(Up)]) 
  let! _ = Sleep 1500 
  let! _ = Command([Wrist(Stop);Wrist(Down);Elbow(Stop);Shoulder(Stop)]) 
  // stop at peak 
  let! _ = Command([Wrist(Stop);Wrist(Down)]) 
  let! _ = Sleep 1500 
  let! _ = Command([Wrist(Stop);Wrist(Up)]) 
  let! _ = Sleep 1500 
  let! _ = Command([Wrist(Stop);Wrist(Down)]) 
  let! _ = Sleep 1500 
  let! _ = Command([Wrist(Stop);Wrist(Up)]) 
  let! _ = Sleep 1500 
  let! _ = Command([Elbow(Down);Shoulder(Down)]) 
  let! _ = Sleep 1500 
  let! _ = Command([Wrist(Stop);Elbow(Stop);Shoulder(Stop)]) 
  let! x = closeGrip() 
  return x }
```

<p>Notice here that I have called openGrip() and closeGrip() within another robot { } expression. You could nest these down many levels if you wished. This particular program repeatedly flicks the V sign at my girlfriend who is sitting across the room. Hilarious I&rsquo;m sure!</p>
<p>Using my new found robot arm language, I thought it would be a good idea to try and integrate something with my Kinect. I thought I could have the arm go all the way up to its highest point when my arm (elbow, actually) is high in the air, and down to its lowest point when my elbow is at seating level. Because the arm has no sensors and doesn&rsquo;t use servos, this was never going to work properly as all I can do is estimate how far the arm will move in a given amount of time and try to track where it should be, which is rubbish at best. However, I timed it a few times, did a bunch of maths to work out how to synchronise the sensor readings from the Kinect and the time resolution, plus a bunch of scaling to normalize the numbers coming out of the Kinect into something I can use.</p>
<p>I&rsquo;m not going to show all the code here, but I quickly whipped in the RX framework so I could use Observable.Sample to bring the events from the Kinect under control with my calculated timing, and when the elbow joint is above or below my current perceived position of the arm it would issue a command to move up or down.</p>
```fsharp
  // right so we know this event will fire every 500ms, so if we need to move up or down 
  // at least 500ms worth (which is 1/12th of the total distance) then do so, otherwise do nothing 
  if abs(kElbowPos - e) >= minInterval then 
    if e < kElbowPos then //down? 
      robot { 
        let! x = Command([Shoulder(Down)]) 
        let! x = Sleep (int resolution) 
        let! x = Command([Shoulder(Stop)]) 
        return x } |> ignore 
      printfn "down" 
      kElbowPos <- kElbowPos - minInterval 
      aShoulderPos <- aShoulderPos - resolution 
    else // up? 
      robot { 
      let! x = Command([Shoulder(Up)]) 
      let! x = Sleep (int resolution) 
      let! x = Command([Shoulder(Stop)]) 
      return x } |> ignore 
    printfn "up" 
    kElbowPos <- kElbowPos + minInterval 
    aShoulderPos <- aShoulderPos + resolution 
    kElbowPos <- e 
    printfn "new kinect postion %A" kElbowPos 
    printfn "new arm postion %A" aShoulderPos
```

<p>Ok so it wasn&rsquo;t a brilliant first attempt, but it sort of worked a bit and the children were mighty impressed to see the robot arm attempt to follow their own arm going up and down :)</p>
<p>Anyways, enough of this! Once again F# continues to impress with its awesome ability to get stuff done quickly, and well. You can easily integrate to a whole bunch of different libraries, model the domain quickly and effectively, and provide all the constructs you could possibly need to achieve any modern programming task, not to mention having a load of fun in the process - and all of this is just a few hours! F# for president!</p>