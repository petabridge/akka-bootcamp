namespace GithubActors

module GithubClientFactory =

    open Octokit
    open Octokit.Internal

    let mutable oauthToken = ""

    let setOauthToken token = oauthToken <- token

    let getUnauthenticatedClient () =
        GitHubClient (ProductHeaderValue "AkkaBootcamp-Unit3")

    let getClient () =
         GitHubClient (ProductHeaderValue "AkkaBootcamp-Unit3", InMemoryCredentialStore (Credentials oauthToken))