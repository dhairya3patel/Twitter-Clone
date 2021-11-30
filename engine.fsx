#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: FSharp.Json"


open System
open Akka.Actor
open Akka.FSharp
open Akka.Configuration
open System.Security.Cryptography
open System.Text
open FSharp.Json
open System.Collections.Generic


let numNodes = fsi.CommandLineArgs.[1] |> int
let numTweets = fsi.CommandLineArgs.[2] |> int
let mutable terminate = false

let HASHTAG = '#'
let MENTION = '@'


let config = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                
            }
            remote {
                helios.tcp {
                    port = 0
                    hostname = localhost
                }
            }
        }")

type apiComm = {
    reqId: String
    userId: String
    content: String
    query : String
}        

let system = ActorSystem.Create("TwitterClone", config)

let serverMessage payload = 
    let server = select ("akka.tcp://TwitterClone@127.0.0.1:9002/user/Server") system

    let request = Json.serialize payload

    server <! request


let userMessage payload id = 
    let user = select ("akka.tcp://TwitterClone@127.0.0.1:9001/user/User_" + id.ToString()) system

    let request = Json.serialize payload

    user <! request

    

let filterSpecial (item: char) (tweet: string) =
    let mutable list = []
    for temp in tweet.Split(' ') do
        if temp.[0] = item then
            list <- List.append list [temp.[1..]]
        else if Seq.contains item temp then 
            for i in 1..temp.Length - 1 do 
                if temp.[i] = item then
                    list <- List.append list [temp.[i + 1..]]
    list


let serverActor (mailbox: Actor<_>) =

    let mutable reqList = []
    let userTweetTable = new Dictionary<string, list<String>>()
    let tweetTable = new Dictionary<string, list<String>>()
    let retweetTable = new Dictionary<string, list<String>>()
    let followers = new Dictionary<string, list<String>>()
    let following = new Dictionary<string, list<String>>()
    let hashTable = new Dictionary<string, list<String>>()
    let mentionTable = new Dictionary<string, list<String>>()
    let mutable dcList = []
    let mutable count = 0

    let isOnline userId = 
        if List.contains userId dcList then
            true
        else
            false    

    let rec loop () =

        actor {

            let! json = mailbox.Receive()

            let message = Json.deserialize<apiComm> json

            let requestId = message.reqId

            if not (List.contains requestId reqList) then

                List.append reqList [requestId] |> ignore

                let userId = message.userId

                match message.query with

                | "Tweet" -> 

                    let tweet = message.content

                    count <- count + 1
                    
                    if tweetTable.ContainsKey userId then
                        tweetTable.[userId] <- List.append tweetTable.[userId] [tweet]
                    else
                        tweetTable.Add(userId,[tweet])

                    if count >= numTweets then
                        terminate <- true

                    let hashtags = filterSpecial HASHTAG tweet

                    for hashtag in hashtags do
                        if hashTable.ContainsKey hashtag then
                            hashTable.[hashtag] <- List.append hashTable.[hashtag] [tweet]
                        else
                            hashTable.Add(hashtag,[tweet])

                    let mentions = filterSpecial MENTION tweet

                    for mention in mentions do
                        if mentionTable.ContainsKey mention then
                            mentionTable.[mention] <- List.append hashTable.[mention] [tweet]
                        else
                            mentionTable.Add(mention,[tweet])                    

                    for follower in followers.[userId] do
                        if isOnline follower then
                            let payload = {
                                reqId = "1234"
                                userId = "Server"
                                content = userId + " Tweeted " + tweet
                                query  = "LiveFeed"
                            }

                            let subId = follower.ToString().Split("_").[1]
                            userMessage payload subId
                      //  else


                | "Follow" -> 

                    let followed = message.content

                    if followers.ContainsKey followed then
                        followers.[followed] <- List.append followers.[followed] [userId]
                    else 
                        followers.Add(followed,[userId])

                    if following.ContainsKey userId then
                        following.[userId] <- List.append following.[userId] [followed]

                // | "Search" ->


                | "Logout" ->

                    dcList <- List.append dcList [userId]

                | _ -> ignore()

            else
                mailbox.Sender() <! Error("Invalid Request")    

            return! loop ()
        }

    loop ()

let server = spawn system "server" serverActor

while terminate = false do 
   1 |> ignore

system.Terminate()
