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
    reqId: String
    userId: String
    content: String
    query: String
}

type Communication = 
    | Initiate of string
    | Done of String



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
    let userId = mailbox.Self.Path.Name
    let mutable status = "online"

    let rec loop () =
        actor {
            let! jsonMessage = mailbox.Receive()
            // Console.WriteLine("Before") //+ jsonMessage.ToString())
            let message = Json.deserialize<apiComm> jsonMessage
            // Console.WriteLine("Hello") //+ message.ToString())
            let action = message.query

            if timer.Elapsed.TotalSeconds - timerState > 1.0 && status = "offline" then

                let guid = Guid.NewGuid()
                let payload = {

                    reqId = guid.ToString()
                    userId = userId
                    content = ""
                    query = "Login"

                }

                engineMessage payload
                status <- "online"
                timerState <- timer.Elapsed.TotalSeconds
            
            match action with
            | "Login" ->    let guid = Guid.NewGuid()
                            let apiComm = {
                                reqId = guid.ToString()
                                userId = userId
                                content = ""
                                query = "Login"
                            }
                            engineMessage apiComm
                            // Console.WriteLine(apiComm)

            | "Register" -> Console.WriteLine(id.ToString() + " has been registered")
                            let guid = Guid.NewGuid()
                            let apiComm = {
                                reqId = guid.ToString()
                                userId = userId
                                content = ""
                                query = "SignUp"
                            }
                            engineMessage apiComm
                            // Console.WriteLine(apiComm)

            | "Subscribe" -> let guid = Guid.NewGuid()
                             let apiComm = {
                                reqId = guid.ToString()
                                userId = userId
                                content = ""
                                query = "Subscribe"
                                }
                             engineMessage apiComm
                            // Console.WriteLine(apiComm)

            | "Tweet" ->    
                let constructTweet = 
                    let mutable str = String.Empty
                    let tweetCount = Constants.Constants.tweets
                    let hashCount = Constants.Constants.hashtags
                    let rnd1 = random.Next(tweetCount.Length)
                    let rnd2 = random.Next(hashCount.Length)
                    str <- tweetCount.[rnd1]
                    str <- str + " " + hashCount.[rnd2]
                    str <- str + " @User_" + (random.Next(1,numNodes) |> string)
                    str
                if myTweetsCount < tweets then
                    let liveTweet = constructTweet
                    Console.WriteLine("User " + id.ToString() + " tweeted " + liveTweet)
                    myTweets <- List.append myTweets [liveTweet]
                    myTweetsCount <- myTweetsCount + 1
                    let guid = Guid.NewGuid()
                    let apiComm = {
                        reqId = guid.ToString()
                        userId = userId
                        content = liveTweet
                        query = "Tweet"
                        }
                    engineMessage apiComm
                else
                    supervisorRef <! Done("Done")

            
            | "Retweet" ->  let guid = Guid.NewGuid()
                            let apiComm = {
                                reqId = guid.ToString()
                                userId = userId
                                content = ""
                                query = "Retweet"
                            }
                            engineMessage apiComm

            | "Search" ->   let guid = Guid.NewGuid()
                            let apiComm = {
                                reqId = guid.ToString()
                                userId = userId
                                content = Constants.Constants.hashtags.[random.Next(Constants.Constants.hashtags.Length)].[1..]
                                query = "Search"
                            }
                            engineMessage apiComm
                            // Console.WriteLine(apiComm)

            | "SearchResults" -> Console.WriteLine("User " + id.ToString() + " SearchResults: " + message.content.ToString())

            | "LiveFeed" -> Console.WriteLine ("User " + id.ToString() + ": " + message.content.ToString())

            | "UpdateFeed" -> Console.WriteLine ("User " + id.ToString() + ": " + message.content.ToString())

            | "Run" -> if status = "online" then
                            let rnd = Constants.Constants.actions.[random.Next(Constants.Constants.actions.Length)]
                            Console.WriteLine("Action selected: " + rnd)
                            match rnd with 
                            | "tweetAction" ->  let guid = Guid.NewGuid()
                                                let apiComm = {
                                                    reqId = guid.ToString()
                                                    userId = userId
                                                    content = ""
                                                    query = "Tweet"
                                                }
                                                userMessage apiComm id
                                        
                            | "searchAction" ->     let guid = Guid.NewGuid()
                                                    let apiComm = {
                                                        reqId = guid.ToString()
                                                        userId = userId
                                                        content = ""
                                                        query = "Search"
                                                    }
                                                    userMessage apiComm id

                            | "retweetAction" ->    let guid = Guid.NewGuid()
                                                    let apiComm = {
                                                        reqId = guid.ToString()
                                                        userId = userId
                                                        content = ""
                                                        query = "Retweet"
                                                    }
                                                    userMessage apiComm id

                            | "subscribeAction" ->  let guid = Guid.NewGuid()
                                                    let apiComm = {
                                                        reqId = guid.ToString()
                                                        userId = userId
                                                        content = ""
                                                        query = "Subscribe"
                                                    }
                                                    userMessage apiComm id 

                            | "disconnectAction" ->     let guid = Guid.NewGuid()
                                                        let apiComm = {
                                                            reqId = guid.ToString()
                                                            userId = userId
                                                            content = ""
                                                            query = "Logout"
                                                        }
                                                        userMessage apiComm id 

                            | "connectAction" ->    let guid = Guid.NewGuid()
                                                    let apiComm = {
                                                        reqId = guid.ToString()
                                                        userId = userId
                                                        content = ""
                                                        query = "Connect"
                                                    }
                                                    userMessage apiComm id 

                            | _ -> ignore()

                            // let guid = Guid.NewGuid()
                            // let apiComm = {
                            //                 reqId = guid.ToString()
                            //                 userId = userId
                            //                 content = ""
                            //                 query = "Run"
                            //                 }
                            // userMessage apiComm id

            | "Logout" -> 
                status <- "offline"
                let guid = Guid.NewGuid()
                let apiComm = {
                    reqId = guid.ToString()
                    userId = userId
                    content = ""
                    query = "Logout"
                }
                engineMessage apiComm            

            | "Connect" -> 
                status <- "online"
                let guid = Guid.NewGuid()
                let apiComm = {
                    reqId = guid.ToString()
                    userId = userId
                    content = ""
                    query = "Login"
                }
                engineMessage apiComm

            | _ -> ignore()

            // if myTweetsCount < tweets then
            //     let guid = Guid.NewGuid()
            //     let apiComm = {
            //                     reqId = guid.ToString()
            //                     userId = userId
            //                     content = ""
            //                     query = "Run"
            //                     }
            //     let mutable counter = 0
            //     while counter < 10 do
            //         counter <- counter + 1
            //     userMessage apiComm id
            // else
            //     Console.WriteLine ("User " + id.ToString() + " Tweet count " + myTweetsCount.ToString())

            return! loop ()
        }
    loop()
let mutable time = 0
let Supervisor (numNodes: int) (tweets: int) (mailbox: Actor<_>) = 
    let currentNodes = numNodes
    let mutable nodesList = []
    let numTweets = tweets
    let mutable doneList = Set.empty
    let timer = Diagnostics.Stopwatch()
    let rec loop () =
        actor {
            let! message = mailbox.Receive()

            match message with
            | Initiate(_) -> 
                nodesList <- [ for i in 1 .. currentNodes do yield (spawn system ("User_" + string (i))) User ]
                //Console.WriteLine(nodesList.ToString())
                let guid = Guid.NewGuid()
                let payload = {
                    reqId = guid.ToString() 
                    userId = "Supervisor" 
                    content = "" 
                    query = "Register"
                }

                nodesList |> List.iter (fun node -> node <! Json.serialize payload)
                timer.Start()
                let payload2 = {
                    reqId = guid.ToString() 
                    userId = "Supervisor" 
                    content = "" 
                    query = "Run"
                }
                
                nodesList |> List.iter (fun node -> system.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(2.0),TimeSpan.FromSeconds(1.0),node , Json.serialize(payload2)))
                // nodesList |> List.iter 

            | Done (_) ->
                doneList <- doneList.Add(mailbox.Sender())
                if doneList.Count = numNodes then
                    time <- timer.ElapsedMilliseconds |> int
                    system.Terminate() |> ignore

                    

            return! loop ()
        }
    loop()

let supervisorRef = spawn system "supervisorRef" (Supervisor numNodes tweets)
supervisorRef <! Initiate("Initiate")

system.WhenTerminated.Wait()
Console.WriteLine("Time " + time.ToString())
