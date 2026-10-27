using UnityEngine;
using SolarDrone.Core;
using SolarDrone.Agent;
using SolarDrone.Environment;
using SolarDrone.Drone;

namespace SolarDrone.UI
{
    /// <summary>
    /// Displays real-time drone telemetry: State, Active Target, Battery %, and AI Decision.
    /// Uses robust OnGUI rendering with dark-themed HUD styling for zero-setup instant execution.
    /// </summary>
    public class TelemetryUI : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private DroneAgentFSM agentFSM;
        [SerializeField] private SolarFarmManager farmManager;
        [SerializeField] private DroneBattery droneBattery;

        private DroneState currentState = DroneState.IdleAtBase;
        private string currentTarget = "None";
        private float batteryPercent = 100.0f;
        private string latestDecision = "System standing by.";
        private LogSeverity decisionSeverity = LogSeverity.Info;

        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle alertStyle;
        private GUIStyle buttonStyle;
        private Texture2D panelBackground;
        private Texture2D buttonBackground;
        private bool stylesInitialized = false;

        private readonly System.Collections.Generic.List<string> detectedAnomalies = new System.Collections.Generic.List<string>();
        private float simSpeed = 1.0f;
        private float missionStartTime = 0f;
        private bool isMissionRunning = false;

        public static bool IsHUDVisible = true;

        private void OnEnable()
        {
            DroneEventBus.OnStateChanged += HandleStateChanged;
            DroneEventBus.OnTargetSelected += HandleTargetSelected;
            DroneEventBus.OnBatteryUpdated += HandleBatteryUpdated;
            DroneEventBus.OnDecisionLogged += HandleDecisionLogged;
            DroneEventBus.OnAnomalyDetected += HandleAnomalyDetected;
        }

        private void OnDisable()
        {
            DroneEventBus.OnStateChanged -= HandleStateChanged;
            DroneEventBus.OnTargetSelected -= HandleTargetSelected;
            DroneEventBus.OnBatteryUpdated -= HandleBatteryUpdated;
            DroneEventBus.OnDecisionLogged -= HandleDecisionLogged;
            DroneEventBus.OnAnomalyDetected -= HandleAnomalyDetected;
        }

        private void HandleAnomalyDetected(string panelId, AnomalyType type)
        {
            string entry = $"{panelId} [{type}]";
            if (!detectedAnomalies.Contains(entry))
            {
                detectedAnomalies.Add(entry);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H)) IsHUDVisible = !IsHUDVisible;
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetSimSpeed(1.0f);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetSimSpeed(2.0f);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetSimSpeed(4.0f);
        }

        public void SetSimSpeed(float speed)
        {
            simSpeed = speed;
            Time.timeScale = speed;
            DroneEventBus.PublishDecisionLogged($"[SIM] Simulation speed set to {speed}x.", LogSeverity.Info);
        }

        private void HandleStateChanged(DroneState newState)
        {
            currentState = newState;
            if (newState == DroneState.TakingOff && !isMissionRunning)
            {
                isMissionRunning = true;
                missionStartTime = Time.realtimeSinceStartup;
                detectedAnomalies.Clear();
            }
        }

        private void HandleTargetSelected(string panelId)
        {
            currentTarget = panelId;
        }

        private void HandleBatteryUpdated(float current, float max)
        {
            batteryPercent = (current / max) * 100.0f;
        }

        private void HandleDecisionLogged(string message, LogSeverity severity)
        {
            latestDecision = message;
            decisionSeverity = severity;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            panelBackground = MakeColorTexture(new Color(0.08f, 0.10f, 0.14f, 0.92f));
            buttonBackground = MakeColorTexture(new Color(0.20f, 0.35f, 0.65f, 0.90f));

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = panelBackground;
            panelStyle.padding = new RectOffset(16, 16, 12, 12);

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 15;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(0.35f, 0.85f, 1.0f);

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 13;
            labelStyle.normal.textColor = new Color(0.75f, 0.80f, 0.85f);

            valueStyle = new GUIStyle(GUI.skin.label);
            valueStyle.fontSize = 13;
            valueStyle.fontStyle = FontStyle.Bold;
            valueStyle.normal.textColor = Color.white;

            alertStyle = new GUIStyle(valueStyle);
            alertStyle.normal.textColor = new Color(1.0f, 0.45f, 0.35f);

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = buttonBackground;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.fontSize = 11;

            stylesInitialized = true;
        }

        private Texture2D MakeColorTexture(Color col)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            InitStyles();

            if (!IsHUDVisible)
            {
                GUI.color = Color.cyan;
                if (GUI.Button(new Rect(20, 20, 130, 26), "👁 SHOW HUD [H]"))
                {
                    IsHUDVisible = true;
                }
                GUI.color = Color.white;
                return;
            }

            // Drone Hardware Avionics Panel (Top Left)
            float altitude = agentFSM != null ? agentFSM.transform.position.y : 0f;
            float airspeed = (agentFSM != null && agentFSM.GetComponent<DroneMovement>() != null) 
                ? agentFSM.GetComponent<DroneMovement>().CurrentVelocity.magnitude 
                : 0f;

            GUILayout.BeginArea(new Rect(20, 20, 360, 310), panelStyle);
            GUILayout.Label("🚁 DRONE ONBOARD AVIONICS", titleStyle);
            GUI.color = new Color(0.4f, 0.7f, 1f);
            GUILayout.Label("HARDWARE INSTRUMENTATION & FLIGHT CONTROLS", GUI.skin.label);
            GUI.color = Color.white;
            GUILayout.Space(4);

            // Flight State
            GUILayout.BeginHorizontal();
            GUILayout.Label("FLIGHT STATE:", labelStyle, GUILayout.Width(130));
            Color stateColor = GetStateColor(currentState);
            GUI.color = stateColor;
            GUILayout.Label(currentState.ToString(), valueStyle);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // Flight Dynamics: Altitude & Airspeed
            GUILayout.BeginHorizontal();
            GUILayout.Label("ALTITUDE / SPEED:", labelStyle, GUILayout.Width(130));
            GUILayout.Label($"{altitude:F2} m  |  {airspeed:F1} m/s", valueStyle);
            GUILayout.EndHorizontal();

            // Target Waypoint
            GUILayout.BeginHorizontal();
            GUILayout.Label("TARGET WAYPOINT:", labelStyle, GUILayout.Width(130));
            GUILayout.Label(currentTarget, valueStyle);
            GUILayout.EndHorizontal();

            // Battery Level with visual gauge
            GUILayout.BeginHorizontal();
            GUILayout.Label("BATTERY CHARGE:", labelStyle, GUILayout.Width(130));
            Color batColor = batteryPercent <= 25.0f ? Color.red : (batteryPercent <= 50.0f ? Color.yellow : Color.green);
            GUI.color = batColor;
            string batGauge = batteryPercent > 75f ? "[████] " : batteryPercent > 50f ? "[███░] " : batteryPercent > 25f ? "[██░░] " : "[█░░░] ";
            GUILayout.Label($"{batGauge}{batteryPercent:F1}% (14.8V)", valueStyle);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // Hardware Rotors Status
            GUILayout.BeginHorizontal();
            GUILayout.Label("MOTORS (4x ROTOR):", labelStyle, GUILayout.Width(130));
            bool isAirborne = currentState != DroneState.IdleAtBase && currentState != DroneState.MissionComplete;
            GUI.color = isAirborne ? Color.green : Color.gray;
            GUILayout.Label(isAirborne ? "ONLINE (1800 RPM)" : "STANDBY (IDLE)", valueStyle);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // Flight Command Buttons
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.3f, 0.9f, 0.4f);
            if (GUILayout.Button("START MISSION", buttonStyle, GUILayout.Height(28)))
            {
                if (agentFSM != null) agentFSM.StartMission();
            }
            GUI.color = new Color(1.0f, 0.4f, 0.3f);
            if (GUILayout.Button("FORCE RTB", buttonStyle, GUILayout.Height(28)))
            {
                if (agentFSM != null) agentFSM.InitiateReturnToBase("Manual Operator Override");
            }
            GUI.color = Color.white;
            if (GUILayout.Button("AUDIT REPORT", buttonStyle, GUILayout.Height(28)))
            {
                ExportAuditReport();
            }
            GUILayout.EndHorizontal();

            // Sim Speed Multiplier Buttons
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("SIM SPEED:", labelStyle, GUILayout.Width(80));
            GUI.color = simSpeed == 1f ? Color.cyan : Color.white;
            if (GUILayout.Button("1x", buttonStyle, GUILayout.Width(45), GUILayout.Height(22))) SetSimSpeed(1.0f);
            GUI.color = simSpeed == 2f ? Color.cyan : Color.white;
            if (GUILayout.Button("2x", buttonStyle, GUILayout.Width(45), GUILayout.Height(22))) SetSimSpeed(2.0f);
            GUI.color = simSpeed == 4f ? Color.cyan : Color.white;
            if (GUILayout.Button("4x", buttonStyle, GUILayout.Width(45), GUILayout.Height(22))) SetSimSpeed(4.0f);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            // Keyboard Shortcuts Banner (Top Center)
            float bannerWidth = 460;
            float bannerX = (Screen.width - bannerWidth) * 0.5f;
            GUI.color = new Color(0.08f, 0.12f, 0.18f, 0.85f);
            GUI.Box(new Rect(bannerX, 10, bannerWidth, 26), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(bannerX + 10, 14, bannerWidth - 20, 20), 
                "<b>[C]</b> Switch Camera  |  <b>[T]</b> FLIR Thermal IR  |  <b>[1/2/4]</b> Speed  |  <b>[H]</b> Hide UI");
        }

        private void ExportAuditReport()
        {
            try
            {
                float duration = isMissionRunning ? (Time.realtimeSinceStartup - missionStartTime) : 0f;
                string reportPath = System.IO.Path.Combine(Application.dataPath, "..", "SolarFarm_Audit_Report.txt");
                reportPath = System.IO.Path.GetFullPath(reportPath);

                using (var writer = new System.IO.StreamWriter(reportPath, false))
                {
                    writer.WriteLine("==========================================================");
                    writer.WriteLine("       SOLAR DRONE AUTONOMOUS MISSION AUDIT REPORT        ");
                    writer.WriteLine("==========================================================");
                    writer.WriteLine($"Timestamp:         {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Drone Final State: {currentState}");
                    writer.WriteLine($"Remaining Battery: {batteryPercent:F1}%");
                    writer.WriteLine($"Flight Duration:   {duration:F1} seconds");
                    writer.WriteLine($"Anomalies Found:   {detectedAnomalies.Count}");
                    writer.WriteLine("----------------------------------------------------------");
                    writer.WriteLine("ANOMALY LOG:");
                    if (detectedAnomalies.Count == 0)
                    {
                        writer.WriteLine("  No defects detected. All panels certified nominal.");
                    }
                    else
                    {
                        foreach (var anom in detectedAnomalies)
                        {
                            writer.WriteLine($"  - {anom}");
                        }
                    }
                    writer.WriteLine("==========================================================");
                }

                DroneEventBus.PublishDecisionLogged($"[AUDIT] Report exported to SolarFarm_Audit_Report.txt!", LogSeverity.Success);
            }
            catch (System.Exception ex)
            {
                DroneEventBus.PublishDecisionLogged($"[AUDIT ERROR] Failed to export report: {ex.Message}", LogSeverity.Warning);
            }
        }

        private Color GetStateColor(DroneState state)
        {
            return state switch
            {
                DroneState.IdleAtBase => Color.gray,
                DroneState.TakingOff => new Color(0.3f, 0.8f, 1f),
                DroneState.NavigatingToPanel => Color.cyan,
                DroneState.PrimaryInspection => Color.yellow,
                DroneState.SecondaryInspection => new Color(1f, 0.5f, 0.2f),
                DroneState.ReturningToBase => new Color(1f, 0.3f, 0.3f),
                DroneState.Landing => new Color(1f, 0.7f, 0.2f),
                DroneState.MissionComplete => Color.green,
                _ => Color.white
            };
        }
    }
}
