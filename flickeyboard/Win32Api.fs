module Win32Api

open System
open System.Runtime.InteropServices


type GetWindowType =
    | GW_HWNDFIRST = 0u
    | GW_HWNDLAST = 1u
    | GW_HWNDNEXT = 2u
    | GW_HWNDPREV = 3u
    | GW_OWNER = 4u
    | GW_CHILD = 5u
    | GW_ENABLEDPOPUP = 6u


[<DllImport("user32.dll")>]
extern IntPtr GetForegroundWindow()

[<DllImport("user32.dll")>]
extern bool SetForegroundWindow(IntPtr hWnd)

[<DllImport("user32.dll")>]
extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, [<Out>] uint& processId)

[<DllImport("user32.dll")>]
extern void SwitchToThisWindow(IntPtr hWnd, bool unknown)

[<DllImport("user32.dll")>]
extern IntPtr GetWindow(IntPtr hWnd, GetWindowType wCmd)

[<DllImport("user32.dll")>]
extern IntPtr GetFocus()