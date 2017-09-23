# Lesson 3.4: How to perform work asynchronously inside your actors using `PipeTo`

One of the first questions developers ask once they learn [how Akka.NET actors work](http://petabridge.com/blog/akkadotnet-what-is-an-actor/) is&hellip;

> "If actors can only process one message at a time, can I still use F# `async` workflows or `Task<T>` objects inside my actors?"

*Yes!* You can still use asynchronous functions and `Task<T>` objects inside your actors - using the `PipeTo` pattern!

This lesson will show you how.

Before going further, remember that F# asynchronous workflows produce an `Async<'T>` construct that needs to be run via a subsequent call to either `Async.RunSynchronously`, `Async.StartAsTask`, etc...This in turn will create a TPL `Task<T>` that will be handled by the .NET runtime. It is important to know that `Async<'T>` is not the same as `Task<T>` and that doing asynchronous work in F# will require knowledge about both. In the rest of the lesson we'll mostly use the term `Task<T>` to represent asynchronous operations.

## Key Concepts / Background
"But wait!", you say. "Aren't actors already asynchronous?"

Indeed they are, and you make an astute point! Due to the nature of passing immutable messages between actors, actors are inherently thread-safe and asynchronous (they don't block each other).

But what if you want to do some asynchronous work from within an actor itself, such as kick off a long-running HTTP request via a `Task`?

In C#, most developers would default to using `await`, which has achieved demigod status since its release in 2012.
In F#, you would probably go with a combination of an [asynchronous workflow](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/asynchronous-workflows) run by `Async.StartAsTask`.

*And they would be making the wrong choice.*

Why? To answer that, we need to review how actors process messages.

### Actors process messages one at a time
Actors process the contents of their mailbox one message at a time. It looks like this:

![Animation - Akka.NET actors processing messages in their mailbox](images/how-akkadotnet-actors-receive-messages.gif)

Why is maintaining this behavior critical?

Recall that immutable messages themselves are inherently thread-safe, since a different thread can't modify something that is immutable.

***BUT: while the messages are inherently thread-safe, the message-processing code has no such guarantee!***

Processing one message at a time is critical because making sure an actor's message processing code (`OnReceive`) can only be run *one invocation at a time* is how Akka.NET enforces thread-safety for all of the code that executes inside an actor.

An immutable message is pushed from the mailbox into `OnReceive`. Once the call to `OnReceive` exits, the actor's mailbox pushes a new message into the actor's `OnReceive` method.

That being said, it's still possible to take advantage of `async` methods and methods that return `Task<T>` objects inside the `OnReceive` method - you just have to use the `PipeTo` extension method!

### Async message processing using `PipeTo`
The [`PipeTo` pattern](https://github.com/akkadotnet/akka.net/blob/dev/src/core/Akka/Actor/PipeToSupport.cs) is a simple [extension method](https://msdn.microsoft.com/en-us/library/bb383977.aspx) built into Akka.NET that you can append to any `Task<T>` object. This is how `PipeTo` looks in C#:

```csharp
public static Task PipeTo<T>(this Task<T> taskToPipe, ICanTell recipient, ActorRef sender = null)
```

But for the F# folks Akka.NET also comes with a nice `PipeTo` operator: **`|!>`** (or its backward equivalent **`<!|`**)
Contrary to its C# cousin though, the F# `PipeTo` lives in the world of `Async<'T>` (and not `Task<T>`)!

Be aware that those custom operators don't allow you to specify the sender (it will be set to `ActorRefs.NoSender` by default). If you want to pass in a sender, you can still use the equivalent F# `pipeTo` function and map it to your own operator.

### `Task`s are just another source of messages
The goal behind `PipeTo` is to ***treat every async operation just like any other method that can produce a message for an actor's mailbox***.

THAT is the right way to think about actors and concurrent `Task<T>`s in Akka.NET. A `Task<T>` is not something you `await` on in Akka.NET. It's *just something else that produces a message* for an actor to process through its mailbox.

The `pipeTo` function takes an `ICanTell` object as a required argument, which tells the method where to pipe the results of an asynchronous `Task<T>`.

Here are all of the Akka.NET classes that you can use with `ICanTell`:

* `ActorRef` - a reference to an actor instance.
* `ActorSelection` - a selection of actors at a specified address. This is what gets returned whenever you look up an actor based on its path.

Most of the time, you're going to want to have your actors pipe the results of a task back to themselves. Here's an example of a real-world use case for `PipeTo`, drawn from our **[official Akka.NET PipeTo code sample](https://github.com/petabridge/akkadotnet-code-samples/tree/master/PipeTo "Petabridge Akka.NET PipeTo code sample")**.

```csharp
// time to kick off the feed parsing process, and send the results to this same actor
Receive<BeginProcessFeed>(feed =>
{
    SendMessage(string.Format("Downloading {0} for RSS/ATOM processing...", feed.FeedUri));
    _feedFactory.CreateFeedAsync(feed.FeedUri).PipeTo(Self);
});
```

and here is the F# equivalent:

```fsharp
match message with
| BeginProcessFeed feed ->
    SendMessage <| sprintf "Downloading %O for RSS/ATOM processing..." feed.FeedUri
    _feedFactory.CreateFeedAsync feed.FeedUri
    |> Async.AwaitTask
    |!> mailbox.Self
```

The `|!>` operator expects an `Async<'T>` while CreateFeedAsync returns a `System.Threading.Task<T>`. This is why we need to use `Async.AwaitTask` to make the conversion from the former to the latter.

[View the full source for this example.](https://github.com/petabridge/akkadotnet-code-samples/blob/master/PipeTo/src/PipeTo.App/Actors/FeedParserActor.cs#L70).

Whenever you kick off a `Task<T>` and use `PipeTo` to deliver the results to some `ActorRef` or `ActorSelection`, here's how your actor is really processing its mailbox.

![Animation - Akka.NET actors processing messages asynchronously in their mailbox using PipeTo](images/how-akkadotnet-actors-receive-messages-async-pipeto.gif)

In this case we're using `PipeTo` to send the results back to itself, but you can just as easily send these results to different actor.

***The important thing to notice in this animation is that the actor continues processing other messages while the asynchronous operation is happening***.

That's why `PipeTo` is great for allowing your actors to parallelize long-running tasks, like HTTP requests.

### Composing `Task<T>` instances using `ContinueWith` and `PipeTo`
Have some post-processing you need to do on a `Task<T>` before the result gets piped into an actor's mailbox? No problem - you can still use `ContinueWith` and all of the other TPL design patterns you used in procedural C# programming.

In fact, this is exactly what we do in our `githubAuthenticationActor`! Have a look at the sample below:

```fsharp
| Authenticate token ->
    showAuthenticatingStatus ()
    let client = GithubClientFactory.getUnauthenticatedClient ()
    client.Credentials <- Octokit.Credentials token
    
    let continuation (task: System.Threading.Tasks.Task<Octokit.User>) : AuthenticationMessage =
        match task.IsFaulted with
        | true -> AuthenticationFailed
        | false ->
            match task.IsCanceled with
            | true -> AuthenticationCancelled
            | false ->
                GithubClientFactory.setOauthToken token
                AuthenticationSuccess
        
    client.User.Current().ContinueWith continuation
    |> Async.AwaitTask
    |!> mailbox.Self
```

So in this case, we're trying to download a GitHub user using our own auth token via the Octokit client inside the actor. We want to check the status of the task before we use `|!>` to deliver a message back to the actor.

We write all the task status handling code inside a `ContinueWith` block and use that to pipe `AuthenticationFailed`. `AuthenticationCancelled` or `AuthenticationSuccess` to the actor. Pretty easy!

### Why is `await` an Anti-pattern inside actors?

#### `await` is not magic, and breaks the core message processing guarantees
While `await` is a powerful and convenient construct, it isn't magic. It's just syntactic sugar for TPL continuation. If this is confusing or unfamiliar, we highly recommend reviewing [Stephen Cleary's excellent Async/Await primer](http://blog.stephencleary.com/2012/02/async-and-await.html).

`await` does two key things which break the core message processing guarantees of Akka.NET:

1. Exits the containing `async` function, while
2. Sets a continuation point in the containing method where the asynchronous `Task` (the `awaitable`) will return to and continue executing once it is done with its async work.

These actions by `await` have two negative effects:

First, `await` makes it harder to reason about exactly what is happening and on which thread. `ContinueWith` (which `await` is just syntactic sugar for anyway) makes it explicit and clear what is happening, and on which thread it's happening.

As we've discussed, an actor's mailbox pushes messages into the actor's `OnReceive` method as soon as the previous iteration of the `OnReceive` function exits. Whenever you `await` an `async` operation inside the `OnReceive` method, *you prematurely exit* the `OnReceive` method and the mailbox will push a new message into it, which is generally not what is intended.

Second, `await` breaks the "actors process one message at a time" guarantee. By the time the `await`ed `Task` returns and starts referencing things in the `ActorContext`, that context will have changed because the actor has moved on from the original message that `await`ed. Variables such as the `Sender` of the previous message may be different, or the actor might even be shutting down when the `await` call returns to the previous context.

***So don't use `await` inside your actors.*** `Await` is evil inside an actor. `Await` is just syntactic sugar anyway. Use `ContinueWith` instead, and pair it with `PipeTo`.

This will turn the results of `async` operations into messages that get delivered to your actor's mailbox and you can take advantage of `Task` and other TPL methods just as you did before, and you'll enjoy nicely parallel processing!

### Do I need to worry about closing over (closures) my actor's internal state when using `PipeTo`?
**Yes**, you need to close over *any state whose value might change between messages* that you need to use inside your `ContinueWith` or `PipeTo` calls.

This usually means closing over the `Sender` property and any private state you've defined that is likely to change between messages.

For instance, the `Sender` property of your actor will definitely change between messages. You'll need to use a closurefor this property in order to guarantee that any asynchronous methods that depend on this property get the right value.

Doing a closure is as simple as stuffing the property into an new variable (`let`) and using that instance variable in your `PipeTo` call, instead of the field or property defined on your actor. To illustrate this, let's have a look at the sample below where several `sender` actors trigger long-running asynchronous operations (simulated by `Async.Sleep`) in a single `processor` actor:

```fsharp
open System
open Akka.Actor
open Akka.FSharp

// helper to allow piping in the async computation
let pipeToWithSender recipient sender asyncComp = pipeTo asyncComp recipient sender

type Message =
    | Duration of int
    | Response of string

let senderActor (processor: IActorRef) (mailbox: Actor<_>) =
    let rec processMessage () = actor {
        let! message = mailbox.Receive ()
        
        match message with
        | Duration duration ->
            printfn "%s job's duration: %is" mailbox.Self.Path.Name duration
            processor <! Duration(duration)
        | Response response ->
            printfn "%s got response: %s" mailbox.Self.Path.Name response

        return! processMessage ()
    }
    processMessage ()

let processorActor (mailbox: Actor<_>) =
    let rec processMessage () = actor {
        let! message = mailbox.Receive ()
        printfn "processor received message, sender is %s" mailbox.Context.Sender.Path.Name

        match message with
        | Duration duration ->
            async {
                printfn "[%s] starting job for %s" (DateTime.Now.ToString("ss.mmm")) mailbox.Context.Sender.Path.Name
                do! Async.Sleep (duration * 1000)
                printfn "[%s] ending job for %s" (DateTime.Now.ToString("ss.mmm")) mailbox.Context.Sender.Path.Name
                return Response mailbox.Context.Sender.Path.Name
            }
            |> pipeToWithSender mailbox.Self mailbox.Context.Sender
        | Response response ->
            mailbox.Context.Sender <! Response(response)

        return! processMessage ()
    }
    processMessage ()

let system = System.create "system" <| Configuration.load()
let processor = spawn system "processor" processorActor
let sender1 = spawn system "sender1" (senderActor processor)
let sender2 = spawn system "sender2" (senderActor processor)

sender1 <! Duration(2)
sender2 <! Duration(3)
```

The interesting bit is here, where the sender is passed as `mailbox.Context.Sender`:

```fsharp
| Duration duration ->
    async {
        printfn "[%s] starting job for %s" (DateTime.Now.ToString("ss.mmm")) mailbox.Context.Sender.Path.Name
        do! Async.Sleep (duration * 1000)
        printfn "[%s] ending job for %s" (DateTime.Now.ToString("ss.mmm")) mailbox.Context.Sender.Path.Name
        return Response mailbox.Context.Sender.Path.Name
    }
    |> pipeToWithSender mailbox.Self mailbox.Context.Sender
```
This returns the following invalid results (look at the lines with NOK):

```
sender1 job's duration: 2s
sender2 job's duration: 3s
processor received message, sender is sender1
[17.29] starting job for sender1
processor received message, sender is sender2
[17.29] starting job for sender2
[19.29] ending job for sender2 ------> NOK, this should be sender1 (after 2s)
processor received message, sender is sender1
sender1 got response: sender2  ------> NOK, this should be sender1
[20.29] ending job for sender1 ------> NOK, this should be sender2 (after 3s)
processor received message, sender is sender2
sender2 got response: sender1  ------> NOK, this should be sender2
```

However, if we close over `mailbox.Context.Sender` as below:

```fsharp
| Duration duration ->
    let sender = mailbox.Context.Sender // close over sender
    async {
        printfn "[%s] starting job for %s" (DateTime.Now.ToString("ss.mmm")) sender.Path.Name // use closure here
        do! Async.Sleep (duration * 1000)
        printfn "[%s] ending job for %s" (DateTime.Now.ToString("ss.mmm")) sender.Path.Name // use closure here
        return Response sender.Path.Name // use closure here
    }
    |> pipeToWithSender mailbox.Self sender // use closure here
```

we now obtain the expected results:

```
sender1 job's duration: 2s
sender2 job's duration: 3s
processor received message, sender is sender1
[23.21] starting job for sender1
processor received message, sender is sender2
[23.21] starting job for sender2
[25.21] ending job for sender1 ------> OK, timing is correct
processor received message, sender is sender1
sender1 got response: sender1  ------> OK, correct sender
[26.21] ending job for sender2 ------> OK, timing is correct
processor received message, sender is sender2
sender2 got response: sender2  ------> OK, correct sender
```

> NOTE: Assuming you're piping the result of the `Task` back to the same actor, you don't need to close over `Self` or `Parent`. Those `ActorRef`s will be the same when the `Task` returns. You just need to close over the state that is going to change by the time the `Task` completes and executes its continuation delegate.

Now, let's get to work and use this powerful parallelism technique inside our actors!

## Exercise

Currently our `githubWorkerActor` instances all block when they're waiting for responses back from the GitHub API, using the following code:

```fsharp
let starredRepos =
    githubClient.Value.Activity.Starring.GetAllForUser (login)
    |> Async.AwaitTask
    |> Async.RunSynchronously

mailbox.Context.Sender <! StarredReposForUser(login, starredRepos)
```

We're going to leverage the full power of the TPL and allow each of our `githubWorkerActor` instances kick off multiple parallel Octokit queries at once, and then use `PipeTo` to asynchronously deliver the completed results back to our `githubCoordinatorActor`.

Take note - this the current speed of our GitHub scraper at the end of lesson 2:

![GtihubActors at the end of lesson 2](../lesson2/images/lesson2-after.gif)

### Phase 1 - Update `githubWorkerActor`

Open up `Actors.fs`and replace the core of `githubWorkerActor`with the following code:

```fsharp
| QueryStarrer login ->
    let sender = mailbox.Context.Sender // closure over the sender

    // continuation that returns either a failure message (RetryableQuery) or a success message (StarredReposForUser)
    let continuation (task: System.Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Octokit.Repository>>) : GithubActorMessage =
        if task.IsFaulted || task.IsCanceled then
            RetryableQuery(nextTry query) 
        else
            StarredReposForUser(login, task.Result)

    githubClient.Value.Activity.Starring.GetAllForUser(login).ContinueWith continuation
    |> Async.AwaitTask
    |!> sender
| QueryStarrers repoKey ->
    let sender = mailbox.Context.Sender

    let continuation (task: System.Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Octokit.User>>) : GithubActorMessage =
        if task.IsFaulted || task.IsCanceled then
            RetryableQuery(nextTry query) 
        else
            task.Result |> Seq.toArray |> UsersToQuery // returns the list of users

    githubClient.Value.Activity.Starring.GetAllStargazers(repoKey.Owner, repoKey.Repo).ContinueWith continuation
    |> Async.AwaitTask
    |!> sender
// rest of the actor...
```

That's it!

### Once you're done

Build and run `GithubActors.sln` - the performance should be *really fast* now.

![GithubActors performance after lesson 4](images/lesson4-after.gif)

**At the start of the lesson, it took us 4 seconds to download our first 4 users** for https://github.com/petabridge/akka-bootcamp. **At the end of the lesson we downloaded 22 users in 4 seconds**. All of this without adding any new actors or doing anything other than just letting the TPL work in concert via `PipeTo`.

> **NOTE:** The GitHub API appears to be *really* slow for a handful of users on every repository we've tested. We have no idea why.

## Great job!

Awesome - now you can use `Task<T>` instances in combination with your actors for maximum concurrency! Hooray!

**Now it's time to move onto the final lesson: [Lesson 5 - How to prevent deadlocks with `ReceiveTimeout`](../lesson5).**

## Further reading
See our [full Akka.NET `PipeTo` sample in C#](https://github.com/petabridge/akkadotnet-code-samples/blob/master/PipeTo/).

## Any questions?
**Don't be afraid to ask questions** :).

Come ask any questions you have, big or small, [in this ongoing Bootcamp chat with the Petabridge & Akka.NET teams](https://gitter.im/petabridge/akka-bootcamp).

### Problems with the code?
If there is a problem with the code running, or something else that needs to be fixed in this lesson, please [create an issue](/issues) and we'll get right on it. This will benefit everyone going through Bootcamp.
