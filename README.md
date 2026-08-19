# Penny

Penny connects a Controller PC to an Agent PC. Once the agent is running on the second machine, the first machine connects and gets immediate control — no PIN or approval step.

## Quick start

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download).

### 1. Run the Agent on the second PC

```bash
dotnet run --project src/Penny.Agent
```

It listens on port **5000** by default and accepts controller connections immediately.

### 2. Connect from the first PC

```bash
dotnet run --project src/Penny.Controller -- <agent-ip> 5000
```

Or run without args for an interactive prompt:

```bash
dotnet run --project src/Penny.Controller
```

Replace `<agent-ip>` with the second PC's LAN address (e.g. `192.168.1.42`).

## Configuration

Copy the example settings for local overrides:

```bash
cp config/agent.settings.example.json config/agent.settings.json
```

## Build & test

```bash
dotnet build Penny.sln
dotnet test Penny.sln
```

## What is not included yet

Screen capture and input injection are not implemented. The current apps prove a fast direct connection over TLS with a live control channel.
