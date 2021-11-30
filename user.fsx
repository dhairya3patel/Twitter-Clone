#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: FSharp.Json"

open System
open Akka.Actor
open Akka.FSharp
open Akka.Configuration
open System.Text
open FSharp.Json

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

let system = System.create "twitter_sim" configuration

let mutable numNodes = fsi.CommandLineArgs.[1] |> int
let tweets = fsi.CommandLineArgs.[2] |> int

let User (userid: int) (mailbox: Actor<_>) = 
    let rec loop () =
        actor {
            let! message = mailbox.Receive()

            match message with
            

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
