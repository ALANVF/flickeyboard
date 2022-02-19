#nowarn "1189"
#nowarn "40"
#nowarn "3220"

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Media
open System.Windows.Threading
open System.Windows.Input
open System.Threading.Tasks
open System.Diagnostics
open WindowsInput
open WindowsInput.Native
open type WindowsInput.Native.VirtualKeyCode // DO NOT REMOVE

type Horiz = HorizontalAlignment
type Vertical = VerticalAlignment

type UIElement with
    member this.ZIndex
        with inline get() = Panel.GetZIndex(this)
        and inline set(index) = Panel.SetZIndex(this, index)

type Panel with
    member this.Children
        with inline set(children: seq<UIElement>) =
            for child in children do
                this.Children.Add(child) |> ignore

type Panel with
    member this.Children
        with inline set(children: seq<FrameworkElement>) =
            for child in children do
                this.Children.Add(child) |> ignore

let inline rgb r g b = Color.FromRgb(byte r, byte g, byte b)

let inline thickness (left: int) (top: int) (right: int) (bottom: int) = Thickness(float left, float top, float right, float bottom)

let isnormal = Double.IsNormal

// wtf
let inline thing< ^a, ^d when ^a :> EventArgs and ^d :> Delegate and ^d : delegate< ^a, unit>> (b: obj -> ^a -> unit): ^d =
    if typedefof< ^d> = typedefof<EventHandler> then
        (EventHandler (b :> obj :?> obj -> EventArgs -> unit)) :> obj :?> ^d
    elif typedefof< ^d> = typedefof<SizeChangedEventHandler> then
        (SizeChangedEventHandler (b :> obj :?> obj -> SizeChangedEventArgs -> unit)) :> obj :?> ^d
    else
        (EventHandler< ^a> b) :> obj :?> ^d

let inline (+=)< ^a, ^t when ^a :> EventArgs and ^t :> Delegate and ^t : delegate< ^a, unit>> (a: IEvent< ^t, ^a>) (b: obj -> ^a -> unit): unit =
    a.AddHandler(thing< ^a, ^t> b)


type Key =
    { primary: char
      secondary: char
      code: VirtualKeyCode }

type State =
    { mutable shift: bool
      mutable ctrl: bool
      mutable alt: bool }

type ActiveKey =
    { startTouch: TouchPoint
      mutable flicked: bool }

let buildWindow (win: Window) =
    // Constants
    
    let default_fill = SolidColorBrush(rgb 223 223 223)
    let pressed_fill = SolidColorBrush(rgb 123 123 223)

    let default_opacity = 0.3
    let end_opacity = 1.0

    let input = InputSimulator()
    let keyboard = input.Keyboard


    // Other global vars

    let mutable prevProc: Process option = None
    
    let state =
        { shift = false
          ctrl = false
          alt = false }
    

    // Pressing keys

    let getInputFocus () =
        match prevProc with
        | Some proc when proc.HasExited ->
            prevProc <- None
            false
        | Some proc ->
            Win32Api.SetForegroundWindow(proc.MainWindowHandle) |> ignore
            true
        | None ->
            false

    let keyPress (code: VirtualKeyCode) =
        if getInputFocus() then
            ignore <|
            match state with
            | {shift=false; ctrl=false; alt=false} -> keyboard.KeyPress(code)
            | _ -> keyboard.ModifiedKeyStroke([
                       if state.shift then SHIFT
                       if state.ctrl then CONTROL
                       if state.alt then MENU
                   ], code)

    
    // Other key stuff

    let makeVeryBasicKey x y width textWidth height name =
        StackPanel(
            Width = float width,
            Height = float height,
            Margin = thickness x (y + (50 - height)) 0 0,
            HorizontalAlignment = Horiz.Left,
            VerticalAlignment = Vertical.Top,
            Orientation = Orientation.Vertical,
            Background = default_fill,
            Children = [
                Label(
                    Content = name,
                    Width = float (textWidth+3),
                    Margin = thickness 0 (12 - ((50 - height) / 2)) 0 0,
                    RenderTransformOrigin = Point(0.5, 0.5),
                    HorizontalAlignment = Horiz.Center,
                    HorizontalContentAlignment = Horiz.Center,
                    FontSize = 14.,
                    ZIndex = 5
                )
            ]
        )

    let makeBasicKey x y width textWidth name =
        makeVeryBasicKey x y width textWidth 50 name
    
    let afterKey (panel: StackPanel) =
        int panel.Margin.Left + int panel.Width + 10

    let beforeKey (panel: StackPanel) =
        int panel.Margin.Right - int panel.Width - 10
    

    // Content

    let mainGrid =
        Grid(
            RenderTransformOrigin = Point(0.0, 0.0),
            MinWidth = win.Width - 20.,
            MinHeight = win.Height - 20.,
            Margin = thickness 10 0 0 0,
            HorizontalAlignment = Horiz.Stretch,
            VerticalAlignment = Vertical.Top
        )

    let debugLabel =
        Label(
            Content = "Label",
            Width = 413.,
            Height = 24.,
            Margin = thickness 140 10 0 0,
            HorizontalAlignment = Horiz.Left,
            VerticalAlignment = Vertical.Top
        )

    let abs_bottom = 280 + 60

    let fromBottom times =
        abs_bottom - (60 * times)

    let key_Ctrl = makeBasicKey 10 abs_bottom 45 35 "Ctrl"

    let key_Alt = makeBasicKey (afterKey key_Ctrl) abs_bottom 45 35 "Alt"

    let key_Space =
        StackPanel(
            Width = 300.,
            Height = 50.,
            Margin = thickness 0 abs_bottom 0 0,
            HorizontalAlignment = Horiz.Center,
            VerticalAlignment = Vertical.Top,
            Background = default_fill
        )
    
    let key_Shift = makeBasicKey 10 (fromBottom 1) 110 35 "Shift"

    let makeKeyOf x y width width1 width2 x2 name1 name2 =
        let label1 =
            Label(
                Content = name1,
                RenderTransformOrigin = Point(0.5, 0.5),
                Width = float (width1+3),
                Margin = thickness 0 12 0 0,
                ZIndex = 5,
                HorizontalContentAlignment = Horiz.Center,
                FontSize = 14.
            )
        
        let label2 =
            Label(
                Content = name2,
                RenderTransformOrigin = Point(0.5, 0.5),
                Width = float (width2+3),
                Height = 30.,
                Margin = thickness x2 -55 0 0,
                ZIndex = 5,
                HorizontalContentAlignment = Horiz.Center,
                VerticalContentAlignment = Vertical.Top,
                FontSize = 14.
            )

        let panel =
            StackPanel(
                Width = float width,
                Height = 50.,
                Margin = thickness x y 0 0,
                HorizontalAlignment = Horiz.Left,
                VerticalAlignment = Vertical.Top,
                Orientation = Orientation.Vertical,
                Background = default_fill,
                Children = [
                    label1
                    label2
                ]
            )

        panel, label1, label2

    let makeKey x y width key =
        let panel, label1, label2 = makeKeyOf x y width 20 25 (width - 20) key.primary key.secondary
        panel, label1, label2, key

    let makeRow x y keys =
        let mutable x = x in seq {
            for key in keys do
                let key' = makeKey x y 50 key
                x <- x + 60
                key'
        }


    let key_Tab, key_Tab_label1, key_Tab_label2 = makeKeyOf 10 (fromBottom 3) 65 50 50 20 "Tab" "Detab"

    let keys = [|
        yield! makeRow (afterKey key_Shift) (fromBottom 1) [
            { primary = 'z'; secondary = 'Z'; code = VK_Z }
            { primary = 'x'; secondary = 'X'; code = VK_X }
            { primary = 'c'; secondary = 'C'; code = VK_C }
            { primary = 'v'; secondary = 'V'; code = VK_V }
            { primary = 'b'; secondary = 'B'; code = VK_B }
            { primary = 'n'; secondary = 'N'; code = VK_N }
            { primary = 'm'; secondary = 'M'; code = VK_M }
            { primary = ','; secondary = '<'; code = OEM_COMMA }
            { primary = '.'; secondary = '>'; code = OEM_PERIOD }
            { primary = '/'; secondary = '?'; code = OEM_2 }
        ]
        
        yield! makeRow 100 (fromBottom 2) [
            { primary = 'a'; secondary = 'A'; code = VK_A }
            { primary = 's'; secondary = 'S'; code = VK_S }
            { primary = 'd'; secondary = 'D'; code = VK_D }
            { primary = 'f'; secondary = 'F'; code = VK_F }
            { primary = 'g'; secondary = 'G'; code = VK_G }
            { primary = 'h'; secondary = 'H'; code = VK_H }
            { primary = 'j'; secondary = 'J'; code = VK_J }
            { primary = 'k'; secondary = 'K'; code = VK_K }
            { primary = 'l'; secondary = 'L'; code = VK_L }
            { primary = ';'; secondary = ':'; code = OEM_1 }
            { primary = '\''; secondary = '"'; code = OEM_7 }
        ]
        
        yield! makeRow (afterKey key_Tab) (fromBottom 3) [
            { primary = 'q'; secondary = 'Q'; code = VK_Q }
            { primary = 'w'; secondary = 'W'; code = VK_W }
            { primary = 'e'; secondary = 'E'; code = VK_E }
            { primary = 'r'; secondary = 'R'; code = VK_R }
            { primary = 't'; secondary = 'T'; code = VK_T }
            { primary = 'y'; secondary = 'Y'; code = VK_Y }
            { primary = 'u'; secondary = 'U'; code = VK_U }
            { primary = 'i'; secondary = 'I'; code = VK_I }
            { primary = 'o'; secondary = 'O'; code = VK_O }
            { primary = 'p'; secondary = 'P'; code = VK_P }
            { primary = '['; secondary = '{'; code = OEM_4 }
            { primary = ']'; secondary = '}'; code = OEM_6 }
            { primary = '\\'; secondary = '|'; code = OEM_5 }
        ]
        
        let panel, _, _, _ as backtick = makeKey 10 (fromBottom 4) 30 { primary = '`'; secondary = '~'; code = OEM_3 }
        backtick
        yield! makeRow (afterKey panel) (fromBottom 4) [
            { primary = '1'; secondary = '!'; code = VK_1 }
            { primary = '2'; secondary = '@'; code = VK_2 }
            { primary = '3'; secondary = '#'; code = VK_3 }
            { primary = '4'; secondary = '$'; code = VK_4 }
            { primary = '5'; secondary = '%'; code = VK_5 }
            { primary = '6'; secondary = '^'; code = VK_6 }
            { primary = '7'; secondary = '&'; code = VK_7 }
            { primary = '8'; secondary = '*'; code = VK_8 }
            { primary = '9'; secondary = '('; code = VK_9 }
            { primary = '0'; secondary = ')'; code = VK_0 }
            { primary = '-'; secondary = '_'; code = OEM_MINUS }
            { primary = '='; secondary = '+'; code = OEM_PLUS }
        ]
    |]

    let key_Enter =
        makeBasicKey (
            afterKey (keys |> Seq.find(fun key -> key.Item4.secondary = '"')).Item1
        ) (fromBottom 2) 95 65 "Enter"

    let key_Backspace = makeBasicKey (afterKey keys.[^0].Item1) (fromBottom 4) 85 70 "Backspace"

    let tools_y = (fromBottom 5) - 10
    let tools_width = 60
    let tools_height = 35

    let makeToolKey x name =
        makeVeryBasicKey x tools_y tools_width 45 tools_height name
    
    let key_Escape = makeToolKey 10 "esc"
    let key_Undo = makeToolKey (afterKey key_Escape) "Undo"
    let key_Redo = makeToolKey (afterKey key_Undo) "Redo"
    let key_Cut = makeToolKey (afterKey key_Redo) "Cut"
    let key_Copy = makeToolKey (afterKey key_Cut) "Copy"
    let key_Paste = makeToolKey (afterKey key_Copy) "Paste"
    let key_PageUp = makeToolKey (afterKey key_Paste) "pg up"
    let key_PageDown = makeToolKey (afterKey key_PageUp) "pg dn"
    let key_Home = makeToolKey (afterKey key_PageDown) "home"
    let key_End = makeToolKey (afterKey key_Home) "end"
    let key_Insert = makeToolKey (afterKey key_End) "insert"
    let key_Delete = makeVeryBasicKey (afterKey key_Insert) tools_y (tools_width+15) 45 tools_height "delete"

    let arrow_width = 55.
    let arrow_height = 40.
    let arrow_bottom = abs_bottom + 5
    let key_Left = 
        let width = arrow_width
        let height = arrow_height
        let mid_height = height / 2.

        StackPanel(
            Width = width,
            Height = height,
            Margin = thickness 0 arrow_bottom 150 0,
            HorizontalAlignment = Horiz.Right,
            VerticalAlignment = Vertical.Top,
            Orientation = Orientation.Horizontal,
            Background = default_fill,
            Children = [
                Canvas(
                    Children = [
                        Shapes.Polygon(
                            Points = PointCollection [
                                Point(20.,  mid_height - 5.)
                                Point(12.5, mid_height)
                                Point(20.,  mid_height + 5.)
                            ],
                            Margin = thickness 0 0 0 0,
                            Fill = SolidColorBrush(rgb 0 0 0),
                            VerticalAlignment = Vertical.Top
                        )
                        Shapes.Line(
                            X1 = 20.,          Y1 = mid_height,
                            X2 = width - 12.5, Y2 = mid_height,
                            Margin = thickness 0 0 0 0,
                            Stroke = SolidColorBrush(rgb 0 0 0)
                        )
                    ]
                )
            ]
        )

    let key_Down = 
        let width = arrow_width
        let height = arrow_height
        let mid_width = width / 2.

        StackPanel(
            Width = width,
            Height = height,
            Margin = thickness 0 arrow_bottom (beforeKey key_Left) 0,
            HorizontalAlignment = Horiz.Right,
            VerticalAlignment = Vertical.Top,
            Orientation = Orientation.Horizontal,
            Background = default_fill,
            Children = [
                Canvas(
                    Children = [
                        Shapes.Line(
                            X1 = mid_width, Y1 = 12.5,
                            X2 = mid_width, Y2 = height - 20.,
                            Margin = thickness 0 0 0 0,
                            Stroke = SolidColorBrush(rgb 0 0 0)
                        )
                        Shapes.Polygon(
                            Points = PointCollection [
                                Point(mid_width - 5., height - 20.)
                                Point(mid_width,      height - 12.5)
                                Point(mid_width + 5., height - 20.)
                            ],
                            Margin = thickness 0 0 0 0,
                            Fill = SolidColorBrush(rgb 0 0 0),
                            VerticalAlignment = Vertical.Top
                        )
                    ]
                )
            ]
        )
    
    let key_Up = 
        let width = arrow_width
        let height = arrow_height
        let mid_width = width / 2.

        StackPanel(
            Width = width,
            Height = height,
            Margin = thickness 0 (arrow_bottom - int height - 10) (beforeKey key_Left) 0,
            HorizontalAlignment = Horiz.Right,
            VerticalAlignment = Vertical.Top,
            Orientation = Orientation.Horizontal,
            Background = default_fill,
            Children = [
                Canvas(
                    Children = [
                        Shapes.Polygon(
                            Points = PointCollection [
                                Point(mid_width - 5., 20.)
                                Point(mid_width,      12.5)
                                Point(mid_width + 5., 20.)
                            ],
                            Margin = thickness 0 0 0 0,
                            Fill = SolidColorBrush(rgb 0 0 0),
                            VerticalAlignment = Vertical.Top
                        )
                        Shapes.Line(
                            X1 = mid_width, Y1 = 20.,
                            X2 = mid_width, Y2 = height - 12.5,
                            Margin = thickness 0 0 0 0,
                            Stroke = SolidColorBrush(rgb 0 0 0)
                        )
                    ]
                )
            ]
        )

    let key_Right = 
        let width = arrow_width
        let height = arrow_height
        let mid_height = height / 2.

        StackPanel(
            Width = width,
            Height = height,
            Margin = thickness 0 arrow_bottom (beforeKey key_Down) 0,
            HorizontalAlignment = Horiz.Right,
            VerticalAlignment = Vertical.Top,
            Orientation = Orientation.Horizontal,
            Background = default_fill,
            Children = [
                Canvas(
                    Children = [
                        Shapes.Line(
                            X1 = 12.5,        Y1 = mid_height,
                            X2 = width - 20., Y2 = mid_height,
                            Margin = thickness 0 0 0 0,
                            Stroke = SolidColorBrush(rgb 0 0 0)
                        )
                        Shapes.Polygon(
                            Points = PointCollection [
                                Point(width - 20.,  mid_height - 5.)
                                Point(width - 12.5, mid_height)
                                Point(width - 20.,  mid_height + 5.)
                            ],
                            Margin = thickness 0 0 0 0,
                            Fill = SolidColorBrush(rgb 0 0 0),
                            VerticalAlignment = Vertical.Top
                        )
                    ]
                )
            ]
        )

    // Layout
    
    win.Content <- mainGrid
    
    mainGrid.Children <-
        let allKeys = seq<FrameworkElement> {
            //debugLabel

            key_Ctrl
            key_Alt
            key_Space

            key_Left
            key_Down
            key_Up
            key_Right

            key_Shift
            
            key_Enter

            key_Tab
            
            for panel, _, label2, _ in keys do
                label2.Opacity <- default_opacity
                panel

            key_Backspace

            key_Escape
            key_Undo; key_Redo
            key_Cut; key_Copy; key_Paste
            key_PageUp; key_PageDown
            key_Home; key_End
            key_Insert
            key_Delete
        }

        let mutable i = 0
        for panel in allKeys do
            panel.Tag <- i
            i <- i + 1

        allKeys

    key_Tab_label2.Opacity <- default_opacity
    
    
    // State

    let resetState () =
        if state.shift then
            key_Shift.Background <- default_fill
            state.shift <- false
            for _, label, _, {primary = p} in keys do
                label.Content <- p

        if state.ctrl then
            key_Ctrl.Background <- default_fill
            state.ctrl <- false

        if state.alt then
            key_Alt.Background <- default_fill
            state.alt <- false
    
    let mutable activeKeys = Map<int, ActiveKey> []

    
    // Events

    let timePressed (timer: DispatcherTimer) ms (button: StackPanel) =
        let task = TaskCompletionSource<bool>()
    
        timer.Interval <- TimeSpan.FromMilliseconds(ms)
    
        let rec onUp = EventHandler<TouchEventArgs>(fun _ _ ->
            timer.Stop()
            match task.Task.Status with
            | TaskStatus.Running ->
                button.PreviewTouchUp.RemoveHandler onUp
                button.TouchLeave.RemoveHandler onUp
                timer.Tick.RemoveHandler tick
                task.SetResult false
            | _ -> ())
            
        and tick = EventHandler(fun _ _ ->
            button.PreviewTouchUp.RemoveHandler onUp
            button.TouchLeave.RemoveHandler onUp
            timer.Stop()
            timer.Tick.RemoveHandler tick
            task.SetResult true)
    
        button.PreviewTouchUp.AddHandler onUp
        button.TouchLeave.AddHandler onUp
        timer.Tick.AddHandler tick
        timer.Start()
        
        task.Task

    let maybeRepeatKey button key =
        let timer = DispatcherTimer()
        Async.StartImmediate <| async { // TODO: replace with task { ... } in F# 6.0 once its stable
            let! res =
                button
                |> timePressed timer 1000.
                |> Async.AwaitTask

            if res then
                let mutable res = true
                while res do
                    keyPress key
                    let! res' =
                        button
                        |> timePressed timer 20.
                        |> Async.AwaitTask in res <- res'
        }

    Stylus.SetIsPressAndHoldEnabled(win, false)

    let fixKeyFlick (label1: Label) (label2: Label) left key1 =
        label1.Content <- key1
        label1.Opacity <- end_opacity
        label1.Margin <-
            let mutable m = label1.Margin
            m.Top <- 12.
            m
        label1.UpdateLayout()

        label2.Opacity <- default_opacity
        label2.Margin <-
            let mutable m = label2.Margin
            m.Left <- float left
            m
        label2.UpdateLayout()

    let keyTouchDown (panel: StackPanel) (e: TouchEventArgs) =
        let index = Unchecked.unbox<int> panel.Tag
        panel.Background <- pressed_fill
        activeKeys <- activeKeys.Add(index, {startTouch = e.GetTouchPoint(panel); flicked = false})

    let keyTouchUp (panel: StackPanel) key =
        let index = Unchecked.unbox<int> panel.Tag
        if activeKeys.ContainsKey(index) then
            panel.Background <- default_fill
            activeKeys <- activeKeys.Remove(index)
            keyPress key

    let keyTouchMove (panel: StackPanel) (label1: Label) (label2: Label) (e: TouchEventArgs) left =
        let index = Unchecked.unbox<int> panel.Tag
        match activeKeys.TryFind(index) with
        | Some({startTouch = point; flicked = flicked} as activeKey) ->
            let y = point.Position.Y
            let y' = e.GetTouchPoint(panel).Position.Y
            let y_ = y' - y
        
            if y_ > 3.0 then
                label1.Opacity <- Math.Clamp(1. - ((12. + (2. * abs y_)) / 100.), default_opacity, 1.)
                label2.Opacity <- Math.Clamp((12. + (2. * abs y_)) / 100., 0., end_opacity)
                label2.Margin <-
                    // I wish I could use `with` on struct types
                    let mutable m = label2.Margin
                    m.Left <- max 0. (float left - y_)
                    m
            
                label2.UpdateLayout()

        
            if y_ > 10.0 then
                if not flicked then activeKey.flicked <- true
                label1.Margin <-
                    let mutable m = label1.Margin
                    m.Top <- 12. + (y_ / 2.)
                    m

                label1.UpdateLayout()

        | _ -> ()

    let keyTouchLeave (panel: StackPanel) (label1: Label) (label2: Label) left key1 key2 =
        let index = Unchecked.unbox<int> panel.Tag
        match activeKeys.TryFind(index) with
        | Some {flicked = true} -> 
            fixKeyFlick label1 label2 left key1

            panel.Background <- default_fill
            
            activeKeys <- activeKeys.Remove(index)

            state.shift <- true
            keyPress key2
            resetState()
        
        | _ -> ()


    key_Ctrl.TouchDown += fun _ _ ->
        if state.ctrl then
            state.ctrl <- false
        else
            key_Ctrl.Background <- pressed_fill
            state.ctrl <- true

    key_Ctrl.TouchUp += fun _ _ ->
        if not state.ctrl then
            key_Ctrl.Background <- default_fill


    key_Alt.TouchDown += fun _ _ ->
        if state.alt then
            state.alt <- false
        else
            key_Alt.Background <- pressed_fill
            state.alt <- true

    key_Alt.TouchUp += fun _ _ ->
        if not state.alt then
            key_Alt.Background <- default_fill


    key_Space.TouchDown += fun _ e ->
        keyTouchDown key_Space e
        maybeRepeatKey key_Space SPACE
        

    key_Space.TouchUp += fun _ _ ->
        keyTouchUp key_Space SPACE
        resetState()


    let arrows = [ key_Left, LEFT
                   key_Down, DOWN
                   key_Up, UP
                   key_Right, RIGHT ] in
    for key, code in arrows do
        key.TouchDown += fun _ e ->
            keyTouchDown key e
            maybeRepeatKey key code
        
        key.TouchUp += fun _ _ ->
            keyTouchUp key code
            // don't disable modifiers


    key_Shift.TouchDown += fun _ _ ->
        if state.shift then
            state.shift <- false
        else
            key_Shift.Background <- pressed_fill
            state.shift <- true
            key_Tab_label1.Content <- "Detab"
            for _, label, _, {secondary = s} in keys do
                label.Content <- s

    key_Shift.TouchUp += fun _ _ ->
        if not state.shift then
            key_Shift.Background <- default_fill
            key_Tab_label1.Content <- "Tab"
            for _, label, _, {primary = p} in keys do
                label.Content <- p

    
    key_Tab.TouchDown += fun _ e ->
        keyTouchDown key_Tab e
        maybeRepeatKey key_Tab TAB

    key_Tab.TouchUp += fun _ _ ->
        keyTouchUp key_Tab TAB
        fixKeyFlick key_Tab_label1 key_Tab_label2 20 "Tab"
        resetState()

    key_Tab.TouchMove += fun _ e ->
        keyTouchMove key_Tab key_Tab_label1 key_Tab_label2 e 20

    key_Tab.TouchLeave += fun _ _ ->
        state.shift <- true
        keyTouchLeave key_Tab key_Tab_label1 key_Tab_label2 20 "Tab" TAB
        state.shift <- false
        resetState()


    key_Enter.TouchDown += fun _ e ->
        keyTouchDown key_Enter e
        maybeRepeatKey key_Enter RETURN
    
    key_Enter.TouchUp += fun _ _ ->
        keyTouchUp key_Enter RETURN
        resetState()

    
    key_Backspace.TouchDown += fun _ e ->
        keyTouchDown key_Backspace e
        maybeRepeatKey key_Backspace BACK

    key_Backspace.TouchUp += fun _ _ ->
        keyTouchUp key_Backspace BACK
        resetState()

    
    key_Escape.TouchDown += fun _ e ->
        keyTouchDown key_Escape e
        maybeRepeatKey key_Escape ESCAPE

    key_Escape.TouchUp += fun _ _ ->
        keyTouchUp key_Escape ESCAPE
        resetState()

    let tools = [ key_Undo, VK_Z
                  key_Redo, VK_Y // TODO: add option to use Ctrl+Shift+Z
                  key_Cut, VK_X
                  key_Copy, VK_C
                  key_Paste, VK_V ] in
    for key, code in tools do
        key.TouchDown += fun _ e ->
            keyTouchDown key e
            maybeRepeatKey key code

        key.TouchUp += fun _ _ ->
            resetState()
            state.ctrl <- true
            keyTouchUp key code
            resetState()

    let tools = [ key_PageUp, PRIOR
                  key_PageDown, NEXT
                  key_Home, HOME
                  key_End, END
                  key_Insert, INSERT
                  key_Delete, DELETE ] in
    for key, code in tools do
        key.TouchDown += fun _ e ->
            keyTouchDown key e
            maybeRepeatKey key code

        key.TouchUp += fun _ _ ->
            keyTouchUp key code
            // don't disable modifiers
    

    for panel, label1, label2, key in keys do
        let left = if key.primary = '`' then 10 else 30

        panel.TouchDown += fun _ e ->
            keyTouchDown panel e
            maybeRepeatKey panel key.code

        panel.TouchUp += fun _ _ ->
            keyTouchUp panel key.code
            fixKeyFlick label1 label2 left key.primary
            resetState()

        panel.TouchMove += fun _ e ->
            keyTouchMove panel label1 label2 e left

        panel.TouchLeave += fun _ _ ->
            keyTouchLeave panel label1 label2 left key.primary key.code


    win.TouchUp += fun _ _ ->
        if not state.ctrl then key_Ctrl.Background <- default_fill
        if not state.alt then key_Alt.Background <- default_fill
        if not state.shift then key_Shift.Background <- default_fill
        for key in [key_Space; key_Enter; key_Backspace; key_Left; key_Down; key_Up; key_Right] do
            key.Background <- default_fill

    win.TouchLeave += fun _ _ ->
        for panel, _, _, _ in keys do
            panel.Background <- default_fill

    win.Activated += fun _ _ ->
        let cur = Win32Api.GetForegroundWindow()
        let prev = Win32Api.GetWindow(cur, Win32Api.GetWindowType.GW_HWNDNEXT)
        let p =
            let mutable pid = 0u
            Win32Api.GetWindowThreadProcessId(prev, &pid) |> ignore
            Process.GetProcessById(int pid)
        
        prevProc <- Some p
        win.Topmost <- true

    win.Deactivated += fun _ _ ->
        // TODO: make this smarter
        if prevProc.IsSome then
            win.Topmost <- false
        else
            win.Topmost <- true
    
    // NEVER AGAIN BAD BAD BAD
    win.SizeChanged += fun _ e ->
        let s = e.PreviousSize
        let s' = e.NewSize

        // TODO: don't strech but also keep UI usable
        if (e.WidthChanged || e.HeightChanged) && not (s.Width = 0. && s.Height = 0.) then
            let wc = s'.Width / s.Width
            let hc = s'.Height / s.Height
            
            for child in mainGrid.Children do
                let child = child :?> FrameworkElement
            
                let scaleW, scaleH =
                    match child.LayoutTransform with
                    | :? ScaleTransform as t -> t.ScaleX, t.ScaleY
                    | _ -> 1., 1. // idk of any other transformations that matter here

                child.LayoutTransform <-
                    ScaleTransform(
                        scaleW * (if e.WidthChanged && isnormal child.Width && wc <> infinity then wc else 1.),
                        scaleH * (if e.HeightChanged && isnormal child.Height && hc <> infinity then hc else 1.)
                    )

                child.Margin <-
                    let mutable m = child.Margin

                    if s.Width <> 0. then
                        if m.Right <> 0.
                            then m.Right <- m.Right * wc
                            else m.Left <- m.Left * wc
                    
                    if s.Height <> 0. then
                        if m.Bottom <> 0.
                             then m.Bottom <- m.Bottom * hc
                             else m.Top <- m.Top * hc
                    
                    m

    ()


[<STAThread>]
[<EntryPoint>]
let main argv =
    let app = Application()

    let window =
        Window(
            Title = "Flickeyboard",
            Width = 900.,
            Height = 450.,
            Background = SolidColorBrush(rgb 144 144 144),
            WindowStyle =
#if DEBUG
                        WindowStyle.SingleBorderWindow,
#else
                        WindowStyle.ToolWindow,
#endif
            IsTabStop = false,
            RenderTransformOrigin = Point(0.5, 0.5),
            VerticalAlignment = Vertical.Bottom,
            HorizontalContentAlignment = Horiz.Center,
            VerticalContentAlignment = Vertical.Bottom
        )
    
    buildWindow window
    app.Run(window) |> ignore
    
    0