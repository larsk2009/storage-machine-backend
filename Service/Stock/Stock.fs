/// This module exposes use-cases of the Stock component as an HTTP Web service using Giraffe.
module StorageMachine.Stock.Stock

open FSharp.Control.Tasks
open Giraffe
open Microsoft.AspNetCore.Http
open Thoth.Json.Giraffe
open Thoth.Json.Net
open Stock

/// An overview of all bins currently stored in the Storage Machine.
let binOverview (next: HttpFunc) (ctx: HttpContext) =
    task {
        let dataAccess = ctx.GetService<IStockDataAccess> ()
        let bins = Stock.binOverview dataAccess
        return! ThothSerializer.RespondJsonSeq bins Serialization.encoderBin next ctx 
    }

/// An overview of actual stock currently stored in the Storage Machine. Actual stock is defined as all non-empty bins.
let stockOverview (next: HttpFunc) (ctx: HttpContext) =
    task {
        let dataAccess = ctx.GetService<IStockDataAccess> ()
        let bins = Stock.stockOverview dataAccess
        return! ThothSerializer.RespondJsonSeq bins Serialization.encoderBin next ctx 
    }

let storeBin (next: HttpFunc) (ctx: HttpContext) =
    task {
        let dataAccess = ctx.GetService<IStockDataAccess> ()
        
        let! inputBin = ThothSerializer.ReadBody ctx Serialization.decoderBin

        match inputBin with
        | Error _ -> return! RequestErrors.badRequest (text $"Can't parse POST body as bin") earlyReturn ctx
        | Ok bin ->
            match dataAccess.StoreBin bin with
            | Error error -> 
                match error with
                | BinAlreadyStored ->  return! RequestErrors.badRequest (text "Bin already exists") earlyReturn ctx
            | Ok _ -> return! Successful.ok (text "") next ctx
    }

/// Defines URLs for functionality of the Stock component and dispatches HTTP requests to those URLs.
let handlers : HttpHandler =
    choose [
        GET >=> route "/bins" >=> binOverview
        POST >=> route "/bins" >=> storeBin
        GET >=> route "/stock" >=> stockOverview
    ]