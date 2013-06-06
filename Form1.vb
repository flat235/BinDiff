Public Class Form1

    Private hash1, hash2 As Byte()
    Private hashcounter As Integer

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        OpenFileDialog1.InitialDirectory = System.IO.Path.GetDirectoryName(TextBox1.Text)
        OpenFileDialog1.Title = "Choose File 1"
        If Not OpenFileDialog1.ShowDialog() = DialogResult.Cancel Then
            TextBox1.Text = OpenFileDialog1.FileName
            Label1.Text = "Select files"
            Label1.ForeColor = Control.DefaultForeColor
        End If
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        OpenFileDialog1.InitialDirectory = System.IO.Path.GetDirectoryName(TextBox2.Text)
        OpenFileDialog1.Title = "Choose File 2"
        If Not OpenFileDialog1.ShowDialog() = DialogResult.Cancel Then
            TextBox2.Text = OpenFileDialog1.FileName
            Label1.Text = "Select files"
            Label1.ForeColor = Control.DefaultForeColor
        End If
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        ProgressBar1.Value = 0
        ProgressBar2.Value = 0

        If My.Computer.FileSystem.FileExists(TextBox1.Text) Then
            If My.Computer.FileSystem.FileExists(TextBox2.Text) Then

                Dim sr1 As New System.IO.FileStream(TextBox1.Text, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                Dim sr2 As New System.IO.FileStream(TextBox2.Text, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                If sr1.Length = sr2.Length Then
                    sr1.Close()
                    sr2.Close()
                    Label1.ForeColor = Control.DefaultForeColor
                    Label1.Text = "File sizes equal, computing sha512 hashes..."
                    Dim hasher1, hasher2 As Hasher
                    hasher1 = New Hasher
                    hasher2 = New Hasher
                    hasher1.threadNo = 1
                    hasher2.threadNo = 2
                    hasher1.filepath = TextBox1.Text
                    hasher2.filepath = TextBox2.Text
                    Dim thread1 As New System.Threading.Thread(AddressOf hasher1.hash)
                    Dim thread2 As New System.Threading.Thread(AddressOf hasher2.hash)
                    AddHandler hasher1.FinishedHashing, AddressOf FinishedHashingHandler
                    AddHandler hasher2.FinishedHashing, AddressOf FinishedHashingHandler
                    AddHandler hasher1.MadeProgress, AddressOf MadeProgressHandler
                    AddHandler hasher2.MadeProgress, AddressOf MadeProgressHandler
                    thread1.Start()
                    thread2.Start()
                Else
                    Label1.Text = "Files Differ (file size)"
                    Label1.ForeColor = Color.Red
                    sr1.Close()
                    sr2.Close()
                End If


            Else
                MsgBox("File 2 does not exist", MsgBoxStyle.Exclamation, "Error")
            End If
        Else
            MsgBox("File 1 does not exist", MsgBoxStyle.Exclamation, "Error")
        End If
    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Control.CheckForIllegalCrossThreadCalls = False
    End Sub

    Private Sub FinishedHashingHandler(ByVal threadNo As Integer, ByVal hash As Byte())

        If hashcounter = 0 Then
            hash1 = hash
            hashcounter = 1
        Else
            hash2 = hash
            hashcounter = 0
            If hash1.Length = hash2.Length Then
                For i As Integer = 0 To hash1.Length - 1
                    If hash1(i) <> hash2(i) Then
                        Label1.Text = "Files differ"
                        Label1.ForeColor = Color.Red
                        Exit Sub
                    End If
                Next i
                Label1.Text = "Files are equal"
                Label1.ForeColor = Color.Green
            Else
                Label1.Text = "Files differ"
                Label1.ForeColor = Color.Red
            End If
        End If
    End Sub

    Private Sub MadeProgressHandler(ByVal threadNo As Integer, ByVal progress As Long)
        If threadNo = 1 Then
            ProgressBar1.Value = CInt(progress)
        ElseIf threadNo = 2 Then
            ProgressBar2.Value = CInt(progress)
        Else
            Console.WriteLine("WTF?")
        End If
    End Sub

End Class

Public Class Hasher
    Public filepath As String
    Public threadNo As Integer
    Public Event FinishedHashing(ByVal threadNo As Integer, ByVal hash As Byte())
    Public Event MadeProgress(ByVal threadNo As Integer, ByVal progress As Long)
    Sub hash()
        Dim sr As New System.IO.FileStream(filepath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
        Dim sha As New System.Security.Cryptography.SHA512CryptoServiceProvider
        Dim blockSize As Integer
        blockSize = 8 * 1024 * 1024
        Dim offset, progress As Long
        offset = 0
        progress = 0
        Dim BufferI(blockSize) As Byte
        Dim BufferO(blockSize) As Byte
        While (sr.Length - offset >= blockSize)
            sr.Read(BufferI, 0, blockSize)
            offset += sha.TransformBlock(BufferI, 0, blockSize, BufferO, 0)
            progress = 100 * offset / sr.Length
            RaiseEvent MadeProgress(threadNo, progress)
        End While
        sr.Read(BufferI, 0, sr.Length - offset)
        sha.TransformFinalBlock(BufferI, 0, sr.Length - offset)
        RaiseEvent MadeProgress(threadNo, 100)

        sr.Close()
        Dim hash As Byte()
        hash = sha.Hash
        sha.Dispose()
        RaiseEvent FinishedHashing(threadNo, hash)
    End Sub
End Class
