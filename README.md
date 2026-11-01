# Go Heroes Unity

The Unity client for **Go Heroes**, a tiny two-player turn-based multiplayer game backed by an authoritative Go server.

This repository intentionally keeps the game presentation small so the project can focus on client/server integration rather than content production.

## Player experience

The MVP has a compact flow:

```text
┌─────────────┐
│    PLAY     │
└──────┬──────┘
       ▼
Waiting for opponent...
       │
       ▼
┌──────────────────────────┐
│        BATTLE            │
│                          │
│ Enemy HP      My HP      │
│                          │
│  Enemy       My Hero     │
│                          │
│ Attack  Defend  Heal     │
└─────────────┬────────────┘
              ▼
        ┌───────────┐
        │ Win/Lose  │
        │ Play Again│
        └─────┬─────┘
              │
              └──► Waiting for opponent...
```

The current UI concept uses a single compact game screen with different visual states rather than a large game world.

## MVP states

### Ready

The player sees **Play**.

Pressing Play asks the networking layer to connect to the Go server and enter matchmaking.

### Waiting

The client displays **Waiting for opponent...** and disables Attack, Defend, and Heal.

### Playing

The client displays:

- Go Heroes title.
- Enemy HP bar.
- Player HP bar.
- Enemy character.
- Player character.
- Attack.
- Defend.
- Heal.

Actions are enabled only when permitted by the server state.

### Game over

The client displays:

- **You win!** or **You lose!**
- **Play Again**

Play Again starts a new matchmaking attempt. It does not imply recovery of a dropped in-progress connection.

## Client responsibility

Unity is responsible for:

- UI.
- Player input.
- Visual state.
- Small animations/effects if added.
- TCP connection management.
- Encoding client intentions.
- Receiving server events.
- Updating Unity objects on the main thread.

Unity is **not** responsible for:

- Calculating damage.
- Applying authoritative HP changes.
- Deciding turns.
- Validating actions.
- Determining victory/defeat.

The Go server owns those rules.

## Networking

The MVP connects directly to the Go Heroes Server using **TCP** and exchanges newline-delimited JSON messages.

Conceptually:

```text
Button click
    │
    ▼
Client sends intention
    │
    ▼
Go server validates/calculates
    │
    ▼
Server sends new state
    │
    ▼
Unity renders it
```

Incoming TCP work must not block Unity's main thread. UI/GameObject changes must be marshalled back to the Unity main thread.

## Visual scope

Keep the MVP deliberately small:

- One primary game screen.
- Two small character sprites.
- Two HP bars.
- Three action buttons.
- Waiting text.
- Win/loss message.
- Play / Play Again.

Optional polish can include tiny attack, defend, and heal feedback, but complex animation systems are not required.

## Planned repository structure

The exact Unity folders can evolve, but networking, UI, and game presentation should remain clearly separated.

```text
go-heroes-unity/
├── docs
│   └── DEVELOPMENT_PLAN.md
├── Assets/
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── Networking/
│   │   ├── UI/
│   │   └── Game/
│   └── Art/
├── Packages/
├── ProjectSettings/
└── README.md
```

Do not commit generated Unity folders such as `Library/`, `Temp/`, `Logs/`, or build output.

## Roadmap

### MVP

- Ready/Play state.
- TCP connection.
- Waiting state.
- Match start.
- HP rendering.
- Attack/Defend/Heal input.
- Turn-aware button state.
- Win/Lose state.
- Play Again.
- Graceful connection error handling.

### Future

- Small action animations.
- Sound effects.
- Lobby/room browser.
- WebSocket/browser client as a separate experiment.
- True reconnect/session recovery if supported by the server.

## Development plan

See [`DEVELOPMENT_PLAN.md`](./docs/DEVELOPMENT_PLAN.md).

## WebGL CI/CD

GitHub Actions builds a WebGL artifact for every pull request and push to
`main`. Builds from `main` are deployed atomically to the DigitalOcean droplet:
the workflow uploads a release to `releases/<commit SHA>` and repoints the
`current` symlink only after the upload succeeds.

Configure these repository-level GitHub Actions values:

| Type | Name | Purpose |
| --- | --- | --- |
| Secret | `UNITY_LICENSE` | Unity activation file used by GameCI. |
| Secret | `DEPLOY_SSH_KEY` | Private key for the restricted SSH deployment user. |
| Variable | `DEPLOY_HOST` | Droplet hostname or IP address. |
| Variable | `DEPLOY_USER` | Restricted SSH deployment user. |
| Variable | `DEPLOY_KNOWN_HOSTS` | Pinned SSH host key entry for the droplet. |
| Variable | `DEPLOY_PATH` | Absolute release root, for example `/var/www/go-heroes`. |

Configure Nginx to serve `${DEPLOY_PATH}/current` as the WebGL site's root.
The deployed game must use `wss://` and Nginx must proxy `/ws` to the Go
server's private WebSocket listener. By default, the Unity client derives its
WebSocket URL from the page origin (for example, `https://game.example.com`
uses `wss://game.example.com/ws`). Set `webSocketUrl` in the Unity Inspector
only when the game and WebSocket endpoint intentionally use different origins.

## Status

**Planning / learning project**

The first milestone is complete when two running Unity clients can press Play, be matched by the Go server, finish a duel, see their result, and use Play Again.

## Related project

[Go Heroes Server](https://github.com/marcelobruzetti/go-heroes-server) contains the authoritative Go backend. The two repositories communicate only through the documented network protocol.
