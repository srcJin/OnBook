using UnityEngine;

namespace Fusion.XR.Shared
{
    // Interface to set a color
    public interface IColorProvider { 
        public Color CurrentColor { get; set; } 
    }
}
