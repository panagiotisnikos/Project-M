using UnityEngine;

/// <summary>
/// A crafting station's "kind" (Forge, Workbench, ...), as data rather than a
/// hardcoded enum - adding a new station type is "create an asset," not a
/// code change. Deliberately minimal: just an identity, matching how
/// CraftingRecipe compares stations by reference equality.
/// </summary>
[CreateAssetMenu(fileName = "StationType_", menuName = "Project M/Crafting/Station Type")]
public class CraftingStationType : ScriptableObject
{
    public string displayName = "Station";
}
