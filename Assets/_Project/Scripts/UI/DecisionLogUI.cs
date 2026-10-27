using System;
using System.Collections.Generic;
using UnityEngine;
using SolarDrone.Core;
using SolarDrone.Environment;
using SolarDrone.Drone;

namespace SolarDrone.UI
{
    /// <summary>
    /// AI Agent Decision & Reasoning Console.
    /// Visually distinct ground-control terminal displaying real-time FSM state reasoning,
    /// heuristic path calculations, anomaly classifications, and interactive scenario controls.
    /// </summary>
    public class DecisionLogUI : MonoBehaviour
    {
        private struct LogEntry
        {
            public string timestamp;
            public string message;
            public LogSeverity severity;
        }

        [Header("Configuration")]
        [SerializeField] private int maxLogHistory = 60;
        [SerializeField] private SolarFarmManager farmManager;
        [SerializeField] private DroneBattery droneBattery;

        private readonly List<LogEntry> logs = new List<LogEntry>();
        private Vector2 scrollPosition = Vector2.zero;

        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle logTextStyle;
        private GUIStyle actionButtonStyle;
        private Texture2D boxBackground;
        private Texture2D btnBackground;
        private bool stylesInitialized = false;

        private void OnEnable()
        {
            DroneEventBus.OnDecisionLogged += AddLogEntry;
        }

        private void OnDisable()
        {
            DroneEventBus.OnDecisionLogged -= AddLogEntry;
        }

        public void AddLogEntry(string message, LogSeverity severity)
        {
            logs.Add(new LogEntry
            {
                timestamp = DateTime.Now.ToString("HH:mm:ss"),
                message = message,
                severity = severity
            });

            if (logs.Count > maxLogHistory)
            {
                logs.RemoveAt(0);
            }

            // Auto-scroll to bottom
            scrollPosition.y = float.MaxValue;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            // Deep charcoal terminal background
            boxBackground = new Texture2D(1, 1);
            boxBackground.SetPixel(0, 0, new Color(0.04f, 0.05f, 0.08f, 0.94f));
            boxBackground.Apply();

            btnBackground = new Texture2D(1, 1);
            btnBackground.SetPixel(0, 0, new Color(0.18f, 0.22f, 0.32f, 0.90f));
            btnBackground.Apply();

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = boxBackground;
            boxStyle.padding = new RectOffset(12, 12, 10, 10);

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 14;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(1.0f, 0.75f, 0.20f); // Amber / Gold terminal header

            subtitleStyle = new GUIStyle(GUI.skin.label);
            subtitleStyle.fontSize = 10;
            subtitleStyle.normal.textColor = new Color(0.9f, 0.65f, 0.25f);

            logTextStyle = new GUIStyle(GUI.skin.label);
            logTextStyle.fontSize = 11;
            logTextStyle.wordWrap = true;

            actionButtonStyle = new GUIStyle(GUI.skin.button);
            actionButtonStyle.normal.background = btnBackground;
            actionButtonStyle.normal.textColor = Color.white;
            actionButtonStyle.fontSize = 11;
            actionButtonStyle.fontStyle = FontStyle.Bold;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!TelemetryUI.IsHUDVisible) return;

            InitStyles();

            if (farmManager == null) farmManager = FindAnyObjectByType<SolarFarmManager>();
            if (droneBattery == null) droneBattery = FindAnyObjectByType<DroneBattery>();

            // Dedicated AI Agent Brain Console (Top Right)
            float boxWidth = 390;
            float boxHeight = 350;
            float boxX = Screen.width - boxWidth - 20;
            float boxY = 46;

            GUILayout.BeginArea(new Rect(boxX, boxY, boxWidth, boxHeight), boxStyle);

            // AI Header
            GUILayout.Label("🧠 AI AGENT DECISION BRAIN", titleStyle);
            GUILayout.Label("AUTONOMOUS FSM REASONING & HEURISTIC ENGINE", subtitleStyle);
            GUILayout.Space(4);

            // 1. Interactive AI Strategy & Scenario Controls
            if (farmManager != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("AI STRATEGY:", GUI.skin.label, GUILayout.Width(85));
                bool isNearest = farmManager.ActiveStrategy == SolarFarmManager.PathStrategy.DistanceOptimized;
                GUI.color = isNearest ? Color.cyan : Color.yellow;
                string btnLabel = isNearest ? "NEAREST-NEIGHBOR (AI)" : "SEQUENTIAL (LINEAR)";
                if (GUILayout.Button(btnLabel, actionButtonStyle, GUILayout.Height(22)))
                {
                    farmManager.ActiveStrategy = isNearest 
                        ? SolarFarmManager.PathStrategy.Sequential 
                        : SolarFarmManager.PathStrategy.DistanceOptimized;
                    DroneEventBus.PublishDecisionLogged($"[AI STRATEGY] Switched path planning heuristic to: {farmManager.ActiveStrategy}", LogSeverity.Info);
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }

            // Scenario Randomizer & Defect Injection Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🎲 RANDOMIZE FARM", actionButtonStyle, GUILayout.Height(22)))
            {
                if (farmManager != null) farmManager.RandomizeDefects(UnityEngine.Random.Range(2, 4));
            }
            if (GUILayout.Button("+ Hotspot P3", actionButtonStyle, GUILayout.Height(22)))
            {
                if (farmManager != null) farmManager.InjectDefect("P3", AnomalyType.ThermalHotspot);
            }
            if (GUILayout.Button("+ Crack P6", actionButtonStyle, GUILayout.Height(22)))
            {
                if (farmManager != null) farmManager.InjectDefect("P6", AnomalyType.PhysicalCrack);
            }
            GUILayout.EndHorizontal();

            // Energy RTB Test Button
            if (GUILayout.Button("⚡ DRAIN BATTERY (-25% TO TEST DYNAMIC RTB)", actionButtonStyle, GUILayout.Height(20)))
            {
                if (droneBattery != null)
                {
                    droneBattery.SetBatteryLevel(Mathf.Max(5f, droneBattery.CurrentBattery - 25f));
                    DroneEventBus.PublishDecisionLogged("[MANUAL TEST] Battery drained to test dynamic energy budgeting RTB!", LogSeverity.Warning);
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("REASONING & EVENT STREAM (BLACK-BOX):", subtitleStyle);

            // 2. Real-Time AI Decision Stream
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < logs.Count; i++)
            {
                var entry = logs[i];
                Color entryColor = entry.severity switch
                {
                    LogSeverity.Alert => new Color(1.0f, 0.45f, 0.30f),  // Bright Anomaly Red
                    LogSeverity.Warning => new Color(1.0f, 0.82f, 0.25f),// Energy Warning Yellow
                    LogSeverity.Success => new Color(0.35f, 0.95f, 0.50f),// Success Green
                    _ => new Color(0.70f, 0.88f, 1.0f)                   // Navigation Cyan
                };

                GUI.color = entryColor;
                GUILayout.Label($"[{entry.timestamp}] {entry.message}", logTextStyle);
            }

            GUI.color = Color.white;
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
