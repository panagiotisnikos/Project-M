using UnityEngine;

/// <summary>
/// Shows the right held meshes for the current loadout. The Axe Warrior model
/// ships with built-in dual axes (Axe_L / Axe_R) - those are hidden and replaced
/// with real weapon models parented to the skeleton.
///
/// Loadout mapping (by WeaponData.WeaponName):
///   contains "axe"  -> heavy axe (R) + tower shield (L arm)
///   otherwise       -> sword (R) + round shield (L arm)
///
/// Swords/axes are parented to the hand bone and swing with the animation.
/// Shields are parented to the forearm bone for position but their rotation is
/// stabilised to face the player's forward direction every LateUpdate - the
/// retargeted Mixamo clips twist the wrist enough that a fixed offset looks wrong
/// in block / attack poses, and a shield should always face the threat anyway.
/// </summary>
public class PlayerWeaponVisuals : MonoBehaviour
{
    [System.Serializable]
    public class HeldItem
    {
        public string label;
        public GameObject prefab;
        [Tooltip("Bone to attach to. Empty = the default hand bone for this side.")]
        public string attachBone;
        public bool leftSide;
        public Vector3 localPosition;
        public Vector3 localEuler;
        public float scale = 18f;

        [Tooltip("Keep this item facing the player's forward instead of following the bone's rotation (shields).")]
        public bool stabiliseRotation;
        [Tooltip("Rotation offset applied on top of 'face forward' (stabilised items only).")]
        public Vector3 stabilisedEuler;

        [System.NonSerialized] public GameObject instance;
        [System.NonSerialized] public Transform attach;
    }

    [Header("References")]
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private Transform facing;
    [SerializeField] private string rightHandBone = "Hand.R";
    [SerializeField] private string leftHandBone = "Hand.L";
    [Tooltip("Built-in model meshes to hide (the warrior's default dual axes).")]
    [SerializeField] private string[] hideMeshNames = { "Axe_L", "Axe_R" };
    [SerializeField] private float stabiliseLerp = 20f;

    [Header("Held Items")]
    [SerializeField] private HeldItem sword;
    [SerializeField] private HeldItem roundShield;
    [SerializeField] private HeldItem heavyAxe;
    [SerializeField] private HeldItem towerShield;

    private Transform rightHand;
    private Transform leftHand;
    private HeldItem[] all;

    // the held item currently shown on each side (its .instance is the live clone)
    private HeldItem currentRight;
    private HeldItem currentLeft;

    private void Awake()
    {
        if (playerEquipment == null) playerEquipment = GetComponent<PlayerEquipment>();
        if (facing == null) facing = transform;

        rightHand = FindDeep(transform, rightHandBone);
        leftHand = FindDeep(transform, leftHandBone);

        foreach (var n in hideMeshNames)
        {
            var m = FindDeep(transform, n);
            if (m != null) foreach (var r in m.GetComponentsInChildren<Renderer>()) r.enabled = false;
        }

        all = new[] { sword, roundShield, heavyAxe, towerShield };
    }

    private void OnEnable()
    {
        if (playerEquipment != null) playerEquipment.OnLoadoutEquipped += HandleLoadout;
    }

    private void OnDisable()
    {
        if (playerEquipment != null) playerEquipment.OnLoadoutEquipped -= HandleLoadout;
    }

    private void Start() => Refresh();

    private void HandleLoadout(string _) => Refresh();

    private void Refresh()
    {
        var w = playerEquipment != null ? playerEquipment.EquippedWeapon : null;
        bool isAxe = w != null && w.WeaponName.ToLower().Contains("axe");

        EquipSide(isAxe ? heavyAxe : sword, ref currentRight);
        EquipSide(isAxe ? towerShield : roundShield, ref currentLeft);
    }

    /// <summary>
    /// Shows <paramref name="desired"/> on one side of the body, destroying only
    /// the previous runtime clone for that side. Bones and the original character
    /// meshes are never touched.
    /// </summary>
    private void EquipSide(HeldItem desired, ref HeldItem current)
    {
        // already showing exactly this item on this side
        if (current == desired && desired != null && desired.instance != null)
            return;

        // tear down the previous visual for THIS side only
        if (current != null && current.instance != null)
        {
            Destroy(current.instance);
            current.instance = null;
        }
        current = desired;

        if (desired == null || desired.prefab == null) return;

        // safety: never leak a prior instance of the incoming item
        if (desired.instance != null)
        {
            Destroy(desired.instance);
            desired.instance = null;
        }

        Spawn(desired);
        if (desired.instance != null) desired.instance.SetActive(true);
    }

    private void LateUpdate()
    {
        if (all == null) return;
        float k = 1f - Mathf.Exp(-stabiliseLerp * Time.deltaTime);

        foreach (var it in all)
        {
            if (it == null || it.instance == null || !it.instance.activeSelf || !it.stabiliseRotation) continue;

            Vector3 fwd = facing.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) continue;

            Quaternion target = Quaternion.LookRotation(fwd.normalized, Vector3.up) * Quaternion.Euler(it.stabilisedEuler);
            it.instance.transform.rotation = Quaternion.Slerp(it.instance.transform.rotation, target, k);
        }
    }

    private void Spawn(HeldItem item)
    {
        if (item == null || item.prefab == null) return;

        Transform parent = !string.IsNullOrEmpty(item.attachBone)
            ? FindDeep(transform, item.attachBone)
            : (item.leftSide ? leftHand : rightHand);
        if (parent == null) parent = item.leftSide ? leftHand : rightHand;
        if (parent == null) return;

        item.attach = parent;
        item.instance = Instantiate(item.prefab, parent);
        item.instance.transform.localPosition = item.localPosition;
        item.instance.transform.localEulerAngles = item.localEuler;
        item.instance.transform.localScale = Vector3.one * item.scale;
        foreach (var c in item.instance.GetComponentsInChildren<Collider>()) Destroy(c);
        item.instance.SetActive(false);
    }

    private void Show(HeldItem item, bool on)
    {
        if (item != null && item.instance != null) item.instance.SetActive(on);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    // ------------------------------------------------------------------
    // Editor preview - tune the held items with your eyes, no Play mode.
    // Right-click the component header -> "Preview Weapons In Editor",
    // adjust the Local Position / Local Euler / Scale (and Stabilised Euler
    // for shields) fields; the preview updates live. "Clear Editor Preview"
    // when done. Preview objects are never saved to the scene.
    // ------------------------------------------------------------------
    const string PreviewPrefix = "__PREVIEW_";

    [ContextMenu("Preview - Sword and Shield")]
    private void PreviewSwordLoadout() => EditorPreview(sword, roundShield);

    [ContextMenu("Preview - Great Axe")]
    private void PreviewAxeLoadout() => EditorPreview(heavyAxe, towerShield);

    private void EditorPreview(params HeldItem[] items)
    {
        ClearPreview();
        var rh = FindDeep(transform, rightHandBone);
        var lh = FindDeep(transform, leftHandBone);
        var facingT = facing != null ? facing : transform;

        foreach (var it in items)
        {
            if (it == null || it.prefab == null) continue;
            Transform parent = !string.IsNullOrEmpty(it.attachBone)
                ? FindDeep(transform, it.attachBone)
                : (it.leftSide ? lh : rh);
            if (parent == null) parent = it.leftSide ? lh : rh;
            if (parent == null) continue;

            var go = Instantiate(it.prefab, parent);
            go.name = PreviewPrefix + (string.IsNullOrEmpty(it.label) ? it.prefab.name : it.label);
            go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            foreach (var c in go.GetComponentsInChildren<Collider>()) DestroyImmediate(c);
            it.instance = go;
            ApplyPreview(it, facingT);
        }
    }

    [ContextMenu("Clear Editor Preview")]
    private void ClearPreview()
    {
        var kill = new System.Collections.Generic.List<GameObject>();
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t != null && t.name.StartsWith(PreviewPrefix)) kill.Add(t.gameObject);
        foreach (var g in kill) DestroyImmediate(g);
        foreach (var it in new[] { sword, roundShield, heavyAxe, towerShield })
            if (it != null) it.instance = null;
    }

    private void ApplyPreview(HeldItem it, Transform facingT)
    {
        if (it == null || it.instance == null) return;
        it.instance.transform.localPosition = it.localPosition;
        it.instance.transform.localScale = Vector3.one * it.scale;
        if (it.stabiliseRotation)
        {
            Vector3 fwd = facingT.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            it.instance.transform.rotation =
                Quaternion.LookRotation(fwd.normalized, Vector3.up) * Quaternion.Euler(it.stabilisedEuler);
        }
        else
        {
            it.instance.transform.localEulerAngles = it.localEuler;
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        var facingT = facing != null ? facing : transform;
        foreach (var it in new[] { sword, roundShield, heavyAxe, towerShield })
            if (it != null && it.instance != null && it.instance.name.StartsWith(PreviewPrefix))
                ApplyPreview(it, facingT);
    }
}
