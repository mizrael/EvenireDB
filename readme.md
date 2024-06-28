# EvenireDB

[![Tests](https://github.com/mizrael/EvenireDB/actions/workflows/dotnet.yml/badge.svg)](https://github.com/mizrael/EvenireDB/actions/workflows/dotnet.yml)
![coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/mizrael/ebd585c5ad0069d0e8486e43cade5793/raw/eveniredb-code-coverage.json)
[![Nuget](https://img.shields.io/nuget/v/Blazorex?style=plastic)](https://www.nuget.org/packages/EvenireDB.Client/)

> [Evenire](https://en.wiktionary.org/wiki/evenire), from Latin, present active infinitive of *ēveniō*, "to happen".

This project is a Proof-of-Concept of a small stream-based DB engine. 

One of the potential use cases is Event Sourcing. If you don't know what Event Sourcing is, I've been writing for a while about it on [my blog](https://www.davidguida.net). These articles can be a good starting point:
- [Event Sourcing in .NET Core – part 1: a gentle introduction](https://www.davidguida.net/event-sourcing-in-net-core-part-1-a-gentle-introduction/)
- [Event Sourcing: 5 things to consider when approaching it](https://www.davidguida.net/event-sourcing-things-to-consider)

I took a personal interest in this amazing pattern and after a while using it, I also wanted to write a database system specifically suited for it.
Honestly, I don't know how far this project will go, but I'm having a lot of fun so far and I am definitely learning a lot :)

## How does it work?

The basic idea behind Evenire is quite simple: events can be appended to streams and later on, retrieved by providing the stream ID.

Streams are identified by a tuple composed of a `Guid` (the stream key) and a string (the stream type). For the curious, the sources are [here](https://github.com/mizrael/EvenireDB/tree/main/src/EvenireDB.Common/StreamId.cs)).

Every stream is kept in memory using a local cache, for fast retrieval. A background process takes care of serializing events to a file, one per stream.

Reading can happen from the very beginning of a stream moving forward or from a specific point. This is the basic scenario, useful when you want to rehydrate the state of an [Aggregate](https://www.martinfowler.com/bliki/DDD_Aggregate.html).

Another option is to read the events from the _end_ of the stream instead, moving backward in time. This is interesting for example if you are recording data from sensors and you want to retrieve the latest state.

AuthN/Z was **left out intentionally** as it would go outside the scope of the project (for now).

## Setup

As of now, there are two possible options for spinning up an Evenire server:
- deploying the [Server project](https://github.com/mizrael/EvenireDB/tree/main/src/EvenireDB.Server) somewhere
- building the [docker image](https://github.com/mizrael/EvenireDB/blob/main/Dockerfile) and deploying it somewhere

These are both viable options, however, I would recommend opting for the Docker solution as it will package everything you need in the container. Building the image can be done using [this script](https://github.com/mizrael/EvenireDB/blob/main/scripts/dockerize.ps1).

Once you have the image ready, you can run it in a Container by running `docker compose up`.

## Client configuration

Once your server is up, you can start using it to store your events. If you are writing a .NET application, you can leverage [the Client library](https://github.com/mizrael/EvenireDB/tree/main/src/EvenireDB.Client) I provided.

Client configuration is pretty easy. The first step is to update your `appsettings.json` file and add a new section:
```json
{
  "Evenire": {
    "ServerUri": "[your server url here]",
    "HttpSettings": {
      "Port": 80 <---- make sure this is correct for you
    },
    "GrpcSettings": {
      "Port": 5243 <---- make sure this is correct for you
    }
  }
}
```

Once you have that, the last step is to register EvenireDB on your DI container. Something like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

var clientConfig = builder.Configuration.GetSection("Evenire").Get<EvenireClientConfig>();

builder.Services.AddEvenireDB(clientConfig);
```

### Writing events

Once you have added the Client to your DI Container, just inject `IEventsClient` into your classes and start making calls to it:

```csharp
var streamKey = /* this is a GUID */;
var streamId = new StreamId(streamKey, "MyStreamType");

await _eventsClient.AppendAsync(streamId, new[]
{
    Event.Create(new{ Foo = "bar" }, "Event type 1"),
    Event.Create(new{ Bar = "Baz" }, "Event type 1"),
});
```

### Reading events

Reading too can be done trough an `IEventsClient` instance:

```csharp
var streamKey = /* this is a GUID */;
var streamId = new StreamId(streamKey, "MyStreamType");

// write some events for streamId...

await foreach(var @event in client.ReadAsync(streamId, StreamPosition.Start, Direction.Forward).ConfigureAwait(false)){
  // do something with the event
}
```

`ReadAsync` can be configured to fetch the events from `StreamPosition.Start`, `StreamPosition.End` or a specific point in the stream. You can also specify the direction you want to move (forward or backward).

## Admin UI
Evenire also has a rudimentary administration UI, written with Blazor. It allows a few basic operations:
- see the list of all the available streams
- create a new stream
- append events to an existing stream
- delete a stream

![streams archive](https://raw.githubusercontent.com/mizrael/EvenireDB/main/docs/assets/streams_archive.jpg)

![stream details](https://raw.githubusercontent.com/mizrael/EvenireDB/main/docs/assets/stream_details.jpg)

## Samples
- [TemperatureSensors](https://github.com/mizrael/EvenireDB/tree/main/samples/EvenireDB.Samples.TemperatureSensors) shows how to use a Background worker to produce events and uses Minimal APIs to retrieve the latest events for a specific stream.

## TODO
- snapshots
- backup and replicas
- cluster management

