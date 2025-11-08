# Quick Start Guide

Get the game running in 5 minutes!

## Prerequisites

- Unity Hub installed
- Unity 6000.2.5f1 or later installed

## Setup Steps

### 1. Open the Project (1 minute)

```bash
# Open Unity Hub
# Click "Add" → "Add project from disk"
# Navigate to the jons-house folder and select it
# Click on the project to open it in Unity
```

### 2. Create Basic Scene (3 minutes)

Once Unity Editor opens:

**Setup Camera:**
1. Select `Main Camera`
2. Set Projection to `Orthographic`, Size to `5`
3. Add `CameraController` script component

**Create Player:**
1. `GameObject` → `2D Object` → `Sprite` → `Square`
2. Rename to "Player", add tag "Player"
3. Add `Rigidbody2D` (Gravity Scale = 0)
4. Add `Box Collider 2D`
5. Add `PlayerController` script
6. Change color to Pink (#FF69B4)

**Create Floor:**
1. `GameObject` → `2D Object` → `Sprite` → `Square`
2. Rename to "Floor"
3. Scale: X=15, Y=10
4. Position: Z=1
5. Color: Tan (#D2B48C)

**Create One Object:**
1. `GameObject` → `2D Object` → `Sprite` → `Square`
2. Rename to "Couch"
3. Add `Box Collider 2D` (check "Is Trigger")
4. Add `InteractableObject` script
5. Fill in Memory Title and Story

**Setup UI:**
1. `GameObject` → `UI` → `Canvas`
2. Create empty GameObject named "UIManager"
3. Add `InteractionUI` script
4. (For full UI setup, see UNITY_SETUP.md)

### 3. Test (1 minute)

1. Press Play button
2. Use WASD to move
3. Walk to the Couch
4. Press E to interact

## Full Setup

For complete setup with all 6 objects and UI, follow:
- [UNITY_SETUP.md](UNITY_SETUP.md) - Complete instructions
- [SCENE_SETUP.md](Assets/Scenes/SCENE_SETUP.md) - Detailed scene hierarchy
- [PREFAB_SETUP.md](Assets/Prefabs/PREFAB_SETUP.md) - Object configurations

## Troubleshooting

**Player doesn't move?**
- Check Rigidbody2D Gravity Scale = 0
- Verify PlayerController script is attached

**Can't interact with objects?**
- Ensure Box Collider 2D has "Is Trigger" checked
- Player needs "Player" tag

**Need help?**
- Check the comprehensive guides above
- All scripts have inline comments
- Review the UNITY_SETUP.md troubleshooting section

## What's Included

✅ 6 C# Scripts (all game logic)
✅ Unity project structure
✅ 6 pre-configured memories
✅ Complete documentation
✅ Easy customization

Just set up the scene and play!
