namespace GithubActors

open System.Drawing
open System.Windows.Forms
open Akka.FSharp

[<AutoOpen>]
module LauncherForm =
    let boldFont = new Font("Microsoft Sans Serif", 11.25f, FontStyle.Bold, GraphicsUnit.Point)
    let regularFont = new Font("Microsoft Sans Serif", 11.25f, FontStyle.Regular, GraphicsUnit.Point)
    
    let form = new Form(Name = "LauncherForm", Visible = false, Text = "Launcher", AutoScaleDimensions = SizeF(6.F, 13.F), AutoScaleMode = AutoScaleMode.Font, ClientSize = Size(582, 155))
    let lblRepo = new Label(Name = "lblRepo", Text = "Repo URL", Size = Size(86, 18), Location = Point(3, 16), TabIndex = 0, Font = boldFont, AutoSize = true)
    let txtRepoUrl = new TextBox(Name = "txtRepoUrl", Size = Size(456, 24), Location = Point(96, 13), TabIndex = 1, Font = regularFont)
    let lblIsValid = new Label(Name = "lblIsValid", Text = "isValid", Size = Size(46, 18), Location = Point(96, 44), TabIndex = 2, Font = regularFont, Visible = false, AutoSize = true)
    let btnLaunch = new Button(Name = "btnLaunch", Text = "GO", Size = Size(142, 32), Location = Point(218, 90), TabIndex = 4, Font = boldFont, UseVisualStyleBackColor = true)

    form.SuspendLayout ()

    form.Controls.Add lblRepo
    form.Controls.Add txtRepoUrl
    form.Controls.Add lblIsValid
    form.Controls.Add btnLaunch

    form.ResumeLayout false

    let load () =

        let createRepoResultsForm (repoKey: RepoKey) coordinator =
            RepoResultsForm.createNew repoKey coordinator
            
        let mainFormActor = spawn ActorSystem.githubActors "mainform" (Actors.mainFormActor lblIsValid createRepoResultsForm)
        let validator = spawn ActorSystem.githubActors "validator" (Actors.githubValidatorActor GithubClientFactory.getClient)
        let commander = spawn ActorSystem.githubActors "commander" (Actors.githubCommanderActor)
        
        btnLaunch.Click.Add (fun _ -> mainFormActor <! ProcessRepo txtRepoUrl.Text)
        form.FormClosing.Add (fun _ -> Application.Exit ())

        form