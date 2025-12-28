# Parenting System Documentation

## Overview

The Parenting System manages hierarchical relationships between entities (e.g., a turret attached to a tank, or a particle effect attached to a projectile).

It ensures that when a parent moves, its children move with it (via the `LocalTransform` component). It also handles the cleanup: if a parent is destroyed, its children are preserved but detached (or disposed if logic dictates, though the core system defaults to detachment).

## Architecture (For Engine Developers)

### 1. HierarchyManager

The central system for managing relationships.
*   **Bucket**: `ExecutionBucket.Cleanup`
*   **Responsibilities**:
    *   **Cleanup**: Checks every frame for `IsChildren` components where the `Parent` entity is dead. Removes the component. (Same for dead entities in `IsParent.Childrens` list).
    *   **API**: Provides `TryAttach` and `TryDetach` methods to safely modify the hierarchy and ensure bidirectional links (`Parent <-> Child`).
    *   **Events**: Publishes `HierarchyChangeEvent` when the structure changes.

### 2. Components

*   `IsParent`: Added to the "Parent" entity. Contains a `List<Entity> Childrens`.
*   `IsChildren`: Added to the "Child" entity. Contains an `Entity Parent` reference.
*   `LocalTransform`: Stores the child's position *relative* to the parent.
    *   *Note*: The `HierarchyManager` does **not** perform the position calculation. That is handled by a separate Transform system (often integrated into `PositionSystem` or `PhysicsSystem` logic where `Position = Parent.Position + LocalTransform`).
    * Or, this might be done by the physics engine. Dunno.

### 3. Events

*   `HierarchyChangeEvent`: Fired whenever an attach or detach occurs.
    *   `Type`: `Attached` or `Detached`.
    *   `Actor`: The child entity.
    *   `Parent`: The parent entity.

## Usage Guide (For Engine Users)

### 1. Attaching Entities

To attach one entity to another:

```csharp
var hierarchy = SystemManager.GetSystem<HierarchyManager>();

// Attach child to parent with a specific offset
hierarchy.TryAttach(childEntity, parentEntity, new LocalTransform 
{ 
    Position = new Vector2(10, 0) // 10 pixels to the right
});
```
*   **Safety**: Returns `false` if `child` already has a parent or if entities are dead.
*   **Unsafe Option**: `AttachUnsafe` throws exceptions instead of returning bool. Use with caution.

### 2. Detaching

```csharp
hierarchy.TryDetach(childEntity, parentEntity);
```
The child retains its current world position but will no longer follow the parent.

### 3. Switching Parents

To move a child from one parent to another (e.g., a player picking up an item from a pedestal):

```csharp
hierarchy.TrySafeSwitchParent(itemEntity, oldParent, newParent, newOffset);
```

### 4. Reacting to Hierarchy Changes

If your system needs to know when relationships change (e.g., to update UI or AI):

```csharp
public override void OnSceneLoad()
{
    Events.Subscribe<HierarchyChangeEvent>(OnHierarchyChange);
}

private void OnHierarchyChange(HierarchyChangeEvent ev)
{
    Console.WriteLine($"{ev.Actor} was {ev.Type} to/from {ev.Parent}");
}
```

### Important Notes

*   **Circular Dependencies**: The system prevents an entity from being its own parent, but does *not* currently perform deep cycle detection (A->B->C->A). Be careful.
*   **Physics**: If both Parent and Child have Physics Bodies, the `HierarchyManager` does **not** automatically weld them. You must use `WeldAttachCommand` or manually create a Joint if you want physics interaction between them. The Parenting system is primarily for logical/visual attachment.
