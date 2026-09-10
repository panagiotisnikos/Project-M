using UnityEngine;

/// <summary>
/// Shows the right held meshes for the current loadout. The Axe Warrior model
/// ships with built-in dual axes (Axe_L / Axe_R) - those are hidden and replaced
/// with real weapon models parented to the hand bones.
///
/// Loadout mapping (by WeaponData.WeaponName):
///   contains "Sword" -> sword (R) + round shield (L)
///   contains "Axe"   -> heavy axe (R) + tower shield (L)
/// </summary>
public class PlayerWeaponVisuals : MonoBehaviour
{
    [System.Serializable]
    public class HeldItem
    {
        public string label;
        public GameObject prefab;
        public bool leftHand;
        public Vector3 localPosition;
        public Vector3 localEuler;
        public float scale = 18f;

        [System.NonSerialized] public GameObject instance;
    }

    [Header("References")]
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private string rightHandBone = "Hand.R";
    [SerializeField] private string leftHandBone = "Hand.L";
    [Tooltip("Built-in model meshes to hide (the warrior's default dual axes).")]
    [SerializeField] private string[] hideMeshNames = { "Axe_L", "Axe_R" };

    [Header("Held Items")]
    [SerializeField] private HeldItem sword;
    [SerializeField] private HeldItem roundShield;
    [SerializeField] private HeldItem heavyAxe;
    [SerializeField] private HeldItem towerShield;

    private Transform rightHand;
    private Transform leftHand;

    private void Awake()
    {
        if (playerEquipment == null) playerEquipment = GetComponent<PlayerEquipment>();

        rightHand = FindDeep(transform, rightHandBone);
        leftHand = FindDeep(transform, leftHandBone);

        foreach (var n in hideMeshNames)
        {
            var m = FindDeep(transform, n);
            if (m != null) foreach (var r in m.GetComponentsInChildren<Renderer>()) r.enabled = false;
        }

        Spawn(sword);
        Spawn(roundShield);
        Spawn(heavyAxe);
        Spawn(towerShield);
    }

    private void OnEnable()
    {
        if (playerEquipment != null) playerEquipment.OnLoadoutEquipped += HandleLoadout;
    }

    private void OnDisable()
    {
        if (playerEquipment != null) playerEquipment.OnLoadoutEquipped -= HandleLoadout;
    }

    private void Start()
    {
        Refresh();
    }

    private void HandleLoadout(string _)
    {
        Refresh();
    }

    private void Refresh()
    {
        var w = playerEquipment != null ? playerEquipment.EquippedWeapon : null;
        string name = w != null ? w.WeaponName : "";
        bool isAxe = name.ToLower().Contains("axe");

        Show(sword, !isAxe);
        Show(roundShield, !isAxe);
        Show(heavyAxe, isAxe);
        Show(towerShield, isAxe);
    }

    private void Spawn(HeldItem item)
    {
        if (item == null || item.prefab == null) return;
        var parent = item.leftHand ? leftHand : rightHand;
        if (parent == null) return;

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
}
