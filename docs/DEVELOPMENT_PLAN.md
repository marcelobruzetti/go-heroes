# Go Heroes Unity — Development Plan

## 1. Goal

Build the smallest useful Unity client for Go Heroes.

The client should make the multiplayer server visible and playable without turning the learning project into a large Unity/game-development project.

## 2. MVP boundary

The UI follows four player-facing states:

```text
READY
  │ Play
  ▼
WAITING
  │ opponent found
  ▼
PLAYING
  │ match finished
  ▼
GAME OVER
  │ Play Again
  └──────────► WAITING
```

Connection failures may return the client to READY with an error message.

The MVP does not need:

- Character selection.
- Menus/settings.
- Accounts.
- Inventory.
- Levels/world.
- Complex animation.
- Audio system.
- Lobby/room browser.
- True reconnect/session recovery.

---

## Phase 1 — Unity project and static UI

### Objective

Reproduce the approved Go Heroes mockup before networking.

### Build

Create the primary screen with:

- `Go Heroes` title.
- Enemy HP bar.
- Player HP bar.
- Player sprite.
- Enemy sprite.
- Main content/action panel.
- Version label if desired.

Create the four UI states:

1. Ready — **Play**.
2. Waiting — `Waiting for opponent...`; action buttons disabled.
3. Playing — Attack, Defend, Heal.
4. Game Over — `You win!` / `You lose!` and **Play Again**.

### Done when

All four states can be switched manually in the Unity Editor without networking.

---

## Phase 2 — Client state model

### Objective

Avoid scattering UI state changes across button handlers.

### Suggested states

```text
Ready
Connecting
Waiting
Playing
GameOver
ConnectionError
```

`Connecting` and `ConnectionError` may be internal/temporary states even if the mockup does not dedicate a full screen to them.

### Tasks

- Represent the current client state explicitly.
- Centralize transitions.
- Enable/disable the appropriate controls per state.
- Keep networking logic out of UI components where practical.

### Done when

A test/debug controller can drive the complete UI flow deterministically.

---

## Phase 3 — Minimal TCP client

### Objective

Connect Unity to the Go Heroes Server.

### Tasks

- Create a TCP networking component.
- Connect to a configurable host/port.
- Read messages without blocking Unity's main thread.
- Send framed JSON messages.
- Detect connection loss.
- Dispose/close connections cleanly.
- Marshal received events to the Unity main thread before touching UI/GameObjects.

### Important boundary

The button is called **Play** because that is the player's intention.

Internally, Play may trigger:

```text
Play
 ↓
Connect
 ↓
Enter matchmaking
```

Do not expose networking terminology unnecessarily in the player-facing UI.

### Done when

Unity can connect to the Go server and display a server response.

---

## Phase 4 — Matchmaking UI

### Objective

Implement the Ready → Waiting → Playing transition.

### Tasks

- **Play** starts the connection/matchmaking flow.
- Show waiting state after the server accepts the player into matchmaking.
- Keep action buttons disabled while waiting.
- React to `match_started`.
- Render the initial HP values.
- Render whether it is the local player's turn.

### Done when

Two Unity instances can press Play and both transition into the same match.

---

## Phase 5 — Battle interaction

### Objective

Make Attack, Defend, and Heal playable.

### Input rule

Buttons send intentions only.

Conceptually:

```text
Attack button
     │
     ▼
{"type":"action","action":"attack"}
```

Unity must not subtract HP locally.

### Tasks

- Send Attack.
- Send Defend.
- Send Heal.
- Disable actions when it is not the local player's turn.
- Update both HP bars from server state.
- Handle rejected actions without corrupting local UI.
- Prevent accidental duplicate submissions while waiting for the authoritative update.

### Done when

Two Unity clients can play multiple turns while staying synchronized.

---

## Phase 6 — Game over and Play Again

### Objective

Implement the final approved mockup flow.

### Tasks

- Handle `game_over`.
- Display **You win!** to the winner.
- Display **You lose!** to the loser.
- Hide/disable battle controls.
- Show **Play Again**.
- On Play Again, request/enter matchmaking for a new match.
- Return to Waiting.
- Start the next match without restarting Unity.

### Terminology

**Play Again** is not true reconnect.

True reconnect means recovering a session after an unexpected network drop and is postponed.

### Done when

Both players can finish a match and independently use Play Again to enter matchmaking again.

---

## Phase 7 — Connection errors and cleanup

### Scenarios

Handle at least:

- Server unavailable when Play is pressed.
- Connection drops while waiting.
- Connection drops during a match.
- Malformed/unexpected server event.
- Unity application closes during a connection.
- Server closes the socket.

### UX

Keep error handling minimal. A short message and a path back to **Play** is enough for the MVP.

### Done when

Networking failures do not freeze Unity or leave action buttons in an invalid state.

---

## Phase 8 — Minimal polish

Only after the complete networking flow works.

Potential additions:

- Short attack movement/flash.
- Defend shield feedback.
- Heal feedback.
- Small turn indicator.
- Small event/status text.
- Basic sound effects.

These are optional and must not delay the networking milestone.

---

# MVP definition of done

The Unity MVP is complete when:

1. The screen matches the intended Go Heroes flow.
2. Play connects/enters matchmaking.
3. Waiting state is shown correctly.
4. Two clients enter a match.
5. HP comes exclusively from server state.
6. Attack, Defend, and Heal send intentions.
7. Turn availability follows server state.
8. Win/Lose is displayed from the server result.
9. Play Again returns the player to matchmaking.
10. Connection errors do not freeze/crash the client.

---

# Future evolution

This should be a separate iterations, not part of the MVP.

## Lobby and rooms

If the server adds rooms, the client may gain:

- Room list.
- Create room.
- Join room.
- Leave room.
- Room code/name.

## Visual polish

When the game is working, can be implemented:

- Better sprites.
- Animations.
- Sound.
- Transitions.

## True reconnect

If the server later implements sessions, add a reconnect flow capable of recovering an interrupted active match.

## Browser comparison

A future browser/WebSocket client should preferably be a separate project so the Unity repository stays focused.

---

# Client architecture guardrail

Keep three concerns conceptually separate:

```text
UI/Input
   │
Client/Game State
   │
Networking
```

Server rules must be only in the server. If Unity needs to know the result of an action, must wait for the server.