#r "nuget: Akka.FSharp"

open Akka.FSharp

module Constants =
    let hashtags = [
        "#holidays";
        "#christmas";
        "#newyork";
        "#celebrations";
        "#instagram";
        "#meta";
        "#tesla";
        "#newyearseve";
        "#celebrations";
        "#rockafeller";
        "#empirestate";
        "#geazy";
        "#weekend";
        "#astroworld";
        "#virgilabloh";
        "#omicron"
    ]

    let tweets = [
        "teamwork makes the dream work";
        "Ohmmmmmmy Goddd its the Grammys";
        "Hello! I'm a Tweet.";
        "We're all here together!";
        "Isn't Twitter so much fun";
        "Covid is back"
    ]

    let search = [
        "omicron";
        "geazy";
        "teamwork";
        "Covid"
    ]

    let actions = [
        "tweetAction";
        "searchAction";
        "retweetAction";
        "subscribeAction";
        "disconnectAction";
        "connectAction"
    ]