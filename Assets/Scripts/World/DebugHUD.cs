using UnityEngine;

public class DebugHUD : MonoBehaviour
{
    [Header("Debug Display")]
    [SerializeField] private bool showDebugHUD = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;
    [Header("Player Combat")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAttack playerAttack;

    [Header("Adaptive World")]
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;
    [SerializeField] private DemoObjectiveManager demoObjectiveManager;
    [SerializeField] private BossController bossController;
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private CampSpawner campSpawner;

    private void Awake()
    {
        FindMissingPlayerReferences();
    }
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugHUD = !showDebugHUD;

            Debug.Log(
                $"[DebugHUD] Developer debug " +
                $"{(showDebugHUD ? "enabled" : "disabled")}."
            );
        }
    }    
    
    private void OnGUI()
    {
        if (!showDebugHUD)
            return;

        GUI.Label(
            new Rect(
                Screen.width - 190,
                10,
                180,
                25
            ),
            "DEVELOPER DEBUG [F3]"
        );

        DrawCombatPanel();
        DrawAdaptivePanel();
        DrawAdaptiveDecisionPanel();
        DrawFinalPerformancePanel();
    }

    private void FindMissingPlayerReferences()
    {
        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<PlayerHealth>();
        }

        if (playerStamina == null)
        {
            playerStamina =
                FindFirstObjectByType<PlayerStamina>();
        }

        if (playerEquipment == null)
        {
            playerEquipment =
                FindFirstObjectByType<PlayerEquipment>();
        }

        if (playerMovement == null)
        {
            playerMovement =
                FindFirstObjectByType<PlayerMovement>();
        }

        if (playerAttack == null)
        {
            playerAttack =
                FindFirstObjectByType<PlayerAttack>();
        }
    }

    private void DrawCombatPanel()
    {
        GUI.Box(
            new Rect(10, 10, 340, 165),
            "Combat Debug"
        );

        if (playerHealth != null)
        {
            float normalizedHealth =
                playerHealth.MaxHealth > 0
                    ? (float)playerHealth.CurrentHealth /
                      playerHealth.MaxHealth
                    : 0f;

            DrawResourceBar(
                new Rect(25, 40, 310, 20),
                normalizedHealth,
                $"Health: {playerHealth.CurrentHealth} / " +
                $"{playerHealth.MaxHealth}",
                new Color(0.25f, 0.8f, 0.3f)
            );
        }
        else
        {
            GUI.Label(
                new Rect(25, 40, 310, 20),
                "Health: PlayerHealth missing"
            );
        }

        if (playerStamina != null)
        {
            DrawResourceBar(
                new Rect(25, 70, 310, 20),
                playerStamina.NormalizedStamina,
                $"Stamina: " +
                $"{playerStamina.CurrentStamina:0.0} / " +
                $"{playerStamina.MaxStamina:0.0}",
                new Color(0.25f, 0.7f, 1f)
            );
        }
        else
        {
            GUI.Label(
                new Rect(25, 70, 310, 20),
                "Stamina: PlayerStamina missing"
            );
        }

        string weaponName =
            playerEquipment != null &&
            playerEquipment.EquippedWeapon != null
                ? playerEquipment.EquippedWeapon.WeaponName
                : "None";

        string shieldName =
            playerEquipment != null &&
            playerEquipment.EquippedShield != null
                ? playerEquipment.EquippedShield.ShieldName
                : "None";

        GUI.Label(
            new Rect(25, 100, 300, 20),
            $"Weapon: {weaponName}"
        );

        GUI.Label(
            new Rect(25, 120, 300, 20),
            $"Shield: {shieldName}"
        );

        GUI.Label(
            new Rect(25, 140, 300, 20),
            $"Action: {GetCurrentPlayerAction()}"
        );
    }

    private void DrawAdaptivePanel()
    {
        GUI.Box(
            new Rect(10, 185, 340, 270),
            "Adaptive Debug"
        );

        if (performanceTracker == null ||
            worldAdaptationManager == null)
        {
            GUI.Label(
                new Rect(25, 215, 300, 20),
                "Adaptive references missing."
            );

            return;
        }

        float score =
            performanceTracker.GetPerformanceScore();

        GUI.Label(
            new Rect(25, 215, 300, 20),
            $"World State: " +
            $"{worldAdaptationManager.CurrentState}"
        );

        GUI.Label(
            new Rect(25, 235, 300, 20),
            $"Score: {score:0.0}"
        );

        GUI.Label(
            new Rect(25, 255, 300, 20),
            $"Kills: {performanceTracker.EnemiesKilled}"
        );

        GUI.Label(
            new Rect(25, 275, 300, 20),
            $"Damage Taken: " +
            $"{performanceTracker.DamageTaken}"
        );

        GUI.Label(
            new Rect(25, 295, 300, 20),
            $"Time Alive: " +
            $"{performanceTracker.TimeAlive:0.0}s"
        );

        if (enemyAI != null)
        {
            GUI.Label(
                new Rect(25, 325, 300, 20),
                "Enemy Adaptation:"
            );

            GUI.Label(
                new Rect(25, 345, 300, 20),
                $"Move: " +
                $"{enemyAI.GetMoveSpeedModifier():0.00}x"
            );

            GUI.Label(
                new Rect(25, 365, 300, 20),
                $"Detection: " +
                $"{enemyAI.GetDetectionModifier():0.00}x"
            );

            GUI.Label(
                new Rect(25, 385, 300, 20),
                $"Attack Cooldown: " +
                $"{enemyAI.GetAttackCooldownModifier():0.00}x"
            );
        }

        if (demoObjectiveManager != null)
        {
            GUI.Label(
                new Rect(25, 415, 300, 20),
                $"Objective: " +
                $"{demoObjectiveManager.CurrentObjective}"
            );

            if (!string.IsNullOrEmpty(
                    demoObjectiveManager.StatusMessage))
            {
                GUI.Label(
                    new Rect(25, 435, 300, 20),
                    demoObjectiveManager.StatusMessage
                );
            }
        }
    }

    private void DrawAdaptiveDecisionPanel()
    {
        if (campSpawner == null)
            return;

        GUI.Box(
            new Rect(360, 10, 320, 130),
            "Adaptive Decision"
        );

        if (!campSpawner.HasSpawned)
        {
            GUI.Label(
                new Rect(375, 40, 290, 20),
                "Adaptive Camp: Waiting"
            );

            GUI.Label(
                new Rect(375, 60, 290, 20),
                "Reason: Clear first camp"
            );

            return;
        }

        GUI.Label(
            new Rect(375, 40, 290, 20),
            $"State: {campSpawner.LastGeneratedState}"
        );

        GUI.Label(
            new Rect(375, 60, 290, 20),
            $"Score: {campSpawner.LastScore:0.0}"
        );

        GUI.Label(
            new Rect(375, 80, 290, 20),
            $"Generated: {campSpawner.LastComposition}"
        );

        GUI.Label(
            new Rect(375, 100, 290, 20),
            "Reason: kills, damage, time"
        );
    }

    private void DrawFinalPerformancePanel()
    {
        if (demoObjectiveManager == null ||
            performanceTracker == null ||
            worldAdaptationManager == null)
        {
            return;
        }

        if (!demoObjectiveManager.IsDemoCompleted)
        {
            return;
        }

        GUI.Box(
            new Rect(360, 150, 320, 170),
            "Final Performance Report"
        );

        GUI.Label(
            new Rect(375, 180, 290, 20),
            $"Kills: {performanceTracker.EnemiesKilled}"
        );

        GUI.Label(
            new Rect(375, 200, 290, 20),
            $"Damage Taken: " +
            $"{performanceTracker.DamageTaken}"
        );

        GUI.Label(
            new Rect(375, 220, 290, 20),
            $"Time Alive: " +
            $"{performanceTracker.TimeAlive:0.0}s"
        );

        GUI.Label(
            new Rect(375, 240, 290, 20),
            $"Score: " +
            $"{performanceTracker.GetPerformanceScore():0.0}"
        );

        GUI.Label(
            new Rect(375, 260, 290, 20),
            $"Final State: " +
            $"{worldAdaptationManager.CurrentState}"
        );

        if (bossController != null)
        {
            GUI.Label(
                new Rect(375, 280, 290, 20),
                $"Boss Profile: " +
                $"{bossController.CurrentProfile}"
            );
        }
    }

    private string GetCurrentPlayerAction()
    {
        if (playerMovement != null)
        {
            if (playerMovement.IsDodging)
            {
                return "Dodging";
            }

            if (playerMovement.IsBlocking)
            {
                return playerMovement.IsParryWindowOpen
                    ? "Parry Window"
                    : "Blocking";
            }
        }

        if (playerAttack != null)
        {
            if (playerAttack.IsUsingHeavyAttack)
            {
                return "Heavy Attack";
            }

            if (playerAttack.IsAttacking)
            {
                return
                    $"Light Attack " +
                    $"{playerAttack.CurrentComboStep}";
            }
        }

        return "Idle";
    }

    private void DrawResourceBar(
        Rect rect,
        float normalizedValue,
        string label,
        Color fillColor)
    {
        normalizedValue =
            Mathf.Clamp01(normalizedValue);

        GUI.Box(
            rect,
            GUIContent.none
        );

        Rect fillRect =
            new Rect(
                rect.x + 2f,
                rect.y + 2f,
                (rect.width - 4f) *
                normalizedValue,
                rect.height - 4f
            );

        Color previousColor =
            GUI.color;

        GUI.color =
            fillColor;

        GUI.DrawTexture(
            fillRect,
            Texture2D.whiteTexture
        );

        GUI.color =
            previousColor;

        GUI.Label(
            new Rect(
                rect.x + 5f,
                rect.y,
                rect.width - 10f,
                rect.height
            ),
            label
        );
    }
}