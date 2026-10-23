# Çiçek Bahçesi (working title) — Project Guide for Claude

## Project
- **Game:** A swipe-based match-3 flower puzzle game with levels. A personal gift from Utku to his girlfriend.
- **Design document:** `docs/GDD.md` — read it before implementing gameplay features. Do not add features listed as out of scope.
- **Second game.** Utku built Flappy Dog before this with Claude Code. He is a web developer (React/TypeScript) but still new to Unity.

## Tech
- Unity **6.3 LTS (6000.3.26f1)**, **Universal 2D (URP)**.
- Target: **Android**, **portrait 1080x1920**.
- C#. Input via the **Input System package**. Do not use the legacy `UnityEngine.Input` API.
- Git + GitHub. Unity `.gitignore` in place.

## Roles
- Claude writes code, sets up scenes, and sources/creates assets.
- Utku tests in the editor and on his phone and gives feedback. He does not set up the editor by hand — use editor menu scripts for scene setup (see below).
- Claude cannot see the editor or play the game unless a Unity MCP integration is connected. Never claim something works without evidence (Utku's report, Console output, passing tests, or a tool).

## Folder structure
```
Assets/_Project/
  Scripts/
    Board/      pure C# board logic (no UnityEngine dependency where possible)
    View/       MonoBehaviours that display and animate the board
    Levels/     level data and progression
    UI/
    Core/       game flow, save, audio
    Editor/     editor-only scripts (scene setup menus)
  Tests/        EditMode unit tests for board logic
  Prefabs/  Scenes/  Art/  Audio/  Fonts/  Data/
```

## Architecture rules
- **Board logic is separate from visuals.** Match detection, swaps, cascades, refill, shuffle, and valid-move checks live in plain C# classes and are covered by EditMode unit tests. The view layer only animates results.
- Write tests for board logic as you write the logic, and run them. Report results honestly.
- Board generation must never start with existing matches and must always have at least one valid move.
- Input is locked while animations run.
- Levels, tuning values, and personal messages live in ScriptableObjects or data files, not in code.
- Use `[SerializeField] private` fields, cache references in `Awake`, no allocations or `GetComponent` in `Update`.
- Pool flower objects.
- Keep it simple. No heavy frameworks or dependency injection.

## Unity-specific rules
- Never edit `.meta` files by hand. Never touch `Library/`, `Temp/`, `Logs/`.
- Avoid hand-editing `.unity`, `.prefab`, `.asset` YAML; use editor scripts instead.
- **Scene setup is done through an editor menu under the "Flowers" menu (e.g. "Flowers → Setup Scene").** Whenever you add a new component or serialized reference, update the setup menu so it wires it, and tell Utku explicitly to re-run the menu. (Lesson from Flappy Dog: missing wiring after code changes caused repeated UnassignedReference/NullReference errors.)
- In setup menus, load assets by path *after* `EditorSceneManager.NewScene`/`OpenScene`. Opening a scene unloads unused assets, so asset references held from before become destroyed objects and get wired as "None" (happened with the Flower prefab).
- Missing required references must fail once with a clear error in `Awake`, not spam errors every frame. Event unsubscription in `OnDisable` must be null-safe.
- Do not add packages without asking.

## Assets and licensing
- Only CC0 or licenses that clearly allow commercial use.
- Record every third-party asset's source and license in `docs/CREDITS.md`.
- Palette: cherry red, pink, **bordo (burgundy) — the main accent, her favorite color**, plus cream/off-white and a little leaf green. Flower types must be distinguishable by **silhouette**, not color alone (must read in grayscale).

## Workflow
- Small, testable steps. After each step, tell Utku exactly what to do and what he should see.
- Ask him to check the Console after each change.
- Suggest a commit after each working step. Do not commit or push without his approval.
- Debugging: observed vs expected, root cause, smallest fix, explain the cause.

## Communication
- Utku writes in Turkish. Reply in Turkish, casual and direct.
- Briefly explain important new concepts (what, why, common mistake). Skip obvious lines.
- End substantial tasks with: what changed, which files, what was verified, what remains.
