# EvenireDB

[![Tests](https://github.com/mizrael/EvenireDB/actions/workflows/tests.yml/badge.svg)](https://github.com/mizrael/EvenireDB/actions/workflows/tests.yml)

> [Evenire](https://en.wiktionary.org/wiki/evenire), from Latin, present active infinitive of *ēveniō*, "to happen".

This project is a Proof-of-Concept of a small DB engine specifically for Event Sourcing. 
If you don't know what Event Sourcing is, I've been writing for a while about it on [my blog](https://www.davidguida.net). These articles can be a good starting point:
- [Event Sourcing in .NET Core – part 1: a gentle introduction](https://www.davidguida.net/event-sourcing-in-net-core-part-1-a-gentle-introduction/)
- [Event Sourcing: 5 things to consider when approaching it](https://www.davidguida.net/event-sourcing-things-to-consider)

I took a personal interest in this amazing pattern and after a while using it, I also wanted to write a database system specifically suited for it.
Honestly, I don't know how far this project will go, but I'm having a lot of fun so far and I am definitely learning a lot :)

The basic idea behind Evenire is quite simple: events can be appended to stream and later on, retrieved by providing the stream ID.

Reading can happen from the very beginning of a stream moving forward or from a specific point. This is the basic scenario, useful when you want to rehydrate the state of an [Aggregate](https://www.martinfowler.com/bliki/DDD_Aggregate.html).

Another option is to read the events from the _end_ of the stream instead, moving backwards in time. This is interesting for example if you are recording data from sensors and you want to retrieve the latest state.

# Setup

As of now, there are two possible options for spinning up an Evenire server:
- deploying the [Server project](https://github.com/mizrael/EvenireDB/tree/main/src/EvenireDB.Server) somewhere
- building the [docker image](https://github.com/mizrael/EvenireDB/blob/main/Dockerfile) and deploying it somewhere

These are both viable options, however, I would recommend opting for the Docker solution as it will package everything you need in the container. Building the image can be done using [this script](https://github.com/mizrael/EvenireDB/blob/main/scripts/dockerize.ps1).
