# Architecture

Penny is split into focused libraries:

- Core models (`Penny.Core`)
- Security primitives (`Penny.Security`)
- Wire protocol and framing (`Penny.Protocol`)
- Network transport/authentication (`Penny.Network`)

Primary flow:

1. Agent connection arrives on the network transport.
2. Session is authenticated with security primitives.
3. Messages are encoded/decoded through the protocol layer.
4. Session state is updated in core models.
