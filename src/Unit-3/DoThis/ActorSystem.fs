namespace GithubActors

open Akka.FSharp

module ActorSystem =
    let githubActors = System.create "GithubActors" (Configuration.load())