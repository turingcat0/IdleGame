using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared utility for all UI builder scripts.
/// Finds or creates the scene-level "UIRoot" empty GameObject
/// so that all generated UI Canvases are grouped under one parent.
/// </summary>
public static class UIRootHelper
{
    public const string UIRootName = "UIRoot";

    /// <summary>
    /// Find an existing UIRoot in the scene, or create one if it doesn't exist yet.
    /// Returns the Transform to use as parent for newly generated UI roots.
    /// </summary>
    public static Transform GetOrCreateUIRoot()
    {
        var existing = GameObject.Find(UIRootName);
        if (existing != null)
            return existing.transform;

        var go = new GameObject(UIRootName);
        Undo.RegisterCreatedObjectUndo(go, "Create " + UIRootName);

        var uiLayer = LayerMask.NameToLayer("UI");
        go.layer = uiLayer >= 0 ? uiLayer : 5;

        return go.transform;
    }
}
