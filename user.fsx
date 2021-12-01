#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: FSharp.Json"
// #r "nuget: FSharp.Core, 6.0.1"
#load "./Constants.fsx"
// #r "nuget: FSharp.Core, 6.0.1"

open System
open System.Text
open Akka.Actor
open Akka.FSharp
open Akka.Configuration
open System.Text
open FSharp.Json
// open Constants.Constants

let configuration =
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
            }
            remote {
                helios.tcp {
                    port = 9090
                    hostname = 10.20.115.11
                }
            }
        }"
    )

let config = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                
            }
            remote {
                helios.tcp {
                    port = 9092
                    hostname = 127.0.0.1
                }
            }
        }")
    
let system = ActorSystem.Create("TwitterClone",config)

let mutable numNodes = fsi.CommandLineArgs.[1] |> int
let tweets = fsi.CommandLineArgs.[2] |> int

let random = System.Random()

let userMessage msg userid= 
    let selectedUser = select ("akka.tcp://TwitterClone@127.0.0.1:9092/user/User_" + (userid |> string)) system
    let message = Json.serialize(msg)
    selectedUser <! message

let engineMessage msg= 
    let engineActor = select ("akka.tcp://TwitterClone@127.0.0.1:9091/user/Server") system
    let message = Json.serialize(msg)
    engineActor <! message

// let constructTweet = 
//     let mutable str = String.Empty
//     let tweetCount = Constants.Constants.tweets
//     let hashCount = Constants.Constants.hashtags
//     let rnd1 = random.Next(tweetCount.Length)
//     let rnd2 = random.Next(hashCount.Length)
//     str <- tweetCount.[rnd1]
//     str <- str + " " + hashCount.[rnd2]
//     str <- str + " @User_" + (random.Next(numNodes-1) |> string)
//     str

type apiComm = {
    // reqId: String
    userId: String
    content: String
    query: String
}

type Communication = 
    | Initiate of string



let User (mailbox: Actor<_>) = 
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
            Console.WriteLine("Before") //+ jsonMessage.ToString())
            let message = Json.deserialize<apiComm> jsonMessage
            Console.WriteLine("Hello") //+ message.ToString())
            let action = message.query

            if timer.Elapsed.TotalSeconds - timerState > 1.0 && status = "offline" then

                let payload = {

                    // reqId = "1234"
                    userId = id |> string
                    content = ""
                    query = "Login"

                }

                engineMessage payload
                status <- "online"
                timerState <- timer.Elapsed.TotalSeconds
            
            match action with
            | "Login" ->    //let Guid = Guid.NewGuid()
                            let apiComm = {
                                // reqId = Guid.ToString()
                                userId = id |> string
                                content = ""
                                query = "Login"
                            }
                            engineMessage apiComm
                            // Console.WriteLine(apiComm)

            | "Register" -> Console.WriteLine(id.ToString() + " has been registered")
                            //let Guid = Guid.NewGuid()
                            let apiComm = {
                                // reqId = Guid.ToString()
                                userId = id |> string
                                content = ""
                                query = "SignUp"
                            }
                            engineMessage apiComm
                            // Console.WriteLine(apiComm)

            | "Subscribe" -> //let Guid = Guid.NewGuid()
                             let apiComm = {
                                // reqId = Guid.ToString()
                                userId = id |> string
                                content = ""
                                query = "Subscribe"
                                }
                             engineMessage apiComm
                            // Console.WriteLine(apiComm)

            | "Tweet" ->    let constructTweet = 
                                let mutable str = String.Empty
                                let tweetCount = Constants.Constants.tweets
                                let hashCount = Constants.Constants.hashtags
                                let rnd1 = random.Next(tweetCount.Length)
                                let rnd2 = random.Next(hashCount.Length)
                                str <- tweetCount.[rnd1]
                                str <- str + " " + hashCount.[rnd2]
                                str <- str + " @User_" + (random.Next(numNodes-1) |> string)
                                str
            
                            let liveTweet = constructTweet
                            Console.WriteLine("User " + id.ToString() + " tweeted " + liveTweet)
                            myTweets <- List.append myTweets [liveTweet]
                            myTweetsCount <- myTweetsCount + 1
                            //let Guid = Guid.NewGuid()
                            let apiComm = {
                                // reqId = Guid.ToString()
                                userId = id |> string
                                content = liveTweet
                                query = "Tweet"
                                }
                            engineMessage apiComm

            
            | "Retweet" ->  //let Guid = Guid.NewGuid()
                            let apiComm = {
                                // reqId = Guid.ToString()
                                userId = id |> string
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
            | Initiate(_) -> 
                nodesList <- [ for i in 1 .. currentNodes do yield (spawn system ("User_" + string (i))) User ]
                Console.WriteLine(nodesList.ToString())
                // let Guid = Guid.NewGuid()
                let payload = {
                    // reqId = Guid.ToString() 
                    userId = "" 
                    content = "" 
                    query = "Tweet"
                }

                nodesList |> List.iter (fun node -> node <! Json.serialize payload)
                // for j in nodesList do
                //     j <! (Json.serialize payload)
                // Console.WriteLine(payload)
                // Console.WriteLine("Hello")

            // | _ -> ignore ()

            return! loop ()
        }
    loop()

let supervisorRef = spawn system "supervisorRef" (Supervisor numNodes tweets)
supervisorRef <! Initiate("Initiate")
system.WhenTerminated.Wait()