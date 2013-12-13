// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open System
open System.IO
open System.Text
open System.ServiceModel
open System.Collections.Generic
open System.Security.Cryptography
open System.Web
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Data.TypeProviders

open FSharp.Data
open ImpromptuInterface.FSharp
open Nancy
open Nancy.Hosting.Self
open Nancy.ModelBinding
open Newtonsoft.Json

open AmazonLibrary

type PresentList = { Name:string; List:string list }

type Message =
    | GetList of Name:string * AsyncReplyChannel<string list option>
    | CreateList of Name:string * List:string list

type Mailbox()=
    let mailbox = MailboxProcessor.Start(fun inbox ->
        let rec loop lists =
            async {
                let! message = inbox.Receive()
                match message with
                | GetList(name, reply) -> 
                     let result =
                        lists
                        |> Seq.where(fun i -> i.Name = name)
                        |> Seq.map(fun i -> i.List)
                     match result |> Seq.length with
                     | 0 -> reply.Reply None
                     | 1 -> reply.Reply <| Some (result |> Seq.exactlyOne)
                     | _ -> ()
                     return! loop lists
                | CreateList(name, newList) ->
                    return! loop <| { Name=name; List=newList } :: (lists |> Seq.where(fun i -> i.Name <> name) |> Seq.toList)
            }
        loop [{Name="Simon";List=["droidcopter"]}])
    member m.GetList name =
        mailbox.PostAndReply(fun i -> GetList(name, i))

    member m.AddList name list =
        CreateList(name, list) |> mailbox.Post

type Result<'a> = { Message:string; Result:'a }

let mailbox = Mailbox()

type ListRequest() = 
    member val list:string array = [||] with get, set
    
type amazonAccountParser = JsonProvider<"rootkey.json">
let amazonConfig = amazonAccountParser.Load("rootkey.json")

type ItemResponse = XmlProvider<"AmazonItemExample.xml">

let amazonSearch keywords =
    let helper = SignedRequestHelper(amazonConfig.AwsAccessKeyId, amazonConfig.AwsSecretKey, amazonConfig.Destination)
    let dict = Dictionary<string, string>()
    dict.["Service"] <- "AWSECommerceService"
    dict.["AssociateTag"] <- "simonhdickson-20"
    dict.["Version"] <- "2011-08-01"
    dict.["Operation"] <- "ItemSearch"
    dict.["ResponseGroup"] <- HttpUtility.UrlPathEncode("Images,ItemAttributes,Offers")
    dict.["Keywords"] <- HttpUtility.UrlPathEncode(keywords)
    dict.["SearchIndex"] <- "All"
    dict.["Condition"] <- "All"
    dict.["Timestamp"] <- HttpUtility.UrlPathEncode(DateTime.Now.ToString("u"))
    helper.Sign(dict)

module Seq =
    let headOrOption (list:seq<_>) =
        match list |> Seq.toList with
        | head :: _ -> Some head
        | [] -> None

type SantaApi() as self =
    inherit NancyModule()
    do
        self.Get.["/{name}"] <- 
            fun parameters ->
                match mailbox.GetList parameters?name?Value with
                | Some x -> { Message="Hohoho, here you go"; Result=x } :> obj
                | None -> { Message="Hohoho, the elves must have lost this list"; Result=null } :> obj
        self.Post.["/{name}"] <- 
            fun parameters ->
                let serializer = new JsonSerializer()
                let body = serializer.Deserialize<ListRequest>(new JsonTextReader(new StreamReader(self.Request.Body)))
                mailbox.AddList parameters?name?Value (body.list |> List.ofArray)
                { Message="Hohoho, the elves are looking in into this"; Result=null } :> obj
        self.Get.["/{name}/amazon/"] <-
            fun parameters ->
                match mailbox.GetList parameters?name?Value with
                | Some list ->
                    list
                    |> Seq.map amazonSearch
                    |> Seq.map ItemResponse.Load
                    |> Seq.map (fun response -> response.Items.GetItems() |> Seq.headOrOption)
                    |> Seq.choose (fun i -> i)
                    |> Seq.map (fun i -> i.DetailPageUrl)
                    |> Seq.toArray
                    |> fun i -> { Message="Hohoho, here is what they want"; Result=i } :> obj
                | None -> { Message="Hohoho, the elves must have lost this list"; Result=null } :> obj
        self.Get.["/{name}/naughtyornice"] <-
            fun parameters ->
                let name:string = parameters?name?Value
                use sha1 = new SHA1Managed()
                let result = sha1.ComputeHash(Encoding.UTF8.GetBytes(name.ToLowerInvariant())) |> Seq.head
                match result with
                | x when x < byte 128 -> "Nice" :> obj
                | _ -> "Naughty" :> obj

[<EntryPoint>]
let main argv =
    let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/")
    nancyHost.Start()
    Console.ReadKey() |> ignore
    0
