#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: FSharp.Json"
#load "./constants.fsx"

open System
open System.Text
open Akka.Actor
open Akka.FSharp
open Akka.Configuration
open System.Text
open FSharp.Json
open Constants.Constants

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

let system = ActorSystem.Create( "twitter_sim",configuration)

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
    str <- tweetCount[rnd1]
    str <- str + " " + hashCount[rnd2]
    str <- str + " @User_" + (random.Next(numNodes-1) |> string)
    str

type apiComm = {
    reqId: String
    userId: String
    content: String
    query: String
}


let User (userid: int) (mailbox: Actor<_>) = 
    let id = userid
    let rec loop () =
        actor {
            let! jsonMessage = mailbox.Receive()
            let message = Json.deserialize jsonMessage
            let action = message.query
            
            match action with
            | "Register" -> Console.WriteLine("%s has been registered", userid)
                            let Guid = Guid.NewGuid()
                            let apiComm = {
                                reqId = Guid.ToString()
                                userId = userid |> string
                                content = ""
                                query = "SignUp"
                            }
                            engineMessage apiComm
                            Console.WriteLine(apiComm)

            | "Subscribe" -> let Guid = Guid.NewGuid()
                             let apiComm = {
                                reqId = Guid.ToString()
                                userId = userid |> string
                                content = ""
                                query = "Subscribe"
                                }
                             engineMessage apiComm
                            // Console.WriteLine(apiComm)
            
            | "Retweet" ->  let Guid = Guid.NewGuid()
                            let apiComm = {
                                reqId = Guid.ToString()
                                userId = userid |> string
                                content = ""
                                query = "Retweet"
                            }
                            engineMessage apiComm
                            // Console.WriteLine(apiComm)

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
            | "Initiate" -> nodesList <- [ for i in 1 .. currentNodes do yield (spawn system ("Node_" + string (i)))  ]
                            Console.WriteLine(nodesList)

                            let apiComm = {
                                reqId = ""
                                userId = ""
                                content = ""
                                query = "register"
                            }

                            for j in 0..nodesList-1 do
                                nodesList.[j] <! 



            return! loop ()
        }
    loop()
