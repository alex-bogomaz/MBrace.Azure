﻿namespace Nessos.MBrace.Azure.Runtime.Resources

open System
open System.Collections.Concurrent
open System.Runtime.Serialization
open Microsoft.WindowsAzure.Storage
open Nessos.MBrace.Azure.Runtime
open Nessos.MBrace.Azure.Runtime.Common
open Nessos.MBrace.Runtime
open Microsoft.WindowsAzure.Storage.Table

[<Sealed;AbstractClass>]
type Registry private () =
    static let registry = ConcurrentDictionary<ConfigurationId * Type, obj>()

    static member Register<'T>(config : ConfigurationId, item : 'T) =
        registry.TryAdd((config, typeof<'T>), item :> obj)
        |> ignore

    static member Resolve<'T>(config) =
        registry.[config, typeof<'T>] :?> 'T

type ResourceFactory private (config : Configuration) =
  
    do Registry.Register<_>(config.ConfigurationId, new WorkerMonitor(config.DefaultTableOrContainer))
    do Registry.Register<_>(config.ConfigurationId, new ProcessMonitor(config.DefaultTableOrContainer))
    do Registry.Register<_>(config.ConfigurationId, new NullLogger() :> ILogger)

    member __.RequestCounter(container, count) = Counter.Init(container, count)
    member __.RequestResultAggregator<'T>(container, count : int) = ResultAggregator<'T>.Init(container, count)
    member __.RequestCancellationTokenSource(container, ?parent) = DistributedCancellationTokenSource.Init(container, ?parent = parent)
    member __.RequestResultCell<'T>(container) = ResultCell<Result<'T>>.Init(container)
    member __.ProcessMonitor = Registry.Resolve<ProcessMonitor>(config.ConfigurationId)
    member __.WorkerMonitor = Registry.Resolve<WorkerMonitor>(config.ConfigurationId)
    member __.Logger = Registry.Resolve<ILogger>(config.ConfigurationId)

    static member Init (config : Configuration) =  new ResourceFactory(config)