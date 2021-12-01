#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: FSharp.Json"
// #r "nuget: FSharp.Core, 6.0.1"
#load "./Constants.fsx"

open System
open System.Text
open Akka.Actor
open Akka.FSharp
open Akka.Configuration
open System.Text
open FSharp.Json
// open Constants.Constants

// let configuration =
//     ConfigurationFactory.ParseString(
//         @"akka {
//             actor {
//                 provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
//                 debug : {
//                     receive : on
//                     autoreceive : on
//                     lifecycle : on
//                     event-stream : on
//                     unhandled : on
//                 }
//             }
//             remote {
//                 helios.tcp {
//                     port = 9090
//                     hostname = 10.20.115.11
//                 }
//             }
//         }"
//     )

let config = 
    ConfigurationFactory.ParseString(
        @"akka {            
            log-dead-letters = 0
            log-dead-letters-during-shutdown = off
        }")
    
let system = ActorSystem.Create("TwitterClone",config)

let mutable numNodes = fsi.CommandLineArgs.[1] |> int
let tweets = fsi.CommandLineArgs.[2] |> int

let random = System.Random()

let userMessage msg userid= 
    let selectedUser = select ("akka.tcp://TwitterClone@127.0.0.1:9002/user/User_" + (userid |> string)) system
    let message = Json.serialize(msg)
    selectedUser <! message

let engineMessage msg= 
    let engineActor = select ("akka.tcp://TwitterClone@127.0.0.1:9001/user/engine") system
    let message = Json.serialize(msg)
    engineActor <! message

let constructTweet = 
    let mutable str = String.Empty
    let tweetCount = Constants.Constants.tweets
    let hashCount = Constants.Constants.hashtags
    let rnd1 = random.Next(tweetCount.Length)
    let rnd2 = random.Next(hashCount.Length)
    str <- tweetCount.[rnd1]
    str <- str + " " + hashCount.[rnd2]
    str <- str + " @User_" + (random.Next(numNodes-1) |> string)
    str

type apiComm = {
    // reqId: String
    userId: String
    content: String
    query: String
}


let User (userid: int) (mailbox: Actor<_>) = 
    // let id = userid
    let mutable reqList = []
    let awayTweets = []
    let mutable myTweets = []
    let mutable myTweetsCount = 0
    let timer = Diagnostics.Stopwatch()
    let mutable timerState = 0.0
    let mutable supervisorRef = mailbox.Self
    let id = mailbox.Self.Path.Name.Split("_").[1] |> int
    let mutable status = "online"

    let rec loop () =
        actor {
            let! jsonMessage = mailbox.Receive()
            let message = Json.deserialize jsonMessage
            Console.WriteLine(message.ToString())
            let action = message.query

            if timer.Elapsed.TotalSeconds - timerState > 1.0 && status = "offline" then

                let payload = {

                    // reqId = "1234"
                    userId = id |> string
                    content = ""
                    query = "UpdateFeed"

                }

                engineMessage payload
                status <- "online"
                timerState <- timer.Elapsed.TotalSeconds
            
            match action with
            | "Login" ->    //let Guid = Guid.NewGuid()
                            let apiComm = {
                                // reqId = Guid.ToString()
                                userId = userid |> string
                                content = ""
                                query = "Login"
                            }
                            engineMessage apiComm
                            // Console.WriteLine(apiComm)

            | "Register" -> Console.WriteLine("%s has been registered", userid)
                            //let Guid = Guid.NewGuid()
                            let apiComm = {
                                // reqId = Guid.ToString()
                                userId = userid |> string
                                content = ""
                                query = "SignUp"
                            }
                            engineMessage apiComm
                            // Console.WriteLine(apiComm)

            | "Subscribe" -> //let Guid = Guid.NewGuid()
                             let apiComm = {
                                // reqId = Guid.ToString()
                                userId = userid |> string
                                content = ""
                                query = "Subscribe"
                                }
                             engineMessage apiComm
                            // Console.WriteLine(apiComm)

            | "Tweet" ->    let liveTweet = constructTweet
                            Console.WriteLine("User %s tweeted %s", id.ToString liveTweet)
                            myTweets <- List.append myTweets [liveTweet]
                            myTweetsCount <- myTweetsCount + 1
                            //let Guid = Guid.NewGuid()
                            let apiComm = {
                                // reqId = Guid.ToString()
                                userId = userid |> string
                                content = liveTweet
                                query = "Tweet"
                                }
                            engineMessage apiComm

            
            | "Retweet" ->  //let Guid = Guid.NewGuid()
                            let apiComm = {
                                // reqId = Guid.ToString()
                                userId = userid |> string
                                content = ""
                                query = "Retweet"
                            }
                            engineMessage apiComm
                            // Console.WriteLine(apiComm)

            | "Logout" -> status <- "offline"
            // | "Tweet" -> 
            | _ -> ignore()

            return! loop ()
        }
    loop()

let Supervisor (numNodes: int) (tweets: int) (mailbox: Actor<_>) = 
    let currentNodes = numNodes
    let mutable nodesList = []
    let numTweets = tweets
    let rec loop () =
        actor {
            let! message = mailbox.Receive()

            match message with
            | "Initiate" -> nodesList <- [ for i in 1 .. currentNodes do yield (spawn system ("User_" + string (i))) (User i) ]
                            Console.WriteLine(nodesList)
                            // let Guid = Guid.NewGuid()
                            let payload = {
                                // reqId = Guid.ToString() 
                                userId = ""
                                content = ""
                                query = "Register"
                            }

                            for j in nodesList do
                                userMessage payload j
                                Console.WriteLine(j)

            | _ -> ignore()

            return! loop ()
        }
    loop()

let supervisorRef = spawn system "supervisorRef" (Supervisor numNodes tweets)
supervisorRef <! "Initiate"