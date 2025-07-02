Imports System.Drawing.Imaging
Imports System.IO
Imports System.Reflection

Namespace Core.Helpers

    Public Class Utils

        ''' <summary>
        ''' Converts a PNG image to an icon (ico)
        ''' </summary>
        ''' <param name="inputBitmap">The input stream</param>
        ''' <param name="output">The output stream</param>
        ''' <param name="size">Needs to be a factor of 2 (16x16 px by default)</param>
        ''' <param name="preserveAspectRatio">Preserve the aspect ratio</param>
        ''' <returns>Wether or not the icon was succesfully generated</returns>
        Public Shared Function ConvertToIcon(inputBitmap As Image, output As Stream, Optional size As Integer = 16, Optional preserveAspectRatio As Boolean = False) As Boolean

            Dim width As Single = size, height As Single = size

            Dim newBitmap = New Bitmap(inputBitmap, New Size(CInt(width), CInt(height)))
            If newBitmap Is Nothing Then
                Return False
            End If

            ' save the resized png into a memory stream for future use
            Using memoryStream As New MemoryStream()
                newBitmap.Save(memoryStream, ImageFormat.Png)

                Dim iconWriter = New BinaryWriter(output)
                If output Is Nothing OrElse iconWriter Is Nothing Then
                    Return False
                End If

                ' 0-1 reserved, 0
                iconWriter.Write(CByte(0))
                iconWriter.Write(CByte(0))

                ' 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write(CShort(1))

                ' 4-5 number of images
                iconWriter.Write(CShort(1))

                ' image entry 1
                ' 0 image width
                iconWriter.Write(CByte(width))
                ' 1 image height
                iconWriter.Write(CByte(height))

                ' 2 number of colors
                iconWriter.Write(CByte(0))

                ' 3 reserved
                iconWriter.Write(CByte(0))

                ' 4-5 color planes
                iconWriter.Write(CShort(0))

                ' 6-7 bits per pixel
                iconWriter.Write(CShort(32))

                ' 8-11 size of image data
                iconWriter.Write(CInt(memoryStream.Length))

                ' 12-15 offset of image data
                iconWriter.Write(CInt(6 + 16))

                ' write image data
                ' png data must contain the whole png data file
                iconWriter.Write(memoryStream.ToArray())

                iconWriter.Flush()
            End Using

            Return True
        End Function

        Public Shared Function ImageToByteArray(ByVal imageIn As Image) As Byte()
            Using ms As New MemoryStream()
                imageIn.Save(ms, imageIn.RawFormat)
                Return ms.ToArray()
            End Using
        End Function

        Public Shared Function OpenFile(Optional ByVal Formats As String = "", Optional MultiSelect As Boolean = False, Optional ByVal BasePath As String = "") As List(Of String)
            Dim OpenFileDialog1 As New OpenFileDialog
            ' OpenFileDialog1.DefaultExt = "txt"
            OpenFileDialog1.FileName = ""
            '  OpenFileDialog1.InitialDirectory = "c:\"
            OpenFileDialog1.Title = "Select file"
            OpenFileDialog1.Multiselect = MultiSelect

            If Formats = "" Then

                OpenFileDialog1.Filter = "Executable Files|*.exe"
            Else

                OpenFileDialog1.Filter = Formats

            End If

            If Not BasePath = "" Then
                OpenFileDialog1.InitialDirectory = BasePath
            End If

            Dim ListFiles As New List(Of String)

            If Not OpenFileDialog1.ShowDialog() = DialogResult.Cancel Then
                ListFiles.AddRange(OpenFileDialog1.FileNames)
                Return ListFiles
            End If

            Return Nothing

        End Function

        Public Shared Function SaveFile(Optional ByVal NameFile As String = "", Optional ByVal Formats As String = "", Optional ByVal InitialDirectory As String = "") As String
            Dim SaveFileDialog1 As New SaveFileDialog
            ' OpenFileDialog1.DefaultExt = "txt"
            SaveFileDialog1.FileName = NameFile
            If Not InitialDirectory = "" Then
                SaveFileDialog1.InitialDirectory = InitialDirectory
            End If
            SaveFileDialog1.Title = "Save file"
            If Not Formats = "" Then
                SaveFileDialog1.Filter = Formats
            Else
                SaveFileDialog1.Filter = "All files Suported|*.exe;*.dll|" &
                "Executable Files|*.exe|" &
                "Dll Files|*.dll"

            End If

            If Not SaveFileDialog1.ShowDialog() = DialogResult.Cancel Then
                Return SaveFileDialog1.FileName
            End If

            Return Nothing

        End Function

        Public Shared Function IsAdmin() As Boolean
            Try
                Dim Identity As System.Security.Principal.WindowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent()
                Dim Principal As System.Security.Principal.WindowsPrincipal = New System.Security.Principal.WindowsPrincipal(Identity)
                Dim IsElevated As Boolean = Principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)
                Return IsElevated
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Shared Function OpenAsAdmin(ByVal FilePth As String, Optional ByVal Argument As String = "") As Boolean
            Try
                Dim procStartInfo As New ProcessStartInfo
                Dim procExecuting As New Process

                With procStartInfo
                    .UseShellExecute = True
                    .FileName = FilePth
                    .Arguments = Argument
                    .WindowStyle = ProcessWindowStyle.Normal
                    .Verb = "runas"
                End With

                procExecuting = Process.Start(procStartInfo)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Shared Function ClosestConsoleColor(ByVal r As Byte, ByVal g As Byte, ByVal b As Byte) As ConsoleColor
            Dim ret As ConsoleColor = 0
            Dim rr As Double = r, gg As Double = g, bb As Double = b, delta As Double = Double.MaxValue

            For Each cc As ConsoleColor In [Enum].GetValues(GetType(ConsoleColor))
                Dim n = [Enum].GetName(GetType(ConsoleColor), cc)
                Dim c = System.Drawing.Color.FromName(If(n = "DarkYellow", "Orange", n))
                Dim t = Math.Pow(c.R - rr, 2.0) + Math.Pow(c.G - gg, 2.0) + Math.Pow(c.B - bb, 2.0)
                If t = 0.0 Then Return cc

                If t < delta Then
                    delta = t
                    ret = cc
                End If
            Next

            Return ret
        End Function

        Public Shared Function GetAll(ByVal control As Control, ByVal type As Type) As IEnumerable(Of Control)
            Dim controls = control.Controls.Cast(Of Control)()
            Return controls.SelectMany(Function(ctrl) GetAll(ctrl, type)).Concat(controls).Where(Function(c) c.[GetType]() = type)
        End Function

        Public Shared Function GetAllWithOutType(ByVal control As Control) As IEnumerable(Of Control)
            Dim controls = control.Controls.Cast(Of Control)()
            Return controls.SelectMany(Function(ctrl) GetAllWithOutType(ctrl)).Concat(controls)
        End Function

#Region " Mutation File "

        Public Shared Function MutateFile(ByVal TargetFile As String, ByVal DestinationDir As String) As String
            Try
                Dim FileExtension As String = IO.Path.GetExtension(TargetFile)
                Dim SourceData() As Byte = IO.File.ReadAllBytes(TargetFile)
                Dim RandomBytes() As Byte = System.Text.ASCIIEncoding.UTF8.GetBytes(RandomString(TimeOfDay.Second))
                Dim MutatedArray() As Byte = ArrayConcat(SourceData, RandomBytes)
                Dim OuputFile As String = IO.Path.Combine(DestinationDir, RandomString(5) & FileExtension)
                If My.Computer.FileSystem.FileExists(OuputFile) Then
                    My.Computer.FileSystem.DeleteFile(OuputFile)
                End If
                My.Computer.FileSystem.WriteAllBytes(OuputFile, MutatedArray, False)
                Return OuputFile
            Catch ex As Exception
                Return String.Empty
            End Try
        End Function

        Private Shared Function ArrayConcat(ByVal x() As Byte, ByVal y() As Byte) As Byte()
            Dim newx(x.Length + y.Length - 1) As Byte
            x.CopyTo(newx, 0)
            y.CopyTo(newx, x.Length)
            Return newx
        End Function

        Public Shared Function RandomString(ByVal length As Integer) As String
            Dim random As New Random()
            Dim charOutput As Char() = New Char(length - 1) {}
            For i As Integer = 0 To length - 1
                Dim selector As Integer = random.[Next](65, 101)
                If selector > 90 Then
                    selector -= 43
                End If
                charOutput(i) = Convert.ToChar(selector)
            Next
            Return New String(charOutput)
        End Function

#End Region

#Region " Base64 Functions "

#Region " Base64 Functions "

        Public Shared Function ConvertImageToBase64String(ByVal ImageL As Image) As String
            Try
                Using ms As New MemoryStream()
                    ImageL.Save(ms, System.Drawing.Imaging.ImageFormat.Png) 'We load the image from first PictureBox in the MemoryStream
                    Dim obyte = ms.ToArray() 'We tranform it to byte array..

                    Return Convert.ToBase64String(obyte) 'We then convert the byte array to base 64 string.
                End Using
            Catch ex As Exception
                Return String.Empty
            End Try
        End Function

        Public Shared Function ConvertBase64StringToImage(ByVal base64 As String) As Image
            Try
                Dim ImageBytes As Byte() = ConvertBase64ToByteArray(base64)
                Using ms As New MemoryStream(ImageBytes)
                    Dim image As Image = System.Drawing.Image.FromStream(ms)
                    Return image
                End Using
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        Public Shared Function ConvertBase64ToByteArray(ByVal base64 As String) As Byte()
            Return Convert.FromBase64String(base64) 'Convert the base64 back to byte array.
        End Function

        'Here's the part of your code (which works)
        Public Shared Function ConvertbByteToImage(ByVal BA As Byte()) As Image
            Dim ms As MemoryStream = New MemoryStream(BA)
            Dim image = System.Drawing.Image.FromStream(ms)
            Return image
        End Function

#End Region

#End Region

#Region " Sleep "

        ' [ Sleep ]
        '
        ' // By Elektro H@cker
        '
        ' Examples :
        ' Sleep(5) : MsgBox("Test")
        ' Sleep(5, Measure.Seconds) : MsgBox("Test")

        Public Enum Measure
            Milliseconds = 1
            Seconds = 2
            Minutes = 3
            Hours = 4
        End Enum

        Public Shared Sub Sleep(ByVal Duration As Int64, Optional ByVal Measure As Measure = Measure.Seconds)
            Try
                Dim Starttime = DateTime.Now

                Select Case Measure
                    Case Measure.Milliseconds : Do While (DateTime.Now - Starttime).TotalMilliseconds < Duration : Application.DoEvents() : Loop
                    Case Measure.Seconds : Do While (DateTime.Now - Starttime).TotalSeconds < Duration : Application.DoEvents() : Loop
                    Case Measure.Minutes : Do While (DateTime.Now - Starttime).TotalMinutes < Duration : Application.DoEvents() : Loop
                    Case Measure.Hours : Do While (DateTime.Now - Starttime).TotalHours < Duration : Application.DoEvents() : Loop
                    Case Else
                End Select
            Catch ex As Exception : End Try

        End Sub

#End Region

#Region " CenterForm function "

        Public Shared Function CenterForm(ByVal ParentForm As Form, ByVal Form_to_Center As Form) As Point
            Try
                Dim FormLocation As New Point
                FormLocation.X = (ParentForm.Left + (ParentForm.Width - Form_to_Center.Width) / 2) ' set the X coordinates.
                FormLocation.Y = (ParentForm.Top + (ParentForm.Height - Form_to_Center.Height) / 2) ' set the Y coordinates.
                Return FormLocation ' return the Location to the Form it was called from.
            Catch ex As Exception
                Return New Point((Screen.PrimaryScreen.WorkingArea.Width / 2) - Form_to_Center.Width, (Screen.PrimaryScreen.WorkingArea.Height / 2) - Form_to_Center.Height)
            End Try
        End Function

        Public Shared Function CenterForm(ByVal ParentForm As Form, ByVal Form_to_Center As Form, ByVal Form_Location As Point) As Point
            Dim FormLocation As New Point
            FormLocation.X = (ParentForm.Left + (ParentForm.Width - Form_to_Center.Width) / 2) ' set the X coordinates.
            FormLocation.Y = (ParentForm.Top + (ParentForm.Height - Form_to_Center.Height) / 2) ' set the Y coordinates.
            Return FormLocation ' return the Location to the Form it was called from.
        End Function

#End Region

#Region " PE Checker "

        Public Shared Function Is64BitPE(ByVal filePath As String) As Boolean
            Try

                Using stream = New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)

                    Using reader = New BinaryReader(stream)
                        reader.BaseStream.Position = &H3C
                        Dim peOffset As UInteger = reader.ReadUInt32()
                        reader.BaseStream.Position = peOffset + &H4
                        Dim machine As UShort = reader.ReadUInt16()
                        If machine <> &H14C AndAlso machine <> &H8664 Then Throw New InvalidDataException()
                        Return (machine = &H8664)
                    End Using
                End Using

                Return False
            Catch
                Return False
            End Try
        End Function

        ' Usage Examples:
        '
        ' MsgBox(IsNetAssembly("C:\File.exe"))
        ' MsgBox(IsNetAssembly("C:\File.dll"))

        ''' <summary>
        ''' Gets the common language runtime (CLR) version information of the specified file, using the specified buffer.
        ''' </summary>
        ''' <param name="filepath">Indicates the filepath of the file to be examined.</param>
        ''' <param name="buffer">Indicates the buffer allocated for the version information that is returned.</param>
        ''' <param name="buflen">Indicates the size, in wide characters, of the buffer.</param>
        ''' <param name="written">Indicates the size, in bytes, of the returned buffer.</param>
        ''' <returns>System.Int32.</returns>
        <System.Runtime.InteropServices.DllImport("mscoree.dll",
        CharSet:=System.Runtime.InteropServices.CharSet.Unicode)>
        Private Shared Function GetFileVersion(
                          ByVal filepath As String,
                          ByVal buffer As System.Text.StringBuilder,
                          ByVal buflen As Integer,
                          ByRef written As Integer
        ) As Integer
        End Function

        ''' <summary>
        ''' Determines whether an exe/dll file is an .Net assembly.
        ''' </summary>
        ''' <param name="File">Indicates the exe/dll file to check.</param>
        ''' <returns><c>true</c> if file is an .Net assembly; otherwise, <c>false</c>.</returns>
        Public Shared Function IsNetAssembly(ByVal [File] As String) As Boolean
            Try
                Dim assembly As AssemblyName = AssemblyName.GetAssemblyName([File])
                Return True
            Catch ex As Exception
                Return False
            End Try
            '  Dim sb = New System.Text.StringBuilder(256)
            '  Dim written As Integer = 0
            '  Dim hr = GetFileVersion([File], sb, sb.Capacity, written)
            ' Return hr = 0
        End Function

        Public Shared Function IsBinary(ByVal filePath As String, ByVal Optional requiredConsecutiveNul As Integer = 1) As Boolean
            Const charsToCheck As Integer = 8000
            Const nulChar As Char = vbNullChar
            Dim nulCount As Integer = 0

            Using streamReaderEx = New StreamReader(filePath)

                For i = 0 To charsToCheck - 1
                    If streamReaderEx.EndOfStream Then Return False

                    If Microsoft.VisualBasic.ChrW(streamReaderEx.Read()) = nulChar Then
                        nulCount += 1
                        If nulCount >= requiredConsecutiveNul Then Return True
                    Else
                        nulCount = 0
                    End If
                Next
            End Using

            Return False
        End Function

#End Region

    End Class

End Namespace