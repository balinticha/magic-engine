using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using DefaultEcs;
using ImGuiNET;
using MagicThing.Engine.Base.EntityWrappers;
using MagicThing.Engine.Base.PrototypeComponentSystem;
using MagicThing.Engine.ECS.Core.Parenting.Components;

namespace MagicThing.Engine.Base.Debug.UI;

public class SceneGraphPanel
{
    private readonly World _world;
    private readonly EntitySet _rootEntities;
    private readonly string _currentSceneName;
    
    public Entity? SelectedEntity { get; private set; }
    public Entity? EntityToInspect { get; private set; }

    public SceneGraphPanel(World world, string currentSceneName)
    {
        _world = world;
        _currentSceneName = currentSceneName;
        _rootEntities = _world.GetEntities().Without<IsChildren>().AsSet();
    }

    /// <summary>
    /// Draws the Scene Graph window.
    /// </summary>
    /// <param name="isOpen">A reference to a boolean that controls the window's visibility. The window's close button will set this to false.</param>
    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        
        EntityToInspect = null;

        ImGui.SetNextWindowSize(new Vector2(300, 400), ImGuiCond.FirstUseEver);
        if (ImGui.Begin($"Scene Graph - {_currentSceneName}", ref isOpen))
        {
            // Iterate over all entities that are not children and draw them.
            foreach (var entity in _rootEntities.GetEntities())
            {
                DrawEntityNode(entity);
            }
            
            if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsAnyItemHovered())
            {
                SelectedEntity = null;
            }
        }
        ImGui.End();
    }

    /// <summary>
    /// Recursively draws an entity and its children as a tree node in ImGui.
    /// </summary>
    private void DrawEntityNode(Entity entity)
    {
        if (!entity.IsAlive) return;

        // Build the label for the tree node
        var label = BuildEntityLabel(entity);

        IEnumerable<Entity> children = null;
        
        if (entity.Has<IsParent>())
        {
            children = entity.Get<IsParent>().Childrens;
        }

        var hasChildren = children != null && children.Any();
        
        var nodeFlags = hasChildren 
            ? ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick 
            : ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        
        if (SelectedEntity.HasValue && SelectedEntity.Value == entity)
        {
            nodeFlags |= ImGuiTreeNodeFlags.Selected;
        }

        bool isNodeOpen = ImGui.TreeNodeEx(label, nodeFlags);
        
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            SelectedEntity = entity;
        }
        
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            EntityToInspect = entity;
        }

        // If the node is open and it's not a leaf, process its children
        if (isNodeOpen && hasChildren)
        {
            var childrenCopy = children.ToArray(); 
            
            foreach (var child in childrenCopy)
            {
                DrawEntityNode(child); // Recursive call
            }
            ImGui.TreePop();
        }
    }

    /// <summary>
    /// Helper function to create a descriptive string label for an entity.
    /// </summary>
    private string BuildEntityLabel(Entity entity)
    {
        // Using StringBuilder for slightly better performance than string concatenation
        var sb = new StringBuilder();
        
        // DefaultEcs.Entity doesn't have a stable integer ID, but its hash code is unique for its lifetime.
        sb.Append($"{($"{entity}").Substring(7)}E |");
        
        if (entity.TryGet<NameComponent>(out var name))
        {
            sb.Append($" {name.Comp.Value}");
        }
        
        if (entity.TryGet<PrototypeIDComponent>(out var protoId))
        {
            sb.Append($" [{protoId.Comp.Value}]");
        }
        
        return sb.ToString();
    }
}
