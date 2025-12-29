using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using DefaultEcs;
using ImGuiNET;
using MagicEngine.Engine.Base.PrototypeComponentSystem;
using MagicEngine.Engine.ECS.Core.Parenting.Components;
using MagicEngine.Engine.Base.EntityWrappers;

namespace MagicEngine.Engine.Base.Debug.UI;

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
        
        ImGui.PushID(entity.GetHashCode());
        
        string nodeText = BuildEntityLabel(entity); 

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
        
        bool isNodeOpen = ImGui.TreeNodeEx(nodeText, nodeFlags);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            SelectedEntity = entity;
        }
    
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            EntityToInspect = entity;
        }

        if (isNodeOpen && hasChildren)
        {
            foreach (var child in children)
            {
                DrawEntityNode(child); 
            }
            ImGui.TreePop();
        }
        
        ImGui.PopID();
    }

    // 1. Static cache to reuse memory frame-over-frame
    private static readonly StringBuilder _labelCache = new StringBuilder(256);

    private string BuildEntityLabel(Entity entity)
    {
        _labelCache.Clear();

        string fullString = entity.ToString();
        _labelCache.Append(fullString.AsSpan(7));

        // ---------------------------------------------------------
        // The rest of your logic remains the same, but now it uses
        // the cached builder, saving 2 more allocations.
        // ---------------------------------------------------------
        
        if (entity.TryGet<NameComponent>(out var name))
        {
            _labelCache.Append(' ');
            _labelCache.Append(name.Comp.Value);
        }
        
        if (entity.TryGet<PrototypeIDComponent>(out var protoId))
        {
            _labelCache.Append(" [");
            _labelCache.Append(protoId.Comp.Value);
            _labelCache.Append(']');
        }
        
        return _labelCache.ToString();
    }
}
