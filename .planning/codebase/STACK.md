# Technology Stack

**Analysis Date:** 2026-06-22

## Languages

**Primary:**
- C# — all game logic, editor tools, and patterns under `Assets/_Project/`

**No secondary languages.** Shader assets use Unity's ShaderGraph (visual, no HLSL files found).

## Runtime

**Environment:**
- Unity Engine 6000.3.11f1 (exact revision `3000ef702840`)
- Target: standalone desktop (PC)
- Color space: Linear (`m_ActiveColorSpace: 1`)
- Rendering: multithreaded (`m_MTRendering: 1`)

**No external runtime** (Node.js, Python, etc.) — pure Unity project, run via Unity Editor Play mode or built from *File → Build Settings*.

**Package Manager:**
- Unity Package Manager (UPM)
- Lockfile: `Packages/packages-lock.json` — present and committed

## Frameworks

**Core:**
- Universal Render Pipeline (URP) 17.3.0 — render pipeline; configured in `Assets/Settings/PC_RPAsset.asset` and `Assets/Settings/Mobile_RPAsset.asset`
- Shader Graph 17.3.0 — dependency of URP, available for custom shaders
- UGUI 2.0.0 — Unity UI system (Canvas, Image, etc.)
- TextMesh Pro — font/UI text rendering; assets present at `Assets/TextMesh Pro/`

**Navigation:**
- AI Navigation 2.0.11 (`com.unity.ai.navigation`) — NavMesh baking/runtime for NPC or obstacle pathfinding (installed, not actively used in existing scripts)

**Animation / Sequencing:**
- Timeline 1.8.11 (`com.unity.timeline`) — cutscene/sequencer support (installed)

**Visual Scripting:**
- Visual Scripting 1.9.10 (`com.unity.visualscripting`) — installed but not used in game code

**Testing:**
- Unity Test Framework 1.6.0 (`com.unity.test-framework`) — installed; no tests written yet
- NUnit (via `com.unity.ext.nunit` 2.0.5) — assertion library bundled with test framework

**Build/Dev:**
- Rider IDE integration 3.0.39 (`com.unity.ide.rider`)
- Visual Studio IDE integration 2.0.26 (`com.unity.ide.visualstudio`)
- Unity Version Control (Collab Proxy) 2.11.4 (`com.unity.collab-proxy`)

**Performance:**
- Burst Compiler 1.8.28 (`com.unity.burst`) — transitive dependency of URP core; available
- Collections 2.6.2 (`com.unity.collections`) — NativeContainers; transitive dependency
- Mathematics 1.3.3 (`com.unity.mathematics`) — transitive dependency of URP/Burst

## Key Dependencies

**Critical:**
- `com.unity.render-pipelines.universal` 17.3.0 — entire rendering pipeline; shaders, materials, post-processing all depend on this
- `com.unity.ugui` 2.0.0 — UI system; removing breaks any Canvas-based HUD
- `com.unity.nuget.newtonsoft-json` 3.2.2 — pulled in by `unity-mcp` and `com.unity.ai.assistant`; JSON serialization

**Infrastructure:**
- `com.unity.modules.physics` 1.0.0 — `Rigidbody` used by `CarController.cs`; `CharacterController` used by `PlayerMovement.cs`
- `com.unity.modules.audio` 1.0.0 — `AudioSource`, `AudioListener` used throughout
- `com.unity.modules.ai` 1.0.0 — NavMesh system
- `com.unity.modules.vehicles` 1.0.0 — WheelCollider support (available, not used in current car implementation which uses raw Rigidbody)
- `com.unity.inputsystem` 1.19.0 — **installed but NOT used in game code**; all input uses legacy `UnityEngine.Input` API. An `.inputactions` asset exists at `Assets/_Project/Core/Input/InputSystem_Actions.inputactions` but is unused by scripts.

**Development-only (local file dependency):**
- `com.coplaydev.unity-mcp` — loaded from local path `C:/Users/matia/Downloads/unity-mcp-beta/MCPForUnity`; this is a developer tool dependency and will not resolve on other machines unless the path exists

## Configuration

**Environment:**
- No `.env` files — this is a local game, no environment variables
- No secrets or API keys required to run the project

**Build:**
- `ProjectSettings/ProjectSettings.asset` — product name `drunk-simulator`, company `DefaultCompany`, default resolution 1024×768
- `ProjectSettings/EditorBuildSettings.asset` — build scene order: `Home` → `City` → `Bar`
- `Assets/Settings/PC_RPAsset.asset` — URP pipeline asset for PC
- `Assets/Settings/Mobile_RPAsset.asset` — URP pipeline asset for Mobile
- `Assets/Settings/PC_Renderer.asset` / `Mobile_Renderer.asset` — URP renderer data per platform
- `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` — global URP settings

**Input (legacy API):**
- Uses `UnityEngine.Input`, `Input.GetKeyDown`, `Input.GetAxis` throughout
- Do NOT switch to the new Input System; keep legacy API for all new input code

## Platform Requirements

**Development:**
- Unity Editor 6000.3.11f1 (exact version required)
- Windows or macOS — project was developed on Windows (path in `com.coplaydev.unity-mcp` references `C:/Users/matia/`)
- No CLI build scripts; build via *File → Build Settings* in the Editor
- Run tests via *Window → General → Test Runner*

**Production:**
- Standalone PC build (Windows/macOS)
- No mobile, WebGL, or console targets configured
- No build automation or CI pipeline present

---

*Stack analysis: 2026-06-22*
