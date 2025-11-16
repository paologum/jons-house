# Fishing backend wiring notes

Files added:

- `FishData.cs` - ScriptableObject describing individual fish.
- `FishDatabase.cs` - Component holding a list of `FishData` assets and selecting fish by weight.
- `FishingManager.cs` - High-level backend that simulates casting, bites, and emits events when a fish is hooked.
- `FishingMinigame.cs` - Stardew-like minigame controller. Wire UI elements (RectTransforms and Slider) in the inspector.
- `FishingDebug.cs` - Simple debug harness to start fishing with the F key and log events.

Quick wiring steps (Fishing scene):

1. Create a GameObject (e.g. `FishingManager`) and add the `FishingManager` component.

   - Assign a `FishDatabase` (see step 2) or add the `FishDatabase` component to the same GameObject and populate `fishes` with `FishData` assets.

2. Create `FishData` assets: Right-click in Project window -> Create -> Fishing -> FishData. Fill name, icon, selectionWeight, baseCatchRate, minigameDifficulty.

3. Create UI for the minigame (Canvas):

   - Create an empty GameObject to hold the play area. Add a `RectTransform` and size it vertically (this is `playArea`).
   - Add an Image child for `fishIcon` with a small sprite centered horizontally.
   - Add an Image child for `playerBar` (a tall thin rectangle) anchored horizontally and movable vertically.
   - Add a `Slider` for `progressBar` (fill meter) and style as you like.
   - Add a `CanvasGroup` on the root minigame panel to show/hide easily.

4. Create a GameObject (e.g. `FishingMinigame`) and add the `FishingMinigame` script. Wire `panel`, `playArea`, `fishIcon`, `playerBar`, and `progressBar` to the UI elements.

5. Create a `FishingDebug` GameObject and assign the `FishingManager` and `FishingMinigame` references, or just leave them empty and the script will auto-find them.

Running the debug:

- Enter Play mode and press F. The debug harness will call `StartFishing()` on the manager, which after a random delay will pick a fish and invoke `OnFishHooked`.
- `FishingDebug` subscribes and starts the `FishingMinigame` when a fish is hooked. The minigame uses Vertical input (W/S or Up/Down) to move the player bar.

Inventory UI (new):

- The project now includes a simple persistent inventory for caught fish. Files added:
  - `CaughtFish.cs` — serializable data for a caught fish (name, difficulty, timestamp).
  - `FishingInventory.cs` — singleton that stores and persists a list of `CaughtFish` to JSON at `Application.persistentDataPath/fishing_inventory.json`.
  - `FishingInventoryUI.cs` — controller to populate a UI content area with item prefabs.
  - `FishingInventoryItemUI.cs` — small helper to display a caught fish (name, time) and optionally delete it.

Wiring the inventory UI:

1. Create a UI panel for the inventory (Canvas). Add a `Scroll View` or a `Vertical Layout Group` content object to hold items.
2. Create an item prefab (e.g. `FishingInventoryItem`) with the following structure:
   - `TMP_Text` for the fish name (assign to `nameText` on `FishingInventoryItemUI`).
   - `TMP_Text` for the caught time (assign to `timeText`).
   - Optional `Image` for an icon (assign to `iconImage`).
   - Optional `Button` for delete (assign to `deleteButton`).
   - Add the `FishingInventoryItemUI` script to the prefab.
3. Add the `FishingInventoryUI` script to the inventory panel and set `contentParent` to the Scroll View's content transform and `itemPrefab` to your prefab.

Behavior and persistence:

- `FishingDebug` now adds caught fish to `FishingInventory` when a minigame succeeds. Inventory changes are saved to disk immediately and loaded on startup.
- Use `FishingInventoryUI.RefreshUI()` to programmatically refresh the UI, or rely on the `OnInventoryChanged` event which `FishingInventoryUI` subscribes to.

Notes & next steps:

- Tweak `FishData.baseCatchRate` and `minigameDifficulty` to vary challenge and catch likelihood.
- Consider adding icons on `CaughtFish` (store asset GUIDs or an index) so the UI can display the fish sprite.
- Add sound/visual feedback when catching or losing fish, and a small confirmation modal for deleting entries.
- If you want the minigame to use mouse controls or a different input scheme, update `FishingMinigame.Update()` accordingly.
