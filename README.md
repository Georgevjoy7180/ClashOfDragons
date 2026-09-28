# Clash Of Dragons

## Overview

A small 3D battle game: a player-controlled dragon fights an AI-controlled dragon inside a single
walled arena. Camera is an angled top-down 2.5D view (Teamfight Tactics / Dota Underlords style).
Both dragons share the exact same combat, health, and cooldown systems — there is no separate
"player version" and "AI version" of any ability.

Built entirely from Unity primitives (capsules, cubes) and procedural placeholder VFX — no imported
3D models, textures, or audio. This was a deliberate, explicit choice (see **Assets Used** below),
not an oversight: the brief prioritized working gameplay over visual polish, and the architecture
was built so real assets can be swapped in later without touching any gameplay code.

## Unity Version

Unity **6000.5.7f1** (Unity 6), Universal Render Pipeline 17.5.0.

Input is handled entirely through the **new Input System** (`com.unity.inputsystem` 1.20.0) —
Active Input Handling is set to "Input System Package (New)" only; the legacy Input Manager is
not used anywhere in this project.

## Controls

```
WASD — Movement
Q     — Fire Attack
E     — Tail Attack
R     — Fly Attack
```

Movement is world-space (not camera-relative). The dragon rotates to face its movement direction.

## Combat

Both dragons carry the same three abilities, with independent cooldowns:

| Ability | Key | Damage | Range | Cooldown | Notes |
|---|---|---|---|---|---|
| Fire Attack | Q | 20 | 7 | 4s | Ranged, frontal 120° cone, beam + particle burst VFX |
| Tail Attack | E | 25 | 2.5 | 3s | Melee, any facing, applies knockback, particle burst VFX |
| Fly Attack | R | 35 | — | 8s | Multi-stage: Takeoff → Rise → Move/Attack → Deal Damage → Land. Disables movement/other abilities for its duration |

Health defaults to 100 for both dragons. Either dragon reaching 0 HP stops the fight, disables both
dragons' controllers, and shows a winner panel ("PLAYER WINS" / "AI DRAGON WINS") with a Restart
button that reloads the battle scene.

Hit feedback: a brief scale "punch" on the attacker and a color flash on the dragon that took
damage, plus per-ability particle bursts and knockback on Tail Attack.

## Enemy AI

A simple finite state machine — **Idle / Chase / Attack** — not a behaviour tree. Each physics
step it re-evaluates distance to the player:

- Far away → **Chase** (move toward the player)
- Fly Attack ready *and* player isn't already in melee range → use it opportunistically
- Within Tail range → **Attack** (Tail)
- Within Fire range → **Attack** (Fire)

Critically, the AI calls the exact same `DragonCombat.TryUseAbility(...)` method the player's input
handler calls — it runs on the same cooldowns as the player because it's the same underlying
system, not a parallel one.

## Architecture

```
PlayerController          DragonAI
       |                      |
       +----------+-----------+
                  |
            DragonCombat
                  |
      +-----------+-----------+
      |           |           |
  FireAttack  TailAttack  FlyAttack   (all : AbilityBase)
      |           |           |
      +-----------+-----------+
                  |
            DragonHealth
```

- **`PlayerController`** / **`DragonAI`** decide *when* to move or attack (input vs. distance-based
  decisions). Neither knows *how* an attack works.
- **`DragonCombat`** is the shared hub. It auto-discovers whichever `AbilityBase` components are
  attached to the dragon (`GetComponents<AbilityBase>()`) and indexes them by an `AbilitySlot` enum
  (Fire/Tail/Fly) — callers just say `TryUseAbility(AbilitySlot.Fire)`. This is what makes the
  player/AI sharing requirement real: adding Fly Attack meant adding one component, not touching
  `DragonCombat`, `PlayerController`, or `DragonAI`.
- **`AbilityBase`** holds the common cooldown timer, an `IsBusy` flag (used by Fly Attack to lock
  out movement/other abilities for its duration), and an `OnActivated` event — the animation hook
  every concrete ability gets for free.
- **`DragonHealth`** is fully decoupled from combat — it only knows `TakeDamage`/`Heal`/`Die` and
  exposes `OnHealthChanged`/`OnDeath` events, which the UI and `GameManager` subscribe to instead of
  polling every frame.
- **`Visual` is a separate child object** under each dragon's root, holding only the mesh/renderer.
  Gameplay code (`PlayerController`, `DragonAI`, `DragonCombat`, all three abilities) only ever
  references the root `Transform`/`Rigidbody`/`CapsuleCollider` — never `Visual`. Swapping the
  placeholder capsule for a real rigged dragon model later means replacing what's under `Visual`,
  nothing else.
- **`DragonVisualFeedback`** stands in for a real Animator: it listens to the same
  `AbilityBase.OnActivated` / `DragonHealth.OnHealthChanged` hooks a real Animator Controller would,
  and drives simple procedural scale/color feedback instead. Replacing it with a real rig-driven
  Animator later doesn't require touching any combat code, since the hooks already exist.
- **`VfxUtility`** is a shared placeholder particle-burst helper used by all three abilities, so the
  ~20 lines of `ParticleSystem` setup exist once, not three times.

### Editor tooling (`Scripts/Editor/`)

The scene (arena, dragons, HUD, GameManager, lighting) is built entirely through a set of Editor
menu commands (`Clash Of Dragons → Build ...`) rather than by hand-placing GameObjects or
hand-authoring `.unity`/`.mat` YAML. Each tool uses Unity's own APIs (`GameObject.CreatePrimitive`,
`AssetDatabase.CreateAsset`, etc.) so the Editor itself writes the scene/asset files correctly, and
every tool is idempotent — re-running it updates existing objects instead of duplicating them. This
was a deliberate choice to avoid manually authoring scene files by hand, and it made rebuilding or
re-wiring the scene after code changes fast and reliable throughout development.

## Assets Used

- **No imported 3D models** — both dragons are Unity primitive capsules. Deliberately deferred
  (visual polish is explicitly the lowest priority in the brief); the architecture above means real
  models can be swapped in later by only touching each dragon's `Visual` child.
- **No imported audio** — deliberately out of scope for this assessment's priorities.
- **No imported textures/materials** — all materials are simple solid-color URP Lit/Unlit materials
  created via script.
- **VFX** are Unity's built-in `ParticleSystem` and `LineRenderer`, procedurally configured — no
  particle textures.
- **UI font** is Unity's built-in legacy font (`LegacyRuntime.ttf`) — no imported font asset.
- **Post-processing**: a URP Global Volume (Bloom, Vignette, Neutral Tonemapping, Color Adjustments)
  for lighting polish — no external LUTs or profiles.

## How to Run

1. Open the project in Unity **6000.5.7f1** or a compatible Unity 6 release.
2. Open `Assets/_Game/Scenes/BattleScene.unity`.
3. Press **Play**.

## How to Build

1. Ensure the **Windows Build Support (IL2CPP)** module is installed for your Unity version (Unity
   Hub → Installs → gear icon → Add Modules).
2. In the Editor menu: **Clash Of Dragons → Build Windows (Phase 20)**.
3. The build is written to `Builds/Windows/ClashOfDragons.exe`, next to (not inside) `Assets/`.

This build command also forces Build Settings to contain only `BattleScene` (so the game boots
into the right scene, not the original template scene), enables Run In Background, and registers
a shader used only by runtime-generated VFX materials so it survives Unity's build-time shader
stripping (see **AI Usage** below for how that last one was found).

## AI Usage

This project was developed with **Claude Code** as an AI pair-programming tool, used for:

- **Planning** — breaking the brief into 21 incremental, independently-testable phases and
  proposing the shared `DragonCombat`/`AbilityBase` architecture up front.
- **Code assistance** — writing the gameplay scripts and the Editor tooling that builds the scene.
- **Debugging** — diagnosing issues from Console output and screenshots (see below).
- **Refactoring** — e.g. consolidating duplicated Editor-tool helper code into
  `EditorSceneUtility` as it started repeating across `ArenaBuilder`/`CharacterBuilder`/`HudBuilder`.

**AI mistake encountered:**
Fly Attack worked correctly in the Editor but got permanently stuck mid-air in every standalone
Windows build. The root cause: `FireAttack`/`VfxUtility` create their placeholder VFX materials at
runtime via `Shader.Find("Universal Render Pipeline/Unlit")` — but no serialized Material asset in
the project actually references that shader, so Unity's build-time shader stripping silently
dropped it from the build. `Shader.Find` then returned `null`, and `new Material(null)` threw —
and because that call happens *inside* `FlyAttack`'s coroutine (via the "deal damage" step), the
exception killed the coroutine outright, permanently stranding the dragon mid-sequence with its
"busy" flag stuck on. It only ever showed up in a real build, never in Play mode, which made it easy
to miss until the actual Windows build test in Phase 20. Fixed by explicitly registering the shader
in Graphics Settings' Always Included Shaders list at build time, plus adding a fallback so VFX
creation degrades gracefully instead of throwing if a shader is ever missing again.
