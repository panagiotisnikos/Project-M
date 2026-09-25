using UnityEngine;

/// <summary>
/// The one UI colour palette, drawn from the art references in Assets/_Refs:
/// moss and ivy over pale bone (the overgrown skeleton, the veiled statue), sepia fog,
/// the teal glow of the firefly meadow, and the violet/rose of the dusk wildflowers.
/// Deliberately no orange/amber anywhere.
///
/// Every script that builds UI in code reads from here, so a palette change is one edit.
/// The *Hex strings are the same colours for TMP rich text (&lt;color=...&gt;).
/// </summary>
public static class UIPalette
{
    /// <summary>Titles and headings - pale lichen green.</summary>
    public static readonly Color Lichen = new Color(0.74f, 0.81f, 0.62f);

    /// <summary>Accent: prompts, highlights, interactive values - spectral teal.</summary>
    public static readonly Color Teal = new Color(0.50f, 0.80f, 0.75f);

    /// <summary>Magic / attunement / Vestige - twilight violet.</summary>
    public static readonly Color Violet = new Color(0.68f, 0.60f, 0.88f);

    /// <summary>Positive values, gains - moss green.</summary>
    public static readonly Color Moss = new Color(0.60f, 0.76f, 0.49f);

    /// <summary>Negative values, warnings - dusty wildflower rose.</summary>
    public static readonly Color Rose = new Color(0.82f, 0.48f, 0.53f);

    /// <summary>Firefly glow - the Hearth Ember and other "warm but not orange" moments.</summary>
    public static readonly Color Firefly = new Color(0.86f, 0.89f, 0.56f);

    /// <summary>Bar fills (stamina, sliders, carry weight) - sage teal.</summary>
    public static readonly Color Sage = new Color(0.44f, 0.64f, 0.56f);

    /// <summary>Thin rules, hotbar strips, quiet dividers - dim moss.</summary>
    public static readonly Color MossDim = new Color(0.33f, 0.43f, 0.32f);

    /// <summary>Body text - bone / parchment.</summary>
    public static readonly Color Parchment = new Color(0.87f, 0.83f, 0.74f);

    /// <summary>Secondary text - fog.</summary>
    public static readonly Color ParchmentDim = new Color(0.60f, 0.57f, 0.50f);

    /// <summary>Health and the boss bar - dried blood.</summary>
    public static readonly Color Blood = new Color(0.60f, 0.13f, 0.12f);

    public const string TealHex = "#80CCBF";
    public const string MossHex = "#99C27D";
    public const string RoseHex = "#D17A87";
    public const string LichenHex = "#BDCF9E";
    public const string ParchmentHex = "#DED4BD";
    public const string DimHex = "#998F80";
}
