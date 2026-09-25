using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// "The Valley's Reading" - an optional post-game page behind the completion
/// screen that lifts the curtain on the adaptive system.
///
/// During play the adaptation is deliberately never shown as numbers (the world
/// answers through fog, light and ground only). Once the run is over, this page
/// explains what the valley actually measured, how each action was weighted,
/// where the player landed between the three endings, what their camp/shrine
/// choices changed, and how the other endings are reached.
///
/// Lives on CompletionPanel (always active while the completion screen is up).
/// Show()/Hide() are wired to the "The Valley's Reading" and "Back" buttons.
/// Reads everything live - no data of its own, nothing to keep in sync.
/// </summary>
public class ValleyReadingUI : MonoBehaviour
{
    [Header("Windows")]
    [SerializeField] private GameObject completionWindow;
    [SerializeField] private GameObject readingWindow;

    [Header("How you fought")]
    [SerializeField] private TMP_Text breakdownText;
    [SerializeField] private TMP_Text signalText;

    [Header("Signal meter (zones + marker are positioned by anchors at runtime)")]
    [SerializeField] private RectTransform stableZone;
    [SerializeField] private RectTransform balancedZone;
    [SerializeField] private RectTransform decayingZone;
    [SerializeField] private RectTransform marker;
    [Tooltip("How far past each threshold the meter extends, in signal points.")]
    [SerializeField] private float meterMargin = 25f;

    [Header("Choices + paths")]
    [SerializeField] private TMP_Text choicesText;
    [SerializeField] private TMP_Text pathsText;
    [SerializeField] private TMP_Text localReadingText;

    private const string Accent = UIPalette.TealHex;
    private const string Gain = UIPalette.MossHex;
    private const string Loss = UIPalette.RoseHex;
    private const string Parchment = UIPalette.ParchmentHex;
    private const string Dim = UIPalette.DimHex;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private PlayerPerformanceTracker tracker;
    private WorldAdaptationManager world;

    private void Awake()
    {
        if (readingWindow != null)
            readingWindow.SetActive(false);
    }

    public void Show()
    {
        Populate();

        if (completionWindow != null) completionWindow.SetActive(false);
        if (readingWindow != null) readingWindow.SetActive(true);
    }

    public void Hide()
    {
        if (readingWindow != null) readingWindow.SetActive(false);
        if (completionWindow != null) completionWindow.SetActive(true);
    }

    private void Populate()
    {
        if (tracker == null) tracker = FindFirstObjectByType<PlayerPerformanceTracker>();
        if (world == null) world = FindFirstObjectByType<WorldAdaptationManager>();

        if (tracker != null && world != null)
        {
            PopulateBreakdown();
            PopulateMeter();
            PopulatePaths();
        }

        PopulateChoices();
        PopulateLocalReading();
    }

    // ------------------------------------------------------------------
    // How you fought

    private void PopulateBreakdown()
    {
        var sb = new StringBuilder();
        Row(sb, "Parries landed", tracker.ParriesLanded, tracker.ParryWeight);
        Row(sb, "Clean dodges", tracker.CleanDodges, tracker.DodgeWeight);
        Row(sb, "Blocks held", tracker.BlocksHeld, tracker.BlockWeight);
        Row(sb, "Hits taken", tracker.HitsTaken, -tracker.HitPenalty);
        Row(sb, "Wounds (damage)", tracker.DamageTaken, -tracker.DamagePenalty);
        sb.Append($"<color={Dim}><i>Kills and time were never counted - the valley reads how you fight, not how much.</i></color>");

        if (breakdownText != null)
            breakdownText.text = sb.ToString();

        if (signalText != null)
        {
            float score = tracker.GetPerformanceScore();
            signalText.text =
                $"Mastery signal <color={Accent}><b>{Signed(score)}</b></color>" +
                $"   <color={Dim}>- ending: {GameUIController.GetWorldResponseTitle(world.CurrentState)}</color>";
        }
    }

    private static void Row(StringBuilder sb, string label, int count, float weight)
    {
        float value = count * weight;
        string colour = value > 0f ? Gain : value < 0f ? Loss : Dim;

        sb.Append(label)
          .Append("<pos=46%>").Append(count.ToString(Inv))
          .Append($"<pos=60%><color={Dim}>x {Signed(weight, "0.0#")}</color>")
          .Append($"<pos=82%><color={colour}>{Signed(value)}</color>")
          .Append('\n');
    }

    // ------------------------------------------------------------------
    // Meter: [ LIES STILL | HOLDS | HAS TURNED ] with a marker for the player

    private void PopulateMeter()
    {
        float stable = world.StableThreshold;
        float decaying = world.DecayingThreshold;
        float min = stable - meterMargin;
        float max = decaying + meterMargin;

        float stableT = Mathf.InverseLerp(min, max, stable);
        float decayT = Mathf.InverseLerp(min, max, decaying);

        SetSpan(stableZone, 0f, stableT);
        SetSpan(balancedZone, stableT, decayT);
        SetSpan(decayingZone, decayT, 1f);

        if (marker != null)
        {
            // Clamped so an off-scale run still shows the marker at the edge.
            float t = Mathf.Clamp01(Mathf.InverseLerp(min, max, tracker.GetPerformanceScore()));
            marker.anchorMin = new Vector2(t, marker.anchorMin.y);
            marker.anchorMax = new Vector2(t, marker.anchorMax.y);
            marker.anchoredPosition = new Vector2(0f, marker.anchoredPosition.y);
        }
    }

    private static void SetSpan(RectTransform zone, float from, float to)
    {
        if (zone == null) return;
        zone.anchorMin = new Vector2(from, 0f);
        zone.anchorMax = new Vector2(to, 1f);
        zone.offsetMin = Vector2.zero;
        zone.offsetMax = Vector2.zero;
    }

    // ------------------------------------------------------------------
    // Paths not taken

    private void PopulatePaths()
    {
        if (pathsText == null) return;

        string stable = Signed(world.StableThreshold);
        string decaying = Signed(world.DecayingThreshold);

        var sb = new StringBuilder();
        PathRow(sb, WorldAdaptationManager.WorldState.Stable,
            $"signal {stable} or lower - struck often, worn down. The valley stays dormant and forgiving.");
        PathRow(sb, WorldAdaptationManager.WorldState.Balanced,
            $"between {stable} and {decaying} - clean defence mixed with wounds. The valley withholds judgement.");
        PathRow(sb, WorldAdaptationManager.WorldState.Decaying,
            $"signal {decaying} or higher - parry, dodge cleanly, stay unhurt. The land wakes to a skilled intruder and festers.");

        pathsText.text = sb.ToString().TrimEnd('\n');
    }

    private void PathRow(StringBuilder sb, WorldAdaptationManager.WorldState state, string condition)
    {
        bool reached = world.CurrentState == state;
        string title = GameUIController.GetWorldResponseTitle(state);

        if (reached)
            sb.Append($"<color={Accent}><b>{title}</b>  (your ending)</color>\n");
        else
            sb.Append($"<color={Parchment}><b>{title}</b></color>\n");

        sb.Append($"<color={Dim}>{condition}</color>\n");
    }

    // ------------------------------------------------------------------
    // What your choices changed

    private void PopulateChoices()
    {
        if (choicesText == null) return;

        var sb = new StringBuilder();

        var camps = new List<Camp>(FindObjectsByType<Camp>(FindObjectsSortMode.None));
        // Boss-weakening camps first - they are the meaningful decision.
        camps.Sort((a, b) => b.WeakensBoss.CompareTo(a.WeakensBoss));

        foreach (var camp in camps)
        {
            string name = string.IsNullOrEmpty(camp.CampName) ? camp.name : camp.CampName;

            if (camp.WeakensBoss)
            {
                string ability = AbilityName(camp.BossEffect);
                sb.Append(camp.IsCleared
                    ? $"<b>{name}</b> - cleared. <color={Accent}>The boss lost its {ability}.</color>\n"
                    : $"<b>{name}</b> - left standing. <color={Dim}>The boss kept its {ability}.</color>\n");
            }
            else
            {
                sb.Append(camp.IsCleared
                    ? $"<b>{name}</b> - cleared. <color={Dim}>Spoils for the shrine.</color>\n"
                    : $"<b>{name}</b> - left standing.\n");
            }
        }

        var altar = FindFirstObjectByType<ProgressionAltar>();
        if (altar != null && altar.Unlocks != null)
        {
            var taken = new List<string>();
            foreach (var unlock in altar.Unlocks)
                if (unlock != null && ProgressionSystem.HasUnlock(unlock))
                    taken.Add(unlock.displayName);

            sb.Append(taken.Count > 0
                ? $"<b>Attunement Shrine</b> - {string.Join(", ", taken)}.\n"
                : $"<b>Attunement Shrine</b> - <color={Dim}>no attunements taken.</color>\n");
        }

        choicesText.text = sb.ToString().TrimEnd('\n');
    }

    private static string AbilityName(Camp.BossWeakeningReward effect)
    {
        switch (effect)
        {
            case Camp.BossWeakeningReward.DisableHealing: return "healing";
            case Camp.BossWeakeningReward.DisableSummons: return "summons";
            case Camp.BossWeakeningReward.DisableDecayAura: return "decay aura";
            default: return "power";
        }
    }

    // ------------------------------------------------------------------
    // Local (per-region) readings - the second, independent adaptive layer

    private void PopulateLocalReading()
    {
        if (localReadingText == null) return;

        var sb = new StringBuilder();
        foreach (var region in FindObjectsByType<WorldRegion>(FindObjectsSortMode.None))
        {
            // Only regions the player actually swayed - an untouched region has nothing to say.
            if (Mathf.Approximately(region.CommittedScore, 0f) && region.CurrentState == RegionWorldState.Balanced)
                continue;

            var camp = region.GetComponent<Camp>();
            string name = camp != null && !string.IsNullOrEmpty(camp.CampName) ? camp.CampName : region.RegionId;
            sb.Append($"<b>{name}</b> - <color={Accent}>{RegionStateName(region.CurrentState)}</color>\n");
        }

        localReadingText.text = sb.Length > 0
            ? $"<color={Dim}>Some places also judge you on their own, by what you did inside them:</color>\n" + sb.ToString().TrimEnd('\n')
            : $"<color={Dim}>Some places also judge you on their own, by what you did inside them. You swayed none this time.</color>";
    }

    private static string RegionStateName(RegionWorldState state)
    {
        switch (state)
        {
            case RegionWorldState.Blossom: return "it blossomed";
            case RegionWorldState.Decayed: return "it decayed";
            default: return "it held its balance";
        }
    }

    // ------------------------------------------------------------------

    private static string Signed(float value, string format = "0.0")
    {
        return value.ToString("+" + format + ";-" + format + ";" + format, Inv);
    }
}
