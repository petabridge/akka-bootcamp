namespace GithubActors

open System
open System.Drawing
open System.Windows.Forms
open System.Diagnostics
open Akka.FSharp

[<AutoOpen>]
module GithubAuthForm =
    let boldFont = new Font("Microsoft Sans Serif", 11.25f, FontStyle.Bold, GraphicsUnit.Point)
    let regularFont = new Font("Microsoft Sans Serif", 11.25f, FontStyle.Regular, GraphicsUnit.Point)
    
    let form = new Form(Name = "GithubAuthForm", Visible = true, Text = "Sign in to GitHub", AutoScaleDimensions = SizeF(6.F, 13.F), AutoScaleMode = AutoScaleMode.Font, ClientSize = Size(582, 155))
    let lblAccessToken = new Label(Name = "lblAccessToken", Text = "GitHub Access Token", Size = Size(172, 18), Location = Point(12, 8), TabIndex = 0, Font = boldFont, AutoSize = true)
    let txtOAuth = new TextBox(Name = "txtOAuth", Size = Size(380, 24), Location = Point(190, 6), TabIndex = 1, Font = regularFont)
    let lblAuthStatus = new Label(Name = "lblAuthStatus", Text = "status", Size = Size(88, 18), Location = Point(188, 32), TabIndex = 2, Font = regularFont, Visible = false, AutoSize = true)
    let lblLinkGitHub = new LinkLabel(Name = "lblLinkGitHub", Text = "How to get a GitHub Access Token", Size = Size(272, 18), Location = Point(148, 128), TabIndex = 3, Font = boldFont, AutoSize = true)
    let btnAuthenticate = new Button(Name = "btnAuthenticate", Text = "Authenticate", Size = Size(136, 32), Location = Point(214, 80), TabIndex = 4, Font = boldFont, UseVisualStyleBackColor = true)

    form.SuspendLayout ()

    form.Controls.Add lblAccessToken
    form.Controls.Add txtOAuth
    form.Controls.Add lblAuthStatus
    form.Controls.Add lblLinkGitHub
    form.Controls.Add btnAuthenticate

    form.ResumeLayout false

    let load () =
        let launcherForm = LauncherForm.load ()
        let authenticator = spawn ActorSystem.githubActors "authenticator" (Actors.githubAuthenticationActor lblAuthStatus form launcherForm)

        lblLinkGitHub.LinkClicked.Add (fun _ -> Process.Start "https://help.github.com/articles/creating-an-access-token-for-command-line-use/" |> ignore)
        btnAuthenticate.Click.Add (fun _ ->
            if (String.IsNullOrEmpty txtOAuth.Text)
                then
                    lblAuthStatus.Text <- "Please enter a token."
                    lblAuthStatus.Visible <- true
                    lblAuthStatus.ForeColor <- Color.Orange
                else authenticator <! Authenticate txtOAuth.Text)

        form