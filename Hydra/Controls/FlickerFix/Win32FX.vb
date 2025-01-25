Public Class Win32FX

    <Flags()> _
    Public Enum WindowStylesEx As UInteger
        ''' <summary>Specifies a window that accepts drag-drop files.</summary>
        WS_EX_ACCEPTFILES = &H10

        ''' <summary>Forces a top-level window onto the taskbar when the window is visible.</summary>
        WS_EX_APPWINDOW = &H40000

        ''' <summary>Specifies a window that has a border with a sunken edge.</summary>
        WS_EX_CLIENTEDGE = &H200

        ''' <summary>
        ''' Specifies a window that paints all descendants in bottom-to-top painting order using double-buffering.
        ''' This cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC. This style is not supported in Windows 2000.
        ''' </summary>
        ''' <remarks>
        ''' With WS_EX_COMPOSITED set, all descendants of a window get bottom-to-top painting order using double-buffering.
        ''' Bottom-to-top painting order allows a descendent window to have translucency (alpha) and transparency (color-key) effects,
        ''' but only if the descendent window also has the WS_EX_TRANSPARENT bit set.
        ''' Double-buffering allows the window and its descendents to be painted without flicker.
        ''' </remarks>
        WS_EX_COMPOSITED = &H2000000

        ''' <summary>
        ''' Specifies a window that includes a question mark in the title bar. When the user clicks the question mark,
        ''' the cursor changes to a question mark with a pointer. If the user then clicks a child window, the child receives a WM_HELP message.
        ''' The child window should pass the message to the parent window procedure, which should call the WinHelp function using the HELP_WM_HELP command.
        ''' The Help application displays a pop-up window that typically contains help for the child window.
        ''' WS_EX_CONTEXTHELP cannot be used with the WS_MAXIMIZEBOX or WS_MINIMIZEBOX styles.
        ''' </summary>
        WS_EX_CONTEXTHELP = &H400

        ''' <summary>
        ''' Specifies a window which contains child windows that should take part in dialog box navigation.
        ''' If this style is specified, the dialog manager recurses into children of this window when performing navigation operations
        ''' such as handling the TAB key, an arrow key, or a keyboard mnemonic.
        ''' </summary>
        WS_EX_CONTROLPARENT = &H10000

        ''' <summary>Specifies a window that has a double border.</summary>
        WS_EX_DLGMODALFRAME = &H1

        ''' <summary>
        ''' Specifies a window that is a layered window.
        ''' This cannot be used for child windows or if the window has a class style of either CS_OWNDC or CS_CLASSDC.
        ''' </summary>
        WS_EX_LAYERED = &H80000

        ''' <summary>
        ''' Specifies a window with the horizontal origin on the right edge. Increasing horizontal values advance to the left.
        ''' The shell language must support reading-order alignment for this to take effect.
        ''' </summary>
        WS_EX_LAYOUTRTL = &H400000

        ''' <summary>Specifies a window that has generic left-aligned properties. This is the default.</summary>
        WS_EX_LEFT = &H0

        ''' <summary>
        ''' Specifies a window with the vertical scroll bar (if present) to the left of the client area.
        ''' The shell language must support reading-order alignment for this to take effect.
        ''' </summary>
        WS_EX_LEFTSCROLLBAR = &H4000

        ''' <summary>
        ''' Specifies a window that displays text using left-to-right reading-order properties. This is the default.
        ''' </summary>
        WS_EX_LTRREADING = &H0

        ''' <summary>
        ''' Specifies a multiple-document interface (MDI) child window.
        ''' </summary>
        WS_EX_MDICHILD = &H40

        ''' <summary>
        ''' Specifies a top-level window created with this style does not become the foreground window when the user clicks it.
        ''' The system does not bring this window to the foreground when the user minimizes or closes the foreground window.
        ''' The window does not appear on the taskbar by default. To force the window to appear on the taskbar, use the WS_EX_APPWINDOW style.
        ''' To activate the window, use the SetActiveWindow or SetForegroundWindow function.
        ''' </summary>
        WS_EX_NOACTIVATE = &H8000000

        ''' <summary>
        ''' Specifies a window which does not pass its window layout to its child windows.
        ''' </summary>
        WS_EX_NOINHERITLAYOUT = &H100000

        ''' <summary>
        ''' Specifies that a child window created with this style does not send the WM_PARENTNOTIFY message to its parent window when it is created or destroyed.
        ''' </summary>
        WS_EX_NOPARENTNOTIFY = &H4

        ''' <summary>
        ''' The window does not render to a redirection surface.
        ''' This is for windows that do not have visible content or that use mechanisms other than surfaces to provide their visual.
        ''' </summary>
        WS_EX_NOREDIRECTIONBITMAP = &H200000

        ''' <summary>Specifies an overlapped window.</summary>
        WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE Or WS_EX_CLIENTEDGE

        ''' <summary>Specifies a palette window, which is a modeless dialog box that presents an array of commands.</summary>
        WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE Or WS_EX_TOOLWINDOW Or WS_EX_TOPMOST

        ''' <summary>
        ''' Specifies a window that has generic "right-aligned" properties. This depends on the window class.
        ''' The shell language must support reading-order alignment for this to take effect.
        ''' Using the WS_EX_RIGHT style has the same effect as using the SS_RIGHT (static), ES_RIGHT (edit), and BS_RIGHT/BS_RIGHTBUTTON (button) control styles.
        ''' </summary>
        WS_EX_RIGHT = &H1000

        ''' <summary>Specifies a window with the vertical scroll bar (if present) to the right of the client area. This is the default.</summary>
        WS_EX_RIGHTSCROLLBAR = &H0

        ''' <summary>
        ''' Specifies a window that displays text using right-to-left reading-order properties.
        ''' The shell language must support reading-order alignment for this to take effect.
        ''' </summary>
        WS_EX_RTLREADING = &H2000

        ''' <summary>Specifies a window with a three-dimensional border style intended to be used for items that do not accept user input.</summary>
        WS_EX_STATICEDGE = &H20000

        ''' <summary>
        ''' Specifies a window that is intended to be used as a floating toolbar.
        ''' A tool window has a title bar that is shorter than a normal title bar, and the window title is drawn using a smaller font.
        ''' A tool window does not appear in the taskbar or in the dialog that appears when the user presses ALT+TAB.
        ''' If a tool window has a system menu, its icon is not displayed on the title bar.
        ''' However, you can display the system menu by right-clicking or by typing ALT+SPACE.
        ''' </summary>
        WS_EX_TOOLWINDOW = &H80

        ''' <summary>
        ''' Specifies a window that should be placed above all non-topmost windows and should stay above them, even when the window is deactivated.
        ''' To add or remove this style, use the SetWindowPos function.
        ''' </summary>
        WS_EX_TOPMOST = &H8

        ''' <summary>
        ''' Specifies a window that should not be painted until siblings beneath the window (that were created by the same thread) have been painted.
        ''' The window appears transparent because the bits of underlying sibling windows have already been painted.
        ''' To achieve transparency without these restrictions, use the SetWindowRgn function.
        ''' </summary>
        WS_EX_TRANSPARENT = &H20

        ''' <summary>Specifies a window that has a border with a raised edge.</summary>
        WS_EX_WINDOWEDGE = &H100
    End Enum

End Class
