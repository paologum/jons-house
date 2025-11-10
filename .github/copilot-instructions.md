## Jon's House — Copilot instructions (concise)

Purpose: Make small, safe code edits and assist with Unity editor setup and gameplay tweaks. This file contains repository-specific facts, conventions, and quick pointers an AI coding agent should follow to be productive immediately.

- Project type: Unity 2D (recommended Unity 6000.2.5f1 or later). Main scene: `Assets/Scenes/HouseScene.unity`.
- How to run locally: Open the project in Unity Hub (Add project → select this folder), open the `HouseScene` scene and press Play.

- Key scripts (single-file responsibilities):

  - `Assets/Scripts/PlayerController.cs` — movement input (WASD/Arrows), uses Rigidbody2D (GravityScale = 0) and MovePosition.
  - `Assets/Scripts/InteractableObject.cs` — per-object memory data (title, story, optional image), glow effect, interaction range and E-key trigger.
  - `Assets/Scripts/InteractionUI.cs` — memory panel, interaction hint, and pause behavior (sets Time.timeScale = 0 when showing a memory).
  - `Assets/Scripts/MemoryData.cs` — ScriptableObject shape for reusable memory assets.
  - `Assets/Scripts/CameraController.cs` — smooth follow and optional bounds.
  - `Assets/Scripts/GameManager.cs` — simple singleton, global state and initialization.

- Project conventions and gotchas (do not assume defaults):

  - Player GameObject MUST have the tag `Player` for interactions to work.
  - Interactable objects require a `Box Collider 2D` with `Is Trigger` = true and the `InteractableObject` script attached.
  - Player Rigidbody2D should have Gravity Scale = 0 and Freeze Rotation enabled for top-down movement.
  - Interaction key: E. Close panel: ESC. Interaction hint text is managed by `InteractionUI`.
  - UI uses TextMeshPro; ensure `Packages/com.unity.textmeshpro` is present when editing UI code.

- Typical workflows an agent may automate or modify:

  - Add a new memory object: create sprite → add `InteractableObject` → set Memory Title/Story/InteractionRange/GlowColor → ensure collider trigger and tag settings.
  - Wire UI: create a `UIManager` GameObject, add `InteractionUI` component and link MemoryPanel, TitleText, StoryText, Image, CloseButton, InteractionHint and HintText (see `UNITY_SETUP.md`).
  - Scene changes: prefer editing `Assets/Scenes/HouseScene.unity` or creating prefabs under `Assets/Prefabs/` and documenting prefab notes in `Assets/Prefabs/PREFAB_SETUP.md`.

- Where to look for bugs (short checklist):

  - Interaction doesn't show: check `Player` tag, collider IsTrigger, `InteractableObject` attached, and `InteractionUI` references.
  - Player doesn't move: ensure `PlayerController` is attached, Rigidbody2D GravityScale = 0 and collision settings.
  - UI missing text/images: verify TextMeshPro package and `InteractionUI` inspector references.

- Integration & external dependencies:

  - Uses built-in Unity packages only (TextMeshPro, UI). There is an optional `com.unity.collab-proxy` package (Collaborate) that has historically caused an ambiguous `TabView` name conflict in some editor versions — if you see CS0104 referencing `TabView`, either remove/disable the Collaborate package in `Packages/manifest.json` or prefer adding explicit namespace aliases locally in project code rather than editing package sources.

- Editing rules for AI agents (must follow):

  1. Never modify files under `Library/` or `Library/PackageCache/` — these are generated or managed by Unity and will be overwritten. If you must change package behaviour, use a local package override under `Packages/` or change your own project code instead.
  2. Prefer non-invasive changes: update `Assets/` scripts, prefabs, and documentation files. If you need to change a package, propose a local package override and get human approval.
  3. When changing serialized fields used by the Editor (e.g., public/SerializeField fields), update README/SCENE_SETUP.md or PREFAB_SETUP.md to keep inspector wiring reproducible.

- Useful file references (examples):
  - Scene setup: `Assets/Scenes/SCENE_SETUP.md` and `Assets/Scenes/HouseScene.unity`
  - Prefab guidance: `Assets/Prefabs/PREFAB_SETUP.md`
  - Project quickstart: `QUICKSTART.md`, `UNITY_SETUP.md`

If anything in these instructions is unclear or you want the tone adjusted (more prescriptive or more exploratory), tell me which sections to expand or redact and I will iterate.
