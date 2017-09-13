namespace GithubActors

open System
open System.Drawing
open System.Windows.Forms
open System.ComponentModel
open Akka.Actor
open Akka.FSharp

[<AutoOpen>]
module RepoResultsForm =

    let generateRandomActorName () =
        Guid.NewGuid().ToString().[0..7]
        |> sprintf "repoResults%s"

    let createNew (repoKey: RepoKey) (coordinator: IActorRef) =
        
        let formTitle = sprintf "Repos similar to %s / %s" repoKey.Owner repoKey.Repo
        let form = new Form(Name = "RepoResultsForm", Visible = false, Text = formTitle, AutoScaleDimensions = SizeF(6.F, 13.F), AutoScaleMode = AutoScaleMode.Font, ClientSize = Size(740, 322))
        
        let dgUsers = new DataGridView()
        let statusStrip = new StatusStrip()
        let progressBar = new ToolStripProgressBar()
        let lblStatus = new ToolStripStatusLabel()

        (dgUsers :> ISupportInitialize).BeginInit ()
        statusStrip.SuspendLayout ()        
        form.SuspendLayout ()

        dgUsers.Name <- "dgUsers"
        dgUsers.AllowUserToOrderColumns <- true
        dgUsers.ColumnHeadersHeightSizeMode <- DataGridViewColumnHeadersHeightSizeMode.AutoSize
        let columns : DataGridViewColumn [] = [|
            new DataGridViewTextBoxColumn(HeaderText = "Owner", Name = "colOwner") // owner
            new DataGridViewTextBoxColumn(HeaderText = "Repo Name", Name = "colRepoName") // repo name
            new DataGridViewLinkColumn(HeaderText = "URL", Name = "colUrl", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill) // URL
            new DataGridViewTextBoxColumn(HeaderText = "Shared", Name = "colShared") // shared
            new DataGridViewTextBoxColumn(HeaderText = "Issues", Name = "colIssues") // issues
            new DataGridViewTextBoxColumn(HeaderText = "Stars", Name = "colStars") // stars
            new DataGridViewTextBoxColumn(HeaderText = "Forks", Name = "colForks") // forks
        |]
        dgUsers.Columns.AddRange columns
        dgUsers.Dock <- DockStyle.Fill
        dgUsers.TabIndex <- 0
        dgUsers.Size <- Size(740, 322)
        dgUsers.Location <- Point(0, 0)

        statusStrip.Name <- "statusStrip"
        statusStrip.Text <- "status"
        let items : ToolStripItem [] = [| progressBar; lblStatus |]
        statusStrip.Items.AddRange items
        statusStrip.Location <- Point(0, 300)
        statusStrip.Size <- Size(740, 22)
        statusStrip.TabIndex <- 1

        progressBar.Name <- "progressBar"
        progressBar.Size <- Size(100, 16)
        progressBar.Visible <- false

        lblStatus.Name <- "lblStatus"
        lblStatus.Text <- "Processing..."
        lblStatus.Size <- Size(72, 16)
        lblStatus.Visible <- false
                
        form.Controls.Add statusStrip
        form.Controls.Add dgUsers

        (dgUsers :> ISupportInitialize).EndInit ()
        statusStrip.ResumeLayout false
        statusStrip.PerformLayout ()
        form.ResumeLayout false
        form.PerformLayout ()
        
        let actorName = generateRandomActorName ()
        let repoResultsActor = spawnOpt ActorSystem.githubActors actorName (Actors.repoResultsActor dgUsers lblStatus progressBar) [SpawnOption.Dispatcher "akka.actor.synchronized-dispatcher"]
        coordinator <! SubscribeToProgressUpdates repoResultsActor

        form.FormClosing.Add (fun _ -> 
            //
            repoResultsActor <! PoisonPill.Instance)
              
        form