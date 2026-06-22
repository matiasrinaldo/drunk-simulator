# External Integrations

**Analysis Date:** 2026-06-22

## APIs & External Services

**None.** This is a fully offline, local Unity game. There are no HTTP calls, REST APIs, GraphQL endpoints, or cloud service SDKs used in gameplay code. The project does not call any external service at runtime.

**Developer tooling only (not runtime):**
- `com.unity.ai.assistant` 2.12.0-pre.2 — Unity Editor AI assistant package; connects to Unity's cloud services from within the Editor only, not at game runtime
- `com.coplaydev.unity-mcp` — local file dependency for Unity MCP (Model Context Protocol) integration; Editor-only development tool, loaded from `C:/Users/matia/Downloads/unity-mcp-beta/MCPForUnity`

## Data Storage

**Databases:** None.

**Persistence mechanism:** In-memory static classes that survive scene loads within a single play session. Reset when the game process exits. No disk I/O.

| Store | File | Purpose |
|-------|------|---------|
| `PlayerSpawner.NextSpawnId` | `Assets/_Project/Core/SceneManagement/PlayerSpawner.cs` | Which `SpawnPoint` to teleport player to on next scene load |
| `CarStateStore` | `Assets/_Project/Core/SceneManagement/CarStateStore.cs` | Car position/rotation across scene reloads |
| `DeliveredObjectsStore` | `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs` | Set of `CarryableObject` IDs already picked up (prevents respawn) |
| `PlayerPickup.hasHeldObject` | `Assets/_Project/Gameplay/Player/PlayerPickup.cs` | Static bool — whether player is carrying an object |

**File Storage:** None. No `System.IO`, `PlayerPrefs`, or file writes detected.

**Caching:** None (no Redis, Memcached, or local disk cache).

## Authentication & Identity

**Auth Provider:** None. Single-player offline game with no user accounts, login, or session management.

## Monitoring & Observability

**Error Tracking:** None. No Sentry, Datadog, Crashlytics, or equivalent.

**Logs:** Unity's built-in `Debug.Log` / `Debug.LogWarning` / `Debug.LogError`. Log messages are in Spanish and use `[ClassName]` prefix convention (e.g., `[BackgroundMusicManager]`, `[PlayerSpawner]`). Logs appear in the Unity Console and in `Logs/` (gitignored).

## CI/CD & Deployment

**Hosting:** Not applicable — local standalone game, no server hosting.

**CI Pipeline:** None. No GitHub Actions, Jenkins, or Unity Cloud Build configured.

**Version Control:** Git. Unity Version Control (Collab Proxy) package is installed (`com.unity.collab-proxy` 2.11.4) but the project uses plain Git.

## Third-Party Assets

These are static art/asset packs imported directly into the project — not runtime service integrations.

**Kenney CityKit:**
- Location: `Assets/ThirdParty/Kenney/CityKit/`
- Format used: FBX (`Assets/ThirdParty/Kenney/CityKit/Models/FBX format/`)
- Also included: GLB and OBJ formats (not actively referenced in code)
- Referenced by: `Assets/_Project/Editor/CityBuilder.cs` at constant `CityKitFBX = "Assets/ThirdParty/Kenney/CityKit/Models/FBX format/"`
- License: `Assets/ThirdParty/Kenney/CityKit/License.txt`

**ADG Textures (Plank Textures):**
- Location: `Assets/ThirdParty/ADG_Textures/Plank Textures/`
- Contents: 5 plank texture sets (`Planks1`–`Planks5`)
- Usage: applied to materials in `Assets/Art/Materials/`

**TutorialInfo (Unity sample scripts):**
- Location: `Assets/ThirdParty/TutorialInfo/`
- Files: `Readme.cs`, `Editor/ReadmeEditor.cs` — boilerplate Unity sample scripts; not used in gameplay

**TextMesh Pro:**
- Location: `Assets/TextMesh Pro/` — fonts and sprites
- Installed as a Unity built-in package; available for UI text rendering

## Audio Assets

Audio is loaded at runtime from `Assets/Resources/` using `Resources.Load<AudioClip>()` with fallback paths — no streaming service or CDN.

| Asset | Runtime Path | File |
|-------|-------------|------|
| Background music | `Audio/Music/BackgroundMusic` → `Audio/BackgroundMusic` | `Assets/Resources/Audio/BackgroundMusic.mp3` |
| Drink sip SFX | `Audio/SFX/DrinkSip` → `Audio/DrinkSip` | `Assets/Resources/Audio/SFX/DrinkSip.mp3` |
| Pay drink SFX | `Audio/SFX/PayDrink` → `Audio/PayDrink` | `Assets/Resources/Audio/SFX/PayDrink.mp3` |

Loader: `Assets/_Project/Core/Audio/BackgroundMusicManager.cs` and `Assets/_Project/Gameplay/Player/PlayerPickup.cs`.

Audio mixer assets are present at `Assets/_Project/Audio/Mixers/` but none of the gameplay scripts route through an `AudioMixer` at runtime (direct `AudioSource` volume is used instead).

## Webhooks & Callbacks

**Incoming:** None.

**Outgoing:** None.

## Environment Configuration

**Required env vars:** None. The game requires no environment variables to run.

**Secrets location:** None. No API keys, tokens, or credentials anywhere in the project.

---

*Integration audit: 2026-06-22*
