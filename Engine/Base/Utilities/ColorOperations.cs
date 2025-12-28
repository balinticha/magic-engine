using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MagicThing.Engine.Base.Utilities;

public static class ColorOperations
{
    public static List<Color> CreateColorGradient(Color startColor, Color endColor, int numberOfSteps)
    {
        var colorGradient = new List<Color>();
        
        for (int i = 0; i < numberOfSteps; i++)
        {
            float amount = (float)i / (numberOfSteps - 1);
            Color interpolatedColor = Color.Lerp(startColor, endColor, amount);
            colorGradient.Add(interpolatedColor);
        }

        return colorGradient;
    }
    
    
    public static Color ToLinear(Color srgbColor)
    {
        Vector3 vec = srgbColor.ToVector3();
        
        vec.X = (float)Math.Pow(vec.X, 2.2f);
        vec.Y = (float)Math.Pow(vec.Y, 2.2f);
        vec.Z = (float)Math.Pow(vec.Z, 2.2f);


        return new Color(vec.X, vec.Y, vec.Z, srgbColor.A / 255f); 
    }
}