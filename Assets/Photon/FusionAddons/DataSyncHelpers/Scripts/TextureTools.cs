using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.DataSyncHelpers
{
    public interface IRenderTextureProvider
    {
        public RenderTexture RenderTexture { get; }
        public void OnRenderTextureEdit();
    }
}
