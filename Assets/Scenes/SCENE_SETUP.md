# Scene Hierarchy Setup Guide

This guide provides the complete hierarchy structure for the HouseScene.

## Complete Scene Hierarchy

```
HouseScene
├── Main Camera
│   └── CameraController (Script)
├── GameManager
│   └── GameManager (Script)
├── UIManager
│   └── InteractionUI (Script)
├── Canvas
│   ├── MemoryPanel (Panel)
│   │   ├── TitleText (TextMeshPro)
│   │   ├── StoryText (TextMeshPro)
│   │   ├── MemoryImage (Image)
│   │   └── CloseButton (Button)
│   └── InteractionHint (Panel)
│       └── HintText (TextMeshPro)
├── EventSystem
├── --- Environment ---
├── Floor
│   └── SpriteRenderer
├── WallTop
│   ├── SpriteRenderer
│   └── BoxCollider2D
├── WallBottom
│   ├── SpriteRenderer
│   └── BoxCollider2D
├── WallLeft
│   ├── SpriteRenderer
│   └── BoxCollider2D
├── WallRight
│   ├── SpriteRenderer
│   └── BoxCollider2D
├── --- Player ---
├── Player (Tag: Player)
│   ├── SpriteRenderer
│   ├── Rigidbody2D
│   ├── BoxCollider2D
│   └── PlayerController (Script)
├── --- Interactable Objects ---
├── Couch
│   ├── SpriteRenderer
│   ├── BoxCollider2D (Trigger)
│   └── InteractableObject (Script)
├── PhotoFrame
│   ├── SpriteRenderer
│   ├── BoxCollider2D (Trigger)
│   └── InteractableObject (Script)
├── RamenBowl
│   ├── SpriteRenderer
│   ├── BoxCollider2D (Trigger)
│   └── InteractableObject (Script)
├── ConcertTickets
│   ├── SpriteRenderer
│   ├── BoxCollider2D (Trigger)
│   └── InteractableObject (Script)
├── Plant
│   ├── SpriteRenderer
│   ├── BoxCollider2D (Trigger)
│   └── InteractableObject (Script)
└── GameConsole
    ├── SpriteRenderer
    ├── BoxCollider2D (Trigger)
    └── InteractableObject (Script)
```

## Detailed Setup Instructions

### Main Camera Setup

1. Select Main Camera in Hierarchy
2. Set these properties:
   - Position: (0, 0, -10)
   - Projection: Orthographic
   - Size: 5
   - Background Color: #2C3E50 (or your choice)
3. Add `CameraController` script
4. In CameraController:
   - Target: Drag Player from Hierarchy
   - Smooth Speed: 0.125
   - Offset: (0, 0, -10)
   - Use Bounds: ✓
   - Min Bounds: (-7, -5)
   - Max Bounds: (7, 5)

### GameManager Setup

1. Create Empty GameObject: `GameObject → Create Empty`
2. Rename to "GameManager"
3. Add `GameManager` script
4. Position: (0, 0, 0)

### UIManager Setup

1. Create Empty GameObject: `GameObject → Create Empty`
2. Rename to "UIManager"
3. Add `InteractionUI` script
4. Connect references (see Canvas setup below)

### Canvas Setup

1. Create Canvas: `GameObject → UI → Canvas`
2. Set Canvas Scaler:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
   - Match: 0.5

#### Memory Panel

1. Right-click Canvas → `UI → Panel`
2. Rename to "MemoryPanel"
3. Settings:
   - Anchors: Center
   - Pos X: 0, Pos Y: 0
   - Width: 600, Height: 400
   - Color: White with Alpha ~240

**TitleText (TextMeshPro)**:
1. Right-click MemoryPanel → `UI → Text - TextMeshPro`
2. Rename to "TitleText"
3. Settings:
   - Anchors: Top Center
   - Pos Y: -30
   - Width: 550, Height: 60
   - Font Size: 32
   - Alignment: Center
   - Color: Dark Blue (#2C3E50)

**StoryText (TextMeshPro)**:
1. Right-click MemoryPanel → `UI → Text - TextMeshPro`
2. Rename to "StoryText"
3. Settings:
   - Anchors: Middle Center
   - Width: 550, Height: 250
   - Font Size: 18
   - Alignment: Center & Top
   - Color: Dark Gray (#34495E)
   - Enable Word Wrapping

**MemoryImage (Image)**:
1. Right-click MemoryPanel → `UI → Image`
2. Rename to "MemoryImage"
3. Settings:
   - Between Title and Story
   - Width: 400, Height: 150
   - Preserve Aspect: ✓
   - Initially: Set Active = false

**CloseButton (Button)**:
1. Right-click MemoryPanel → `UI → Button - TextMeshPro`
2. Rename to "CloseButton"
3. Settings:
   - Anchors: Top Right
   - Pos X: -15, Pos Y: -15
   - Width: 30, Height: 30
   - Colors: Normal = Red (#E74C3C)
   - Text: "×" (multiply symbol)
   - Font Size: 24

#### Interaction Hint

1. Right-click Canvas → `UI → Panel`
2. Rename to "InteractionHint"
3. Settings:
   - Anchors: Bottom Center
   - Pos Y: 50
   - Width: 400, Height: 50
   - Color: Black with Alpha ~180
   - Initially: Set Active = false

**HintText (TextMeshPro)**:
1. Right-click InteractionHint → `UI → Text - TextMeshPro`
2. Rename to "HintText"
3. Settings:
   - Stretch to fill parent
   - Font Size: 16
   - Alignment: Center
   - Color: White

### Connect UIManager References

1. Select UIManager in Hierarchy
2. In InteractionUI component, drag these references:
   - Memory Panel: MemoryPanel
   - Title Text: TitleText
   - Story Text: StoryText
   - Memory Image Display: MemoryImage
   - Close Button: CloseButton
   - Interaction Hint: InteractionHint
   - Hint Text: HintText

### Floor Setup

1. Create Sprite: `GameObject → 2D Object → Sprite → Square`
2. Rename to "Floor"
3. Settings:
   - Position: (0, 0, 1)
   - Scale: (15, 10, 1)
   - Color: Tan (#D2B48C)
   - Sorting Layer: Default
   - Order in Layer: -10

### Walls Setup

Create 4 walls with these settings:

**WallTop**:
- Position: (0, 5, 0)
- Scale: (15, 0.5, 1)
- Color: Brown (#8B7355)
- Add BoxCollider2D (not trigger)

**WallBottom**:
- Position: (0, -5, 0)
- Scale: (15, 0.5, 1)
- Color: Brown (#8B7355)
- Add BoxCollider2D (not trigger)

**WallLeft**:
- Position: (-7.5, 0, 0)
- Scale: (0.5, 10, 1)
- Color: Brown (#8B7355)
- Add BoxCollider2D (not trigger)

**WallRight**:
- Position: (7.5, 0, 0)
- Scale: (0.5, 10, 1)
- Color: Brown (#8B7355)
- Add BoxCollider2D (not trigger)

### Player Setup

1. Create Sprite: `GameObject → 2D Object → Sprite → Square`
2. Rename to "Player"
3. Set Tag to "Player" (create if doesn't exist)
4. Settings:
   - Position: (0, 0, 0)
   - Scale: (0.6, 0.8, 1)
   - Color: Pink (#FF69B4)
5. Add Components:
   - Rigidbody2D:
     * Body Type: Dynamic
     * Gravity Scale: 0
     * Constraints: Freeze Rotation Z
     * Collision Detection: Continuous
   - BoxCollider2D:
     * No special settings
   - PlayerController script

### Interactable Objects Setup

Follow the [PREFAB_SETUP.md](PREFAB_SETUP.md) guide for detailed setup of each of the 6 interactable objects.

## Testing Your Scene

1. **Play Mode**: Press Play button
2. **Movement**: Use WASD or Arrow keys to move around
3. **Collisions**: Verify player can't walk through walls or objects
4. **Interactions**: Walk near objects to see glow effect
5. **Memory Display**: Press E when near object to see memory panel
6. **Close Panel**: Click X button or press ESC

## Common Issues

### Player falls through floor
- Check Rigidbody2D Gravity Scale is 0
- Verify Floor doesn't have a collider

### Can't interact with objects
- Ensure objects have BoxCollider2D with "Is Trigger" checked
- Verify Player has "Player" tag
- Check InteractionUI references are connected

### UI doesn't show
- Verify Canvas has EventSystem
- Check UIManager script references
- Ensure MemoryPanel starts inactive

### Camera doesn't follow
- Check CameraController Target is set to Player
- Verify camera is at Z: -10

## Performance Tips

- Keep sprite sizes reasonable
- Use sprite atlases for multiple sprites
- Set appropriate Quality Settings
- Use object pooling if you add many objects later
