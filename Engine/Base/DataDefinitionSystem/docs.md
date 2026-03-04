# Data Definition System Documentation

## Overview

The **Data Definition System** allows developers to declare standalone data objects (assets) in C# that can be instantiated from YAML files without requiring the class to implement specific interfaces or contain serialization-specific `ID` properties. 

This enables highly decoupled data architectures, such as creating `TerrainTileset` definitions from a YAML file, and then safely assigning that exact instance directly into an ECS `TerrainComponent`.

## 1. Creating a Data Definition (C#)

To create a new data definition, define a class and mark it with the `[DataDefinition]` attribute. Unlike entity components, this class exists independently of any entity.

Mark any fields you want to set via YAML with `[DataField]`.

> [!WARNING]
> **Data Definitions are Read-Only Flyweights!**
> The engine only creates **one** single instance of each data definition in memory. This means if 10,000 entities reference the same `OverworldTileset`, they all point to the exact same C# object.
> **DO NOT mutate your data definitions at runtime!** Doing so will instantly alter the data for every other entity referencing it. Treat them as strictly read-only after they are loaded.
> To help enforce this pattern, the `[DataDefinition]` attribute can only be applied to `class` types (never `structs`). We also highly recommend using read-only properties (like `get; init;`) or collections like `IReadOnlyList` for your fields.

```csharp
using MagicEngine.Engine.Base.DataDefinitionSystem;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using System.Collections.Generic;

// Marking it with DataDefinition automatically registers it with the engine.
[DataDefinition]
public class TerrainTileDefinition
{
    [DataField] 
    public string TilesetName;

    [DataField] 
    public List<TileDef> Tiles = new();
}

public class TileDef 
{
    [DataField] public string Id;
    [DataField] public bool IsSolid;
}
```

> [!NOTE]
> You can provide an optional alias to `[DataDefinition("CustomAlias")]` if you want the YAML `type` to differ from the C# class name. Otherwise, it defaults to the exact class name.

## 2. Using it in Components

The real power of this system is that your ECS components can hold strongly-typed references to these definitions without needing to load them manually.

```csharp
[Component]
public struct TerrainComponent 
{
    // The engine's TypeConverters automatically map YAML strings to the DataDefinition instances!
    [DataField] 
    public TerrainTileDefinition Tileset;
}
```

## 3. Creating Definitions in YAML

Create a `.yaml` file anywhere inside your `Data/` or `Definitions/` directory.

To declare a definition, you must specify the **`type`** and the **`id`**.
- `type`: Must match the class name (or `Alias` if provided in the attribute).
- `id`: The unique reference string used by components to request this specific instance.

```yaml
- type: TerrainTileDefinition
  id: OverworldTileset
  tilesetName: "Spring Fields"
  tiles:
    - id: grass
      isSolid: false
    - id: stone
      isSolid: true

- type: TerrainTileDefinition
  id: UndergroundTileset
  tilesetName: "Dark Caverns"
  tiles:
    - id: stone
      isSolid: true
    - id: lava
      isSolid: false 
```

## 4. Referencing Definitions in Entity Prototypes

When building an Entity in your Prefab YAML files, simply provide the `id` of the Definition you created when filling out the component's data.

```yaml
- id: OverworldTerrainEntity
  components:
    - TerrainComponent:
        tileset: OverworldTileset # This automatically injects the instance defined above!
```

## Internal Architecture

The system is powered by the `DataDefinitionManager`.

1. **Discovery:** At startup, `DataDefinitionManager` scans for `[DataDefinition]` and caches the Types.
2. **Loading:** It loops through all `.yaml` files in `Data/` and `Definitions/`. It reads the `id` and `type` fields into a temporary wrapper, and then deserializes the *rest* of the yaml directly into the target type using `YamlDotNet`.
3. **Registry:** It stores all instantiated objects in memory mapped by their Type and ID.
4. **Linking:** `PrototypeManager` fetches all definition types and registers a completely automatic `TypeConverter` for each, instructing the Entity spawner to resolve `string` YAML fields into the mapped `object` instances seamlessly.
