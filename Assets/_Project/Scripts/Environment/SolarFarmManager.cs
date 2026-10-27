using System.Collections.Generic;
using UnityEngine;
using SolarDrone.Core;

namespace SolarDrone.Environment
{
    /// <summary>
    /// Manages the solar farm layout, keeps registry of panels, and provides targets to the agent.
    /// </summary>
    public class SolarFarmManager : MonoBehaviour
    {
        [Header("Solar Panels Registry")]
        [SerializeField] private List<SolarPanel> panels = new List<SolarPanel>();

        public IReadOnlyList<SolarPanel> Panels => panels;
        public int TotalPanels => panels.Count;

        private void Awake()
        {
            if (panels.Count == 0)
            {
                panels.AddRange(GetComponentsInChildren<SolarPanel>());
            }
        }

        public void RegisterPanels(IEnumerable<SolarPanel> newPanels)
        {
            panels.Clear();
            panels.AddRange(newPanels);
        }

        /// <summary>
        /// Strategy used by the AI agent to choose its next target.
        /// </summary>
        public enum PathStrategy
        {
            DistanceOptimized, // Spatial proximity optimization (Greedy Euclidean Nearest-Neighbor)
            Sequential         // Fixed linear array traversal
        }

        [Header("AI Path Strategy")]
        [SerializeField] private PathStrategy activeStrategy = PathStrategy.DistanceOptimized;

        public PathStrategy ActiveStrategy
        {
            get => activeStrategy;
            set => activeStrategy = value;
        }

        /// <summary>
        /// Dynamically calculates the optimal next target panel based on the selected AI strategy.
        /// In DistanceOptimized mode, it calculates 3D Euclidean distance to all uninspected panels
        /// and returns the closest one to minimize flight time and battery consumption.
        /// </summary>
        public SolarPanel GetOptimalNextPanel(Vector3 currentPosition)
        {
            if (activeStrategy == PathStrategy.Sequential)
            {
                return GetNextUninspectedPanel();
            }

            SolarPanel closestPanel = null;
            float shortestDistance = float.MaxValue;

            for (int i = 0; i < panels.Count; i++)
            {
                var p = panels[i];
                if (p != null && !p.IsInspected)
                {
                    float dist = Vector3.Distance(currentPosition, p.transform.position);
                    if (dist < shortestDistance)
                    {
                        shortestDistance = dist;
                        closestPanel = p;
                    }
                }
            }

            return closestPanel;
        }

        /// <summary>
        /// Retrieves the next uninspected panel in linear sequence.
        /// </summary>
        public SolarPanel GetNextUninspectedPanel()
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] != null && !panels[i].IsInspected)
                {
                    return panels[i];
                }
            }
            return null; // All panels inspected
        }

        /// <summary>
        /// Dynamically randomizes which panels have defects and assigns random defect types.
        /// Ensures every simulation run has different conditions!
        /// </summary>
        public void RandomizeDefects(int count = 2)
        {
            ResetAllPanels();

            // Clear defects on all panels first
            foreach (var p in panels)
            {
                p.Setup(p.PanelId, false, AnomalyType.None);
            }

            // Shuffle panel list
            var shuffled = new List<SolarPanel>(panels);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[r];
                shuffled[r] = temp;
            }

            int toAssign = Mathf.Min(count, shuffled.Count);
            List<string> assignedLog = new List<string>();

            for (int i = 0; i < toAssign; i++)
            {
                AnomalyType type = (Random.value > 0.5f) ? AnomalyType.ThermalHotspot : AnomalyType.PhysicalCrack;
                shuffled[i].Setup(shuffled[i].PanelId, true, type);
                assignedLog.Add($"{shuffled[i].PanelId} ({type})");
            }

            DroneEventBus.PublishDecisionLogged(
                $"[AI WORLD RANDOMIZED] Defect locations randomized: {string.Join(", ", assignedLog)}",
                LogSeverity.Warning
            );
        }

        /// <summary>
        /// Injects a defect onto a specific panel in real time while the drone is in flight!
        /// </summary>
        public bool InjectDefect(string panelId, AnomalyType type)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] != null && panels[i].PanelId == panelId)
                {
                    panels[i].Setup(panelId, true, type);
                    DroneEventBus.PublishDecisionLogged(
                        $"[REAL-TIME INJECTION] Defect {type} injected into {panelId}! Waiting for drone detection.",
                        LogSeverity.Alert
                    );
                    return true;
                }
            }
            return false;
        }

        public int GetInspectedCount()
        {
            int count = 0;
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] != null && panels[i].IsInspected)
                {
                    count++;
                }
            }
            return count;
        }

        public void ResetAllPanels()
        {
            foreach (var panel in panels)
            {
                if (panel != null)
                {
                    panel.ResetPanel();
                }
            }
        }
    }
}
