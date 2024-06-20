# EvenireDB

[![Tests](https://github.com/mizrael/EvenireDB/actions/workflows/dotnet.yml/badge.svg)](https://github.com/mizrael/EvenireDB/actions/workflows/dotnet.yml)

> [Evenire](https://en.wiktionary.org/wiki/evenire), from Latin, present active infinitive of *ēveniō*, "to happen".

This project is a Proof-of-Concept of a small DB engine specifically for Event Sourcing. 
If you don't know what Event Sourcing is, I've been writing for a while about it on [my blog](https://www.davidguida.net). These articles can be a good starting point:
- [Event Sourcing in .NET Core – part 1: a gentle introduction](https://www.davidguida.net/event-sourcing-in-net-core-part-1-a-gentle-introduction/)
- [Event Sourcing: 5 things to consider when approaching it](https://www.davidguida.net/event-sourcing-things-to-consider)

I took a personal interest in this amazing pattern and after a while using it, I also wanted to write a database system specifically suited for it.
Honestly, I don't know how far this project will go, but I'm having a lot of fun so far and I am definitely learning a lot :)

## How does it work?

The basic idea behind Evenire is quite simple: events can be appended to stream and later on, retrieved by providing the stream ID.

Every stream is kept in memory using a local cache, for fast retrieval. A background process takes care of serializing events to a file, one per stream.

Reading can happen from the very beginning of a stream moving forward or from a specific point. This is the basic scenario, useful when you want to rehydrate the state of an [Aggregate](https://www.martinfowler.com/bliki/DDD_Aggregate.html).

Another option is to read the events from the _end_ of the stream instead, moving backwards in time. This is interesting for example if you are recording data from sensors and you want to retrieve the latest state.

# Setup

As of now, there are two possible options for spinning up an Evenire server:
- deploying the [Server project](https://github.com/mizrael/EvenireDB/tree/main/src/EvenireDB.Server) somewhere
- building the [docker image](https://github.com/mizrael/EvenireDB/blob/main/Dockerfile) and deploying it somewhere

These are both viable options, however, I would recommend opting for the Docker solution as it will package everything you need in the container. Building the image can be done using [this script](https://github.com/mizrael/EvenireDB/blob/main/scripts/dockerize.ps1).

Once you have the image ready, you can run it in a Container by running `docker compose up`.

# Client configuration

Once your server is up, you can start using it to store your events. If you are writing a .NET application, you can leverage [the Client library](https://github.com/mizrael/EvenireDB/tree/main/src/EvenireDB.Client) I provided.

Configuration is pretty easy, just add this to your `Program.cs` or `Startup.cs` file:

```csharp
var builder = WebApplication.CreateBuilder(args);

var connectionString = new Uri(builder.Configuration.GetConnectionString("evenire"));

builder.Services.AddEvenireDB(new EvenireConfig(connectionString, useGrpc: true));
```

## Writing events

Once you have added the Client to your IoC Container, just inject `IEventsClient` into your classes and start making calls to it:

```csharp
var streamId = Guid.NewGuid();

await _eventsClient.AppendAsync(streamId, new[]
{
    Event.Create(new{ Foo = "bar" }, "Event type 1"),
    Event.Create(new{ Bar = "Baz" }, "Event type 1"),
});
```

## Reading events

Reading too can be done trough an `IEventsClient` instance:

```csharp
var streamId = Guid.NewGuid();

// write some events for streamId...

await foreach(var @event in client.ReadAsync(streamId, StreamPosition.Start, Direction.Forward).ConfigureAwait(false)){
  // do something with the event
}
```

`ReadAsync` can be configured to fetch the events from `StreamPosition.Start`, `StreamPosition.End` or a specific point in the stream. You can also specify the direction you want to move (forward or backward).

# Samples
- [TemperatureSensors](https://github.com/mizrael/EvenireDB/tree/main/samples/EvenireDB.Samples.TemperatureSensors) shows how to use a Background worker to produce events and uses Minimal APIs to retrieve the latest events for a specific stream.

# TODO
- snapshots
- backup and replicas
- cluster management

