# Penny

Penny is a modular .NET solution for agent connection identity, transport security,
protocol framing, and session authentication.

## Projects

- `src/Penny.Core`: shared models and core abstractions.
- `src/Penny.Security`: security primitives for IDs, PINs, tokens, certificates.
- `src/Penny.Protocol`: protocol envelope, serialization, and frame codec.
- `src/Penny.Network`: transport and listener/session authentication surfaces.

## Tests

- `tests/Penny.Core.Tests`
- `tests/Penny.Protocol.Tests`

## Build

```bash
dotnet restore Penny.sln
dotnet build Penny.sln
dotnet test Penny.sln
```
