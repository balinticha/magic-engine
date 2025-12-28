# Prototype & Component System Documentation

## Overview

The Prototype System allows for data-driven entity creation. Instead of hardcoding entity composition in C#, you can define "Prototypes" (prefabs) in YAML files. These prototypes define what components an entity has and what their initial values are.

The system supports **Inheritance**, meaning you can create a base `Enemy` prototype and have `Goblin` and `Orc` prototypes inherit from it, overriding specific values.

## Architecture (For Engine Developers)

The system consists of three main parts:

### 1. Attributes

*   `[Component]`: Marks a `struct` or `class` as a valid component that can be attached to an entity.
*   `[DataField]`: Marks specific fields or properties within a component as serializable. Only these fields can be set via YAML.
    *   **Arguments**: `[DataField("yamlKey")]` allows you to map a custom YAML key to the C# member. If omitted, the member name is used (converted to camelCase).

### 2. PrototypeManager

The `PrototypeManager` is responsible for:
*   **Discovery**: It scans the assembly for types with the `[Component]` attribute to build a type cache.
*   **Loading**: It reads all `.yaml` files in the `Prefabs/` directory at startup.
*   **Spawning**: The `SpawnEntity(id, position)` method creates a new entity, adds the `PrototypeIDComponent`, `Position`, and `Velocity`, and then recursively applies components from the prototype definition.
*   **Inheritance**: It resolves the `parent` field in YAML, applying the parent's components first, then overlaying the child's components.

### 3. Serialization

The system uses `YamlDotNet` for parsing. It includes custom value converters for common types:
*   `Vector2`: `{ x: 10, y: 20 }`
*   `Color`: `"Red"`, `"CornflowerBlue"`, etc. (Uses static properties of `Microsoft.Xna.Framework.Color`)
*   `Texture2D`: `"Path/To/Texture"` (Loads via `ContentManager`)

## Usage Guide (For Engine Users)

### 1. Defining a Component

To make a component usable in prototypes, add the `[Component]` attribute and mark its fields with `[DataField]`.

```csharp
[Component]
public struct Health
{
    [DataField] 
    public int Max;
    
    [DataField] 
    public int Current;
    
    // This field will NOT be touchable from YAML
    public bool IsInvincible; 
}
```

### 2. Creating a Prototype (YAML)

Create a `.yaml` file anywhere in the `Prefabs/` folder. You can define multiple prototypes in one file using a list format.

```yaml
- id: BaseEnemy
  name: Generic Enemy
  components:
    - Sprite:
        texture: "Sprites/Enemies/base"
        color: "White"
    - Health:
        max: 100
        current: 100
    - Body: # Example physics component
        mass: 1.0
        isStatic: false

- id: Goblin
  parent: BaseEnemy # Inherits Sprite, Health, Body from BaseEnemy
  name: Nasty Goblin
  components:
    - Sprite:
        texture: "Sprites/Enemies/goblin" # Overrides texture
    - Health:
        max: 50 # Overrides Max Health
        current: 50
    - AI: # Adds a new component
        aggroRange: 200.0
```

### 3. Spawning Entities

To spawn an entity in the game world, use the `Spawn` command in the console or access the `PrototypeManager` via code.

**Console:**
`spawn Goblin 100 200`

**C# Code:**
```csharp
// Inside an EntitySystem or Command
Prototypes.SpawnEntity("Goblin", new Vector2(100, 200));
```

### Troubleshooting

*   **"Component not found"**: Ensure your C# struct has the `[Component]` attribute and the name matches exactly.
*   **"Field not set"**: Ensure the field has `[DataField]` and is `public`. Check case sensitivity (though the system tries to handle camelCase).
*   **Circular Dependency**: You cannot have `A` inherit from `B` and `B` inherit from `A`. The console will print an error if this is detected.
