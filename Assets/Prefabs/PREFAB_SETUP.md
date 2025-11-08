# Memory Object Prefab Configuration Guide

This document describes how to set up each of the 6 interactable objects in Unity.

## Object 1: Couch

**GameObject Name**: `Couch`

**Transform**:
- Position: (-3, 2, 0)
- Scale: (2, 1.5, 1)

**Sprite Renderer**:
- Color: Brown (#8B4513)

**Box Collider 2D**:
- Is Trigger: ✓
- Size: Match sprite size

**Interactable Object Component**:
- Memory Title: `Our Cozy Movie Nights`
- Memory Story: `This couch has seen countless movie marathons, where we laughed at comedies, jumped at horror movies, and cried during romantic dramas. Remember that time we watched all three Lord of the Rings extended editions in one weekend? We survived on pizza and popcorn!`
- Interaction Range: 2.0
- Glow Color: Gold (#FFD700)

---

## Object 2: Photo Frame

**GameObject Name**: `PhotoFrame`

**Transform**:
- Position: (3, 3, 0)
- Scale: (0.8, 1.2, 1)

**Sprite Renderer**:
- Color: Dark Wood (#654321)

**Box Collider 2D**:
- Is Trigger: ✓
- Size: Match sprite size

**Interactable Object Component**:
- Memory Title: `Our First Adventure`
- Memory Story: `This photo captures our first hiking trip together. You were so excited about reaching the summit, even though we got a bit lost on the way. The view at the top was breathtaking, but not as beautiful as your smile when we finally made it!`
- Interaction Range: 2.0
- Glow Color: Pink (#FF6B9D)

---

## Object 3: Ramen Bowl

**GameObject Name**: `RamenBowl`

**Transform**:
- Position: (0, -2, 0)
- Scale: (1.2, 0.8, 1)

**Sprite Renderer**:
- Color: Tomato Red (#FF6347)

**Box Collider 2D**:
- Is Trigger: ✓
- Size: Match sprite size

**Interactable Object Component**:
- Memory Title: `First Date Ramen Bowl`
- Memory Story: `Our first date! We went to that tiny ramen shop downtown. You ordered the spiciest ramen on the menu and tried so hard not to show it was too hot. I thought it was adorable how you kept sipping water while insisting it was "just right". Best first date ever!`
- Interaction Range: 2.0
- Glow Color: Yellow (#FFFF00)

---

## Object 4: Concert Tickets

**GameObject Name**: `ConcertTickets`

**Transform**:
- Position: (4, -2, 0)
- Scale: (0.7, 1, 1)

**Sprite Renderer**:
- Color: Royal Blue (#4169E1)

**Box Collider 2D**:
- Is Trigger: ✓
- Size: Match sprite size

**Interactable Object Component**:
- Memory Title: `The Concert We'll Never Forget`
- Memory Story: `These are the tickets from that amazing concert where your favorite band played. We sang along to every song, danced like nobody was watching, and you even caught a guitar pick! You were so happy that night, your joy was absolutely infectious.`
- Interaction Range: 2.0
- Glow Color: Cyan (#00FFFF)

---

## Object 5: Plant

**GameObject Name**: `Plant`

**Transform**:
- Position: (-3, -2, 0)
- Scale: (1, 1.2, 1)

**Sprite Renderer**:
- Color: Sea Green (#2E8B57)

**Box Collider 2D**:
- Is Trigger: ✓
- Size: Match sprite size

**Interactable Object Component**:
- Memory Title: `Our Little Green Friend`
- Memory Story: `This plant was a gift from our first apartment together. You named it "Planty" and talk to it every morning. It's thrived under your care, just like our relationship. Remember when you were convinced it was dying and we spent a whole evening researching plant care?`
- Interaction Range: 2.0
- Glow Color: Light Green (#90EE90)

---

## Object 6: Game Console

**GameObject Name**: `GameConsole`

**Transform**:
- Position: (0, 1, 0)
- Scale: (1, 1, 1)

**Sprite Renderer**:
- Color: Goldenrod (#DAA520)

**Box Collider 2D**:
- Is Trigger: ✓
- Size: Match sprite size

**Interactable Object Component**:
- Memory Title: `Our Gaming Adventures`
- Memory Story: `So many epic gaming sessions! From co-op adventures to friendly competitions where you absolutely destroyed me at Mario Kart. You always say you let me win sometimes, but we both know you're just better at it. Gaming with you is always fun, win or lose!`
- Interaction Range: 2.0
- Glow Color: Orange (#FFA500)

---

## Quick Setup Tips

1. **Creating Objects**: Use `GameObject → 2D Object → Sprite → Square` for each object
2. **Positioning**: Arrange objects around the room leaving space for the player to walk
3. **Colliders**: Always set "Is Trigger" to true for interactable objects
4. **Colors**: Use the color codes provided or customize to your preference
5. **Testing**: Play mode and walk to each object to test the glow and interaction

## Customizing Memories

To personalize the memories:
1. Select the object in the Hierarchy
2. Find the "Interactable Object" component in the Inspector
3. Edit the "Memory Title" and "Memory Story" fields
4. Optionally add a Sprite to "Memory Image" field

## Using Sprites Instead of Colored Squares

To use custom sprites:
1. Import your sprite images to `Assets/Sprites/`
2. Select the object's Sprite Renderer
3. Drag your custom sprite to the "Sprite" field
4. Adjust the size as needed
