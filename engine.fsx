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

    let mutable reqList = List.Empty
    let mutable userList = List.Empty
    let userTweetTable = new Dictionary<string, list<String>>()
    let mutable allTweets = List.Empty
    let tweetTable = new Dictionary<string, list<String>>()
    let retweetTable = new Dictionary<string, list<String>>()
    let followers = new Dictionary<string, list<String>>()
    let following = new Dictionary<string, list<String>>()
    let hashTable = new Dictionary<string, list<String>>()
    let mentionTable = new Dictionary<string, list<String>>()
    let mutable dcList = List.Empty
    let mutable count = 0

    let isOnline userId = 
        if not(List.contains userId dcList) then
            true
        else
            false    

    let searchResults (query:string) = 
        let output = new Dictionary<string, list<String>>()
        let mutable tweetOutput = List.Empty
        let queryLower = query.ToLower.ToString()
        for tweet in allTweets do
            let words = tweet.ToString().Split(" ")
            let mutable tempBreak = false
            let mutable searchIndex = 0
            while not tempBreak && searchIndex < words.Length do
                let temp = words.[searchIndex].ToLower.ToString()
                if temp.Contains queryLower then
                    tweetOutput <- List.append tweetOutput [tweet]
                    tempBreak <- true
                else
                    searchIndex <- searchIndex + 1
        let mutable hashOutput = List.Empty
        for key in hashTable.Keys do
            let temp = key.ToLower.ToString()
            if temp.Contains queryLower then
                hashOutput <- List.append hashOutput [key]

        let mutable userOutput = List.Empty
        for user in userList do
            let temp = user.ToString().ToLower.ToString()
            if temp.Contains queryLower then
                userOutput <- List.append userOutput [user]

        if userOutput = List.Empty && hashOutput = List.Empty && tweetOutput = List.Empty then
            output.Add("Output",["No results found for" + query])
        else
            output.Add("tweets",tweetOutput)
            output.Add("hashTags",hashOutput)
            output.Add("users",userOutput)
        output

    let rec loop () =

        actor {

            let! json = mailbox.Receive()

            let message = Json.deserialize<apiComm> json

            let requestId = message.reqId

            if not (List.contains requestId reqList) then

                List.append reqList [requestId] |> ignore

                let userId = message.userId

                match message.query with

                | "SignUp" ->
                    userList <- List.append userList [userId]

                | "Tweet" -> 

                    let tweet = message.content

                    count <- count + 1
                    
                    if tweetTable.ContainsKey userId then
                        tweetTable.[userId] <- List.append tweetTable.[userId] [tweet]
                    else
                        tweetTable.Add(userId,[tweet])

                    allTweets <-List.append allTweets [tweet]    

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

                | "Retweet" ->

                    let tweet = message.content
                    let mutable tempBreak = false
                    let mutable index = 0
                    let keys = List(tweetTable.Keys)
                    while not tempBreak && index < keys.Capacity do
                        if List.contains tweet tweetTable.[keys.[index]] then
                            let res = tweetTable.[keys.[index]] |> List.find(fun(x) -> x = tweet)  
                            tempBreak <- true
                            let destUser = keys.[index]
                            let payload = {
                                reqId = "1234"
                                userId = "Server"
                                content = userId + " Re - tweeted your tweet " + res 
                                query  = "ReTweetNotif"
                            }

                            userMessage payload destUser


                | "Follow" -> 

                    let followed = message.content

                    if followers.ContainsKey followed then
                        followers.[followed] <- List.append followers.[followed] [userId]
                    else 
                        followers.Add(followed,[userId])

                    if following.ContainsKey userId then
                        following.[userId] <- List.append following.[userId] [followed]

                | "Search" ->
                    let query = message.content

                    let searchOutput = Json.serialize (searchResults query)

                    let payload = {
                        reqId = "1234"
                        userId = "Server"
                        content = searchOutput
                        query = "SearchResults"
                    }

                    userMessage payload userId

                | "Login" ->

                    dcList <- dcList |> List.filter(fun(x) -> x <> userId)
                    
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