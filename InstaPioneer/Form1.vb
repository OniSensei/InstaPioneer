Imports System.IO
Imports System.Net
Imports System.Text
Imports InstaSharper.API
Imports InstaSharper.API.Builder
Imports InstaSharper.Classes
Imports InstaSharper.Logger

Public Class Form1
    Public _instaApi As IInstaApi

    Private Sub Form1_LoadAsync(sender As Object, e As EventArgs) Handles MyBase.Load
        NsTextBox2.Text = My.Settings.minlikes
        NsTextBox3.Text = My.Settings.minfollowers
        NsTextBox4.Text = My.Settings.pagination
        NsTextBox5.Text = My.Settings.hourstart
        NsTextBox6.Text = My.Settings.minstart
        NsTextBox8.Text = My.Settings.hourstop
        NsTextBox7.Text = My.Settings.minstop

        If My.Settings.username <> "" Then
            LoadAccount()
        Else
            ToolStripStatusLabel1.Text = "Please enter user login information on 'Basic Settings' tab."
        End If
    End Sub

    Private Sub NsButton3_Click(sender As Object, e As EventArgs) Handles NsButton3.Click
        If NsListView1.Items.Count > 0 Then
            ToolStripStatusLabel1.Text = String.Format("Removing {0} items from the tag list.", NsListView1.Items.Count)

            For Each item As NSListView.NSListViewItem In NsListView1.Items
                NsListView1.RemoveItem(item)
            Next
        Else
            ToolStripStatusLabel1.Text = String.Format("Tag list contains {0} items.", NsListView1.Items.Count)
        End If
    End Sub

    Private Sub NsButton2_Click(sender As Object, e As EventArgs) Handles NsButton2.Click
        ToolStripStatusLabel1.Text = String.Format("Removing item from the tag list at index {0}.", NsListView1.SelectedItems.First)
        NsListView1.RemoveItem(NsListView1.SelectedItems.First)
        ToolStripStatusLabel1.Text = String.Format("Tag list contains {0} items.", NsListView1.Items.Count)
    End Sub

    Private Sub NsButton1_Click(sender As Object, e As EventArgs) Handles NsButton1.Click
        If NsTextBox1.Text <> "" Then
            ToolStripStatusLabel1.Text = String.Format("Adding {0} to the tag list.", NsTextBox1.Text)
            NsListView1.AddItem(NsTextBox1.Text)
            NsTextBox1.Text = ""
            ToolStripStatusLabel1.Text = String.Format("Tag list contains {0} items.", NsListView1.Items.Count)
        Else
            ToolStripStatusLabel1.Text = String.Format("Tag can not be '{0}'.", NsTextBox1.Text)
        End If
    End Sub

    Private Sub NsButton5_Click(sender As Object, e As EventArgs) Handles NsButton5.Click
        Dim sbuilder As New StringBuilder
        If NsListView1.Items.Count > 0 Then
            ToolStripStatusLabel1.Text = String.Format("Saving {0} items from the tag list.", NsListView1.Items.Count)
            For Each item As NSListView.NSListViewItem In NsListView1.Items
                sbuilder.Append(item.Text).AppendLine()
            Next
            File.WriteAllText(Application.StartupPath & "\tags.txt", sbuilder.ToString)
            ToolStripStatusLabel1.Text = String.Format("{0} items from the tag list saved successfully.", NsListView1.Items.Count)
        Else
            ToolStripStatusLabel1.Text = String.Format("Tag list contains {0} items.", NsListView1.Items.Count)
        End If
    End Sub

    Private Async Sub NsButton4_ClickAsync(sender As Object, e As EventArgs) Handles NsButton4.Click
        Dim curr As Date = Date.Now
        Dim startTime As New Date(curr.Year, curr.Month, curr.Day, 8, 0, 0)
        Dim endTime As New Date(curr.Year, curr.Month, curr.Day, 17, 0, 0)
        If (curr >= startTime) And (curr <= endTime) Then
            ToolStripStatusLabel1.Text = "Starting the auto liker..."
            NsListView2.AddItem("Starting the auto liker...")
            RunLikes()
        Else
            ToolStripStatusLabel1.Text = "Auto liker turned off..."
            NsListView2.AddItem("Auto liker turned off...")
        End If
    End Sub

    Private Sub NsButton6_Click(sender As Object, e As EventArgs) Handles NsButton6.Click
        My.Settings.minlikes = NsTextBox2.Text
        My.Settings.minfollowers = NsTextBox3.Text
        My.Settings.pagination = NsTextBox4.Text
        My.Settings.hourstart = NsTextBox5.Text
        My.Settings.minstart = NsTextBox6.Text
        My.Settings.hourstop = NsTextBox8.Text
        My.Settings.minstop = NsTextBox7.Text
        My.Settings.Save()

        ToolStripStatusLabel1.Text = "Settings saved..."
    End Sub

    Private Sub NsButton7_Click(sender As Object, e As EventArgs) Handles NsButton7.Click
        My.Settings.username = NsTextBox9.Text
        My.Settings.password = NsTextBox10.Text
        My.Settings.Save()

        ToolStripStatusLabel1.Text = "Settings saved..."

        If My.Settings.username <> "" Then
            LoadAccount()
        End If
    End Sub

    Public Async Sub RunLikes()
        If NsListView1.Items.Count > 0 Then
            ToolStripStatusLabel1.Text = String.Format("Tag list contains {0} items.", NsListView1.Items.Count)
            NsListView2.AddItem(String.Format("Auto like bot started... Loaded {0} items.", NsListView1.Items.Count))
            Dim mcount As Integer = NsListView1.Items.Count
            Dim m As Integer = 1

            For Each item As NSListView.NSListViewItem In NsListView1.Items
                Dim tagcomplete As Double = Math.Round(m / mcount * 100, 2)

                Dim tag As String = item.Text
                ToolStripStatusLabel1.Text = String.Format("Searching for media with #{0}... {1}/{2} tags {3}% complete...", item, m, mcount, tagcomplete)
                NsListView2.AddItem(String.Format("Searching for media with #{0}...", item))

                Dim tagfeed = Await _instaApi.GetTagFeedAsync(tag, PaginationParameters.MaxPagesToLoad(My.Settings.pagination))
                If tagfeed.Succeeded Then
                    Dim mediacount As Integer = tagfeed.Value.Medias.Count
                    Dim i As Integer = 1
                    For Each tagmedia In tagfeed.Value.Medias
                        Dim complete As Double = Math.Round(i / mediacount * 100, 2)
                        If tagmedia.HasLiked = False Then
                            If tagmedia.User.UserName <> My.Settings.username Then
                                If tagmedia.LikesCount >= My.Settings.minlikes Then
                                    Dim followcountraw = Await _instaApi.GetUserFollowersAsync(tagmedia.User.UserName, PaginationParameters.MaxPagesToLoad(My.Settings.pagination))

                                    If followcountraw.Value.Count >= My.Settings.minfollowers Then
                                        Await _instaApi.LikeMediaAsync(tagmedia.InstaIdentifier)
                                        ToolStripStatusLabel1.Text = String.Format("{0}/{1} {2}% complete... {3}/{4} tags {5}% complete...", i, mediacount, complete, m, mcount, tagcomplete)
                                        NsListView2.AddItem(String.Format("Liked image {0}...", tagmedia.InstaIdentifier))
                                    Else
                                        ToolStripStatusLabel1.Text = String.Format("{0}/{1} {2}% complete... {3}/{4} tags {5}% complete...", i, mediacount, complete, m, mcount, tagcomplete)
                                        NsListView2.AddItem(String.Format("Image below minimum follower requirement... Username: {0} | Followers: {1}", tagmedia.User.UserName, followcountraw.Value.Count))
                                    End If
                                Else
                                    ToolStripStatusLabel1.Text = String.Format("{0}/{1} {2}% complete... {3}/{4} tags {5}% complete...", i, mediacount, complete, m, mcount, tagcomplete)
                                    NsListView2.AddItem(String.Format("Image {0} below minimum like requirement...", tagmedia.InstaIdentifier))
                                End If
                            Else
                                ToolStripStatusLabel1.Text = String.Format("{0}/{1} {2}% complete... {3}/{4} tags {5}% complete...", i, mediacount, complete, m, mcount, tagcomplete)
                                NsListView2.AddItem(String.Format("Image {0} is our own...", tagmedia.InstaIdentifier))
                            End If
                        Else
                            ToolStripStatusLabel1.Text = String.Format("{0}/{1} {2}% complete... {3}/{4} tags {5}% complete...", i, mediacount, complete, m, mcount, tagcomplete)
                            NsListView2.AddItem(String.Format("Image {0} already liked...", tagmedia.InstaIdentifier))
                        End If
                        i += 1
                    Next
                    m += 1
                Else
                    ToolStripStatusLabel1.Text = String.Format("Error getting tag feed for #{0}...", item)
                    NsListView2.AddItem(String.Format("Error getting tag feed for #{0}...", item))
                End If
            Next
            Dim curr As Date = Date.Now
            Dim startTime As New Date(curr.Year, curr.Month, curr.Day, 8, 0, 0)
            Dim endTime As New Date(curr.Year, curr.Month, curr.Day, 17, 0, 0)
            If (curr >= startTime) And (curr <= endTime) Then
                ToolStripStatusLabel1.Text = "Restarting auto liker..."
                NsListView2.AddItem("Restarting auto liker...")
                RunLikes()
            Else
                ToolStripStatusLabel1.Text = "Auto liker turned off..."
                NsListView2.AddItem("Auto liker turned off...")
            End If
        Else
            ToolStripStatusLabel1.Text = String.Format("Tag list contains {0} items.", NsListView1.Items.Count)
        End If
    End Sub

    Public Async Sub LoadAccount()
        Dim userSession = New UserSessionData
        userSession.UserName = My.Settings.username
        userSession.Password = My.Settings.password

        Dim delay = RequestDelay.FromSeconds(2, 2)
        _instaApi = InstaApiBuilder.CreateBuilder().SetUser(userSession).UseLogger(New DebugLogger(LogLevel.Exceptions)).SetRequestDelay(delay).Build()
        Const stateFile As String = "state.bin"

        Try
            If File.Exists(stateFile) Then
                ToolStripStatusLabel1.Text = "Loading state from file..."
                Using fs = File.OpenRead(stateFile)
                    _instaApi.LoadStateDataFromStream(fs)
                End Using
                ToolStripStatusLabel1.Text = String.Format("Successfully logged in as {0}.", userSession.UserName)
            End If
        Catch ex As Exception
            Console.WriteLine(ex)
        End Try

        If Not _instaApi.IsUserAuthenticated Then
            ToolStripStatusLabel1.Text = String.Format("Logging in as {0}...", userSession.UserName)

            delay.Disable()
            Dim logInResult = Await _instaApi.LoginAsync()
            delay.Enable()

            If Not logInResult.Succeeded Then
                ToolStripStatusLabel1.Text = String.Format("Unable to login: {0}.", logInResult.Info.Message)
            Else
                ToolStripStatusLabel1.Text = String.Format("Successfully logged in as {0}.", userSession.UserName)
            End If

            Dim state = _instaApi.GetStateDataAsStream()

            Using fileStream = File.Create(stateFile)
                state.Seek(0, SeekOrigin.Begin)
                state.CopyTo(fileStream)
            End Using
        End If

        If File.Exists("tags.txt") Then
            ToolStripStatusLabel1.Text = "Loading tags from file..."
            For Each line As String In File.ReadLines("tags.txt")
                NsListView1.AddItem(line)
            Next
        End If

        ToolStripStatusLabel1.Text = String.Format("Loading dashboard for {0}...", userSession.UserName)

        Dim currentuser = Await _instaApi.GetCurrentUserAsync
        Dim tclient As WebClient = New WebClient
        Dim timage As Bitmap = Bitmap.FromStream(New MemoryStream(tclient.DownloadData(currentuser.Value.ProfilePicture)))
        PictureBox1.BackgroundImage = timage

        Label1.Text = currentuser.Value.UserName
        Dim followers = Await _instaApi.GetCurrentUserFollowersAsync(PaginationParameters.MaxPagesToLoad(50))
        Dim following = Await _instaApi.GetUserFollowingAsync(My.Settings.username, PaginationParameters.MaxPagesToLoad(50))

        NsLabel1.Value2 = followers.Value.Count
        NsLabel2.Value2 = following.Value.Count

        ToolStripStatusLabel1.Text = String.Format("Finished loading dashboard for {0}.", userSession.UserName)
    End Sub
End Class
