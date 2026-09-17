/// <summary>
/// The long-term world identity, correctly named per the actual vision
/// (Balanced / Blossom / Decayed) - deliberately a separate enum from the
/// legacy WorldAdaptationManager.WorldState (Stable/Balanced/Decaying), which
/// predates this naming and is left untouched so the existing global
/// adaptation system keeps working unmodified during the migration to
/// region-based adaptation.
/// </summary>
public enum RegionWorldState
{
    Balanced,
    Blossom,
    Decayed
}
