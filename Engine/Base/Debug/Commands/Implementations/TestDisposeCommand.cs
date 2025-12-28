using System;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using Microsoft.Xna.Framework;

namespace MagicEngine.Engine.Base.Debug.Commands.Implementations;

public class TestDisposeCommand : ConsoleCommand
{
    public override string Name => "testdispose";
    public override string Description => "";

    public override string Execute(string[] args)
    {
        var output = "";
        
        var ent = World.CreateEntity();
        output += $"Entity: {ent}";

        ent.Set<Position>();
        output += $" Added position. Does it have position? {ent.Has<Position>()}";
        
        ent.Dispose();
        
        var ent2 = World.CreateEntity();
        output += $" New entity: {ent2} - does it have position? {ent2.Has<Position>()}";

        return output;
    }
}