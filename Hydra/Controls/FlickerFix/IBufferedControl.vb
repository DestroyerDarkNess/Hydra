' ***********************************************************************
' Author   : Elektro
' Modified : 20-March-2017
' ***********************************************************************

#Region " Public Members Summary "

#Region " Properties "

' CreateParams As CreateParams
' DoubleBuffered As Boolean
' PreventFlickering As Boolean

#End Region

#End Region

#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.ComponentModel
Imports System.Windows.Forms

#End Region

#Region " IBufferedControl "

Namespace Types

    ''' ----------------------------------------------------------------------------------------------------
    ''' <summary>
    ''' Provides simple double buffering (anti flickering) functionality for a Windows Forms <see cref="Control"/>,
    ''' such for example a <see cref="TextBox"/>.
    ''' </summary>
    ''' ----------------------------------------------------------------------------------------------------
    Public Interface IBufferedControl

        ''' ----------------------------------------------------------------------------------------------------
        ''' <summary>
        ''' Gets the required creation parameters when the control handle is created.
        ''' </summary>
        ''' ----------------------------------------------------------------------------------------------------
        ''' <value>
        ''' The creation parameters.
        ''' </value>
        ''' ----------------------------------------------------------------------------------------------------
        <Browsable(False)>
        <EditorBrowsable(EditorBrowsableState.Advanced)>
        ReadOnly Property CreateParams As CreateParams
        ' Implementation Exmple:
        '
        ' Protected Overrides ReadOnly Property CreateParams As CreateParams Implements IBufferedControl.CreateParams
        '     Get
        '         If (Me.preventFlickeringB) Then
        '             Dim cp As CreateParams = MyBase.CreateParams
        '             cp.ExStyle = (cp.ExStyle Or CInt(WindowStylesEx.WS_EX_COMPOSITED))
        '             Return cp
        '         Else
        '             Return MyBase.CreateParams
        '         End If
        '     End Get
        ' End Property

        ''' ----------------------------------------------------------------------------------------------------
        ''' <summary>
        ''' Gets or sets a value indicating whether this control should redraw its surface using a secondary buffer 
        ''' to reduce or prevent flicker.
        ''' </summary>
        ''' ----------------------------------------------------------------------------------------------------
        ''' <value>
        ''' <see langword="True"/> if the surface of the control should be drawn using double buffering; 
        ''' otherwise, <see langword="False"/>.
        ''' </value>
        ''' ----------------------------------------------------------------------------------------------------
        <Browsable(True)>
        <EditorBrowsable(EditorBrowsableState.Always)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
        <Localizable(True)>
        <Category("Behavior")>
        <Description("Indicates whether this control should redraw its surface using a secondary buffer to reduce or prevent flicker.")>
        <DefaultValue(GetType(Boolean), "True")>
        Property DoubleBuffered As Boolean
        ' Implementation Exmple:
        '
        ' Public Overridable Shadows Property DoubleBuffered As Boolean Implements IBufferedControl.DoubleBuffered
        '     Get
        '         Return MyBase.DoubleBuffered
        '     End Get
        '     Set(ByVal value As Boolean)
        '         Me.SetStyle(ControlStyles.DoubleBuffer, value)
        '         MyBase.DoubleBuffered = value
        '     End Set
        ' End Property

        ''' ----------------------------------------------------------------------------------------------------
        ''' <summary>
        ''' Gets or sets a value that indicates whether the control should avoid unwanted flickering effects.
        ''' <para></para>
        ''' If <see langword="True"/>, this will avoid any flickering effect on the control, however,
        ''' it will also have a negative impact by slowing down the responsiveness of the control about to 30% slower.
        ''' <para></para>
        ''' This negative impact doesn't affect to the performance of the application itself, 
        ''' just to the performance of this control.
        ''' </summary>
        ''' ----------------------------------------------------------------------------------------------------
        ''' <value>
        ''' A value that indicates whether the control should avoid unwanted flickering effects.
        ''' </value>
        ''' ----------------------------------------------------------------------------------------------------
        <Browsable(True)>
        <EditorBrowsable(EditorBrowsableState.Always)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
        <Localizable(True)>
        <Category("Behavior")>
        <Description("Indicates whether the control should avoid unwanted flickering effects. If True, this will avoid any flickering effect on the control, however, it will also have a negative impact by slowing down the responsiveness of the control about to 30% slower.")>
        <DefaultValue(GetType(Boolean), "False")>
        Property PreventFlickering As Boolean
        ' Implementation Exmple:
        '
        ' Public Overridable Property PreventFlickering As Boolean Implements IBufferedControl.PreventFlickering
        '     Get
        '         Return Me.preventFlickeringB
        '     End Get
        '     Set(ByVal value As Boolean)
        '         Me.preventFlickeringB = value
        '     End Set
        ' End Property
        ' ''' ----------------------------------------------------------------------------------------------------
        ' ''' <summary>
        ' ''' ( Backing Field )
        ' ''' A value that indicates whether the control should avoid unwanted flickering effects.
        ' ''' </summary>
        ' ''' ----------------------------------------------------------------------------------------------------
        ' Private preventFlickeringB As Boolean

    End Interface

End Namespace

#End Region