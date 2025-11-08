# Jon's House - Unity 2D Game

A 2D top-down interactive house game where players explore memories through interactable objects.

## Project Overview

This Unity project creates an interactive 2D experience where:
- Your girlfriend controls a character sprite
- She walks around a top-down house environment
- Objects (couch, photo frame, ramen bowl, tickets, etc.) glow when nearby
- Pressing 'E' shows a memory panel with stories from your relationship

## Unity Version

- **Recommended**: Unity 6000.2.5f1 or later
- **Project Type**: 2D

## Project Structure

```
Assets/
├── Scripts/          # C# game scripts
│   ├── PlayerController.cs      # Player movement and controls
│   ├── InteractableObject.cs    # Object interaction logic
│   ├── InteractionUI.cs         # UI panel management
│   ├── CameraController.cs      # Camera follow system
│   ├── GameManager.cs           # Game state management
│   └── MemoryData.cs            # Memory data ScriptableObject
├── Scenes/          # Game scenes
├── Prefabs/         # Reusable game objects
├── Sprites/         # 2D graphics and textures
└── Materials/       # Materials for sprites

ProjectSettings/     # Unity project settings
Packages/           # Package dependencies
```

## Setup Instructions

### 1. Open in Unity

1. Open Unity Hub
2. Click "Add" → "Add project from disk"
3. Select the `jons-house` folder
4. Unity will import the project

### 2. Create the Main Scene

1. Create a new 2D scene: `File` → `New Scene` → `2D`
2. Save it as `Assets/Scenes/HouseScene.unity`

### 3. Set Up the Player

1. Create a new 2D GameObject: `GameObject` → `2D Object` → `Sprite` → `Square`
2. Rename it to "Player"
3. Add Tag "Player" in the Inspector
4. Add components:
   - `Rigidbody2D` (set Gravity Scale to 0)
   - `Box Collider 2D`
   - `PlayerController` script
5. Change the sprite color to pink (#FF69B4) to represent the girlfriend character

### 4. Set Up the Camera

1. Select the Main Camera
2. Set Projection to `Orthographic`
3. Set Size to `5` (or adjust for desired zoom level)
4. Add the `CameraController` script
5. Drag the Player object to the Target field

### 5. Create the House Background

1. Create a new 2D Sprite: `GameObject` → `2D Object` → `Sprite` → `Square`
2. Rename to "Floor"
3. Scale it to create a room (e.g., scale X: 15, Y: 10)
4. Change color to a floor color (e.g., tan #D2B48C)
5. Set Z position to 1 (behind player)

### 6. Create Walls

1. Create 4 sprites for walls (Top, Bottom, Left, Right)
2. Add `Box Collider 2D` to each wall
3. Position them around the floor to create boundaries

### 7. Create Interactable Objects

For each object (create 4-6):

1. Create a new 2D Sprite: `GameObject` → `2D Object` → `Sprite` → `Square`
2. Rename appropriately (e.g., "Couch", "PhotoFrame", "RamenBowl", "Tickets", "Plant", "GameConsole")
3. Add components:
   - `Box Collider 2D` (check "Is Trigger")
   - `InteractableObject` script
4. Configure in Inspector:
   - **Memory Title**: e.g., "First Date Ramen Bowl"
   - **Memory Story**: Write a personal memory
   - **Interaction Range**: 2.0
   - **Glow Color**: Choose a color
5. Adjust sprite color and size to represent the object

### 8. Set Up the UI Canvas

1. Create UI Canvas: `GameObject` → `UI` → `Canvas`
2. Set Canvas Scaler to "Scale With Screen Size"
3. Reference Resolution: 1920x1080

#### Memory Panel:

1. Create a Panel as child of Canvas (rename to "MemoryPanel")
2. Set anchors to center, size ~600x400
3. Add background color with some transparency
4. Add child UI elements:
   - **Title Text (TextMeshPro)**: Top of panel, large font
   - **Story Text (TextMeshPro)**: Middle, smaller font, multi-line
   - **Image**: Optional image display
   - **Close Button**: Top-right corner

#### Interaction Hint:

1. Create another Panel (rename to "InteractionHint")
2. Position at bottom of screen
3. Add TextMeshPro for hint text
4. Initially set to inactive

### 9. Connect UI to InteractionUI Script

1. Create an empty GameObject named "UIManager"
2. Add the `InteractionUI` script
3. Drag all UI references:
   - Memory Panel
   - Title Text
   - Story Text
   - Memory Image
   - Close Button
   - Interaction Hint
   - Hint Text

### 10. Create a GameManager

1. Create empty GameObject named "GameManager"
2. Add the `GameManager` script

## Default Memory Stories

The project includes 6 default interactable objects with memories:

1. **Couch** - "Our Cozy Movie Nights"
2. **Photo Frame** - "Our First Adventure"
3. **Ramen Bowl** - "First Date Ramen Bowl"
4. **Concert Tickets** - "The Concert We'll Never Forget"
5. **Plant** - "Our Little Green Friend"
6. **Game Console** - "Our Gaming Adventures"

## Controls

- **WASD** or **Arrow Keys**: Move the character
- **E**: Interact with nearby objects
- **ESC**: Close memory panel

## Customization

### Adding New Memories

1. Create a new sprite for the object
2. Add `InteractableObject` script
3. Fill in the memory details in the Inspector
4. Add appropriate collider

### Changing Player Appearance

1. Replace the Player sprite with a custom character sprite
2. Adjust the sprite renderer color
3. You can use sprite sheets for animations

### Adding Animations

1. Create an Animator Controller
2. Add animation clips for walking, idle, etc.
3. Attach to the Player GameObject

## Technical Details

### Scripts Overview

- **PlayerController.cs**: Handles player input and movement using Rigidbody2D
- **InteractableObject.cs**: Manages object interaction, glow effects, and triggering UI
- **InteractionUI.cs**: Controls the memory panel display and interaction hints
- **CameraController.cs**: Smooth camera following with optional bounds
- **GameManager.cs**: Singleton pattern for game state management
- **MemoryData.cs**: ScriptableObject for organizing memory data

### Physics Settings

- Player uses Rigidbody2D with:
  - Gravity Scale: 0 (for top-down movement)
  - Freeze Rotation: Enabled
  - Collision Detection: Continuous
- Walls use Box Collider 2D (non-trigger)
- Interactable objects use Box Collider 2D (trigger)

## Building the Game

1. Go to `File` → `Build Settings`
2. Add the HouseScene to "Scenes in Build"
3. Select your target platform (PC, Mac, WebGL, etc.)
4. Click "Build" and choose output location

## Troubleshooting

### Player doesn't move
- Check that PlayerController script is attached
- Verify Rigidbody2D gravity is set to 0
- Check Input Manager settings (Edit → Project Settings → Input Manager)

### Interactions don't work
- Ensure objects have InteractableObject script
- Verify Box Collider 2D is set to "Is Trigger"
- Check that Player has the "Player" tag
- Verify InteractionUI script references are set

### UI doesn't show
- Check Canvas Scaler settings
- Verify UI Manager script references
- Ensure EventSystem exists in scene

## Future Enhancements

- Add character animations
- Include background music and sound effects
- Add more rooms to explore
- Create save system for tracking discovered memories
- Add dialogue system
- Implement day/night cycle
- Add more interactive elements

## Credits

Created as a heartfelt interactive experience to celebrate relationship memories.

## License

Personal project - All rights reserved
