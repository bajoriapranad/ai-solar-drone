using System.Collections;
using UnityEngine;
using SolarDrone.Core;
using SolarDrone.Drone;
using SolarDrone.Environment;

namespace SolarDrone.Agent
{
    /// <summary>
    /// Autonomous agent Finite State Machine (FSM).
    /// Drives mission lifecycle, target selection, waypoint navigation, anomaly handling,
    /// and safety-critical Return-to-Base (RTB) interrupts.
    /// </summary>
    public class DroneAgentFSM : MonoBehaviour
    {
        [Header("System Dependencies")]
        [SerializeField] private DroneMovement movement;
        [SerializeField] private DroneBattery battery;
        [SerializeField] private SolarFarmManager farmManager;
        [SerializeField] private BaseStation baseStation;
        [SerializeField] private InspectionLogic inspectionLogic;

        [Header("State Tracking")]
        [SerializeField] private DroneState currentState = DroneState.IdleAtBase;
        [SerializeField] private SolarPanel currentTargetPanel;
        [SerializeField] private bool autoStartMission = true;

        private Coroutine activeStateRoutine;

        public DroneState CurrentState => currentState;
        public SolarPanel CurrentTarget => currentTargetPanel;

        private void OnEnable()
        {
            DroneEventBus.OnLowBatteryTriggered += HandleLowBatteryInterrupt;
        }

        private void OnDisable()
        {
            DroneEventBus.OnLowBatteryTriggered -= HandleLowBatteryInterrupt;
        }

        private void Start()
        {
            SetState(DroneState.IdleAtBase);
            DroneEventBus.PublishDecisionLogged("[AI AGENT] System online. Pre-flight checks passed.", LogSeverity.Info);

            if (autoStartMission)
            {
                StartCoroutine(DelayedAutoStart());
            }
        }

        private void Update()
        {
            // Press Spacebar to start or restart mission
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (currentState == DroneState.IdleAtBase || currentState == DroneState.MissionComplete)
                {
                    StartMission();
                }
            }
        }

        private IEnumerator DelayedAutoStart()
        {
            yield return new WaitForSeconds(1.5f);
            if (currentState == DroneState.IdleAtBase)
            {
                StartMission();
            }
        }

        /// <summary>
        /// Initiates the autonomous solar farm inspection mission.
        /// </summary>
        [ContextMenu("Start Mission")]
        public void StartMission()
        {
            if (currentState != DroneState.IdleAtBase && currentState != DroneState.MissionComplete)
            {
                Debug.LogWarning("[AI AGENT] Mission can only start from IdleAtBase or MissionComplete.");
                return;
            }

            DroneEventBus.PublishDecisionLogged("[AI AGENT] Mission start authorized. Initiating automated takeoff sequence.", LogSeverity.Info);
            SetState(DroneState.TakingOff);

            Vector3 takeoffPoint = baseStation != null ? baseStation.GetTakeoffPoint() : transform.position + Vector3.up * 5.0f;
            movement.Takeoff(takeoffPoint, () =>
            {
                DroneEventBus.PublishDecisionLogged("[AI AGENT] Takeoff complete. Reached cruise altitude.", LogSeverity.Info);
                SelectNextTarget();
            });
        }

        /// <summary>
        /// Selects the next optimal uninspected panel from the solar farm manager using dynamic spatial heuristics
        /// and enforces real-time energy-aware return-to-base budgeting.
        /// </summary>
        private void SelectNextTarget()
        {
            // Dynamic Energy-Aware Budgeting: Calculate real-time energy required to return home from current 3D position
            Vector3 homePos = baseStation != null ? baseStation.HomePosition : Vector3.zero;
            float distToHome = Vector3.Distance(transform.position, homePos);
            float speed = (movement != null && movement.FlightSpeed > 0f) ? movement.FlightSpeed : 5.0f;
            float dischargeRate = (battery != null) ? battery.DischargeRatePerSecond : 2.5f;
            float energyRequiredForReturn = (distToHome / speed) * dischargeRate;
            float dynamicSafetyReserve = 8.0f; // 8% reserve buffer

            if (battery != null && battery.CurrentBattery <= (energyRequiredForReturn + dynamicSafetyReserve))
            {
                InitiateReturnToBase(
                    $"[DYNAMIC ENERGY BUDGET] Battery at {battery.BatteryPercentage:F1}% reaches return limit ({energyRequiredForReturn:F1}% needed for {distToHome:F1}m return + {dynamicSafetyReserve}% reserve)!"
                );
                return;
            }

            // Heuristic Path Selection: Query nearest-neighbor optimal panel
            currentTargetPanel = farmManager != null 
                ? farmManager.GetOptimalNextPanel(transform.position) 
                : null;

            if (currentTargetPanel == null)
            {
                DroneEventBus.PublishDecisionLogged("[AI AGENT] All panels successfully inspected! Returning to base station.", LogSeverity.Success);
                InitiateReturnToBase("Mission objectives complete.");
                return;
            }

            float distToTarget = Vector3.Distance(transform.position, currentTargetPanel.transform.position);
            DroneEventBus.PublishTargetSelected(currentTargetPanel.PanelId);
            DroneEventBus.PublishDecisionLogged(
                $"[AI PATH HEURISTIC] Target selected: {currentTargetPanel.PanelId} (Dist: {distToTarget:F1}m | Battery: {battery?.BatteryPercentage:F0}%). Navigating...",
                LogSeverity.Info
            );

            SetState(DroneState.NavigatingToPanel);
            movement.FlyTo(currentTargetPanel.GetPrimaryInspectionPoint(), OnArrivalAtPrimaryInspection);
        }

        private void OnArrivalAtPrimaryInspection()
        {
            if (currentState != DroneState.NavigatingToPanel) return;

            SetState(DroneState.PrimaryInspection);
            StartCoroutine(ExecutePrimaryInspectionRoutine());
        }

        private IEnumerator ExecutePrimaryInspectionRoutine()
        {
            bool hasAnomaly = false;
            AnomalyType anomalyType = AnomalyType.None;

            yield return inspectionLogic.PerformPrimaryScan(currentTargetPanel, (defect, type) =>
            {
                hasAnomaly = defect;
                anomalyType = type;
            });

            if (hasAnomaly)
            {
                // Anomaly reaction: execute secondary close-up orbital scan
                SetState(DroneState.SecondaryInspection);
                DroneEventBus.PublishDecisionLogged(
                    $"[AI AGENT] Anomaly detected ({anomalyType})! Diverting to secondary close-up waypoint for verification.",
                    LogSeverity.Alert
                );

                movement.FlyTo(currentTargetPanel.GetSecondaryInspectionPoint(), () =>
                {
                    StartCoroutine(ExecuteSecondaryInspectionRoutine());
                });
            }
            else
            {
                ConcludePanelInspection();
            }
        }

        private IEnumerator ExecuteSecondaryInspectionRoutine()
        {
            yield return inspectionLogic.PerformSecondaryScan(currentTargetPanel, () =>
            {
                ConcludePanelInspection();
            });
        }

        private void ConcludePanelInspection()
        {
            if (currentTargetPanel != null)
            {
                currentTargetPanel.MarkAsInspected();
                DroneEventBus.PublishInspectionCompleted(currentTargetPanel.PanelId);
                DroneEventBus.PublishDecisionLogged($"[AI AGENT] Inspection concluded for {currentTargetPanel.PanelId}.", LogSeverity.Info);
            }

            // Move to next target or return home
            SelectNextTarget();
        }

        /// <summary>
        /// Event listener triggered immediately when battery falls below the safety threshold.
        /// </summary>
        private void HandleLowBatteryInterrupt()
        {
            if (currentState == DroneState.ReturningToBase || currentState == DroneState.Landing || currentState == DroneState.IdleAtBase)
            {
                return; // Already returning or grounded
            }

            InitiateReturnToBase("CRITICAL: Safety-triggered Low Battery Interrupt!");
        }

        /// <summary>
        /// Enforces Return-to-Base (RTB) navigation.
        /// </summary>
        [ContextMenu("Abort To Base")]
        public void InitiateReturnToBase(string reason)
        {
            DroneEventBus.PublishDecisionLogged($"[AI DECISION] Returning to base! Reason: {reason}", LogSeverity.Warning);
            SetState(DroneState.ReturningToBase);

            // Halt any current scanning or waypoint flights
            StopAllCoroutines();
            movement.StopCurrentFlight();

            Vector3 rtbPoint = baseStation != null ? baseStation.GetTakeoffPoint() : new Vector3(0f, 5.0f, 0f);
            movement.FlyTo(rtbPoint, () =>
            {
                ExecuteLanding();
            });
        }

        private void ExecuteLanding()
        {
            SetState(DroneState.Landing);
            DroneEventBus.PublishDecisionLogged("[AI AGENT] Overhead base station. Commencing landing descent.", LogSeverity.Info);

            Vector3 landingPad = baseStation != null ? baseStation.HomePosition : Vector3.zero;
            movement.Land(landingPad, () =>
            {
                if (farmManager != null && farmManager.GetInspectedCount() == farmManager.TotalPanels && farmManager.TotalPanels > 0)
                {
                    SetState(DroneState.MissionComplete);
                    DroneEventBus.PublishMissionCompleted();
                    DroneEventBus.PublishDecisionLogged("[MISSION COMPLETE] All solar panels inspected and logged. Drone docked at base.", LogSeverity.Success);
                }
                else
                {
                    SetState(DroneState.IdleAtBase);
                    DroneEventBus.PublishDecisionLogged("[AI AGENT] Drone safely landed on base pad. Standby mode.", LogSeverity.Info);
                }
            });
        }

        private void SetState(DroneState newState)
        {
            currentState = newState;
            DroneEventBus.PublishStateChanged(newState);
        }
    }
}
