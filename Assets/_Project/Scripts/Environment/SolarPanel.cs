using UnityEngine;
using SolarDrone.Core;

namespace SolarDrone.Environment
{
    /// <summary>
    /// Represents an individual solar panel in the solar farm.
    /// Manages inspection waypoints, simulated defect conditions, and visual indicators.
    /// </summary>
    public class SolarPanel : MonoBehaviour
    {
        [Header("Panel Configuration")]
        [SerializeField] private string panelId = "P1";
        [SerializeField] private bool hasSimulatedAnomaly = false;
        [SerializeField] private AnomalyType anomalyType = AnomalyType.None;

        [Header("Waypoints Relative Offsets")]
        [SerializeField] private Vector3 primaryScanOffset = new Vector3(0f, 4.0f, 0f);
        [SerializeField] private Vector3 secondaryScanOffset = new Vector3(0f, 1.8f, 0.5f);

        [Header("Visual Feedback")]
        [SerializeField] private Renderer panelRenderer;
        [SerializeField] private Color normalColor = new Color(0.12f, 0.22f, 0.45f); // Deep solar blue
        [SerializeField] private Color inspectedNormalColor = new Color(0.15f, 0.55f, 0.30f); // Clean green
        [SerializeField] private Color anomalyColor = new Color(0.90f, 0.25f, 0.15f); // Alert red/amber

        private bool isInspected = false;
        private Material panelMaterial;
        private GameObject defectMarker;

        public string PanelId => panelId;
        public bool HasSimulatedAnomaly => hasSimulatedAnomaly;
        public AnomalyType Anomaly => anomalyType;
        public bool IsInspected => isInspected;

        private void Awake()
        {
            if (panelRenderer == null)
            {
                panelRenderer = GetComponent<Renderer>();
            }

            if (panelRenderer != null)
            {
                panelMaterial = panelRenderer.material;
                panelMaterial.color = normalColor;
            }
        }

        public void Setup(string id, bool hasAnomaly, AnomalyType anomaly)
        {
            panelId = id;
            hasSimulatedAnomaly = hasAnomaly;
            anomalyType = anomaly;

            if (defectMarker != null)
            {
                Destroy(defectMarker);
                defectMarker = null;
            }

            if (hasAnomaly)
            {
                CreateDefectVisualMarker();
            }
        }

        private void CreateDefectVisualMarker()
        {
            if (anomalyType == AnomalyType.ThermalHotspot)
            {
                // Thermal Hotspot: Overheating circular cell marker
                defectMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                defectMarker.name = $"ThermalHotspot_{panelId}";
                defectMarker.transform.SetParent(transform);
                defectMarker.transform.localPosition = new Vector3(0.5f, 0.08f, 0.2f);
                defectMarker.transform.localRotation = Quaternion.identity;
                defectMarker.transform.localScale = new Vector3(0.6f, 0.04f, 0.6f);

                Renderer r = defectMarker.GetComponent<Renderer>();
                r.material = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                r.material.color = new Color(1.0f, 0.35f, 0.05f); // Hot incandescent orange
            }
            else if (anomalyType == AnomalyType.PhysicalCrack)
            {
                // Physical Crack: Structural fracture fissure line
                defectMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                defectMarker.name = $"PhysicalCrack_{panelId}";
                defectMarker.transform.SetParent(transform);
                defectMarker.transform.localPosition = new Vector3(-0.4f, 0.08f, -0.1f);
                defectMarker.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
                defectMarker.transform.localScale = new Vector3(1.2f, 0.03f, 0.08f);

                Renderer r = defectMarker.GetComponent<Renderer>();
                r.material = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                r.material.color = new Color(0.08f, 0.08f, 0.08f); // Fracture black
            }
        }

        /// <summary>
        /// Waypoint for standard high-level optical/thermal survey.
        /// </summary>
        public Vector3 GetPrimaryInspectionPoint()
        {
            return transform.position + primaryScanOffset;
        }

        /// <summary>
        /// Closer vantage waypoint for detailed secondary anomaly diagnostic scan.
        /// </summary>
        public Vector3 GetSecondaryInspectionPoint()
        {
            return transform.position + secondaryScanOffset;
        }

        /// <summary>
        /// Updates the visual appearance when inspection concludes.
        /// </summary>
        public void MarkAsInspected()
        {
            isInspected = true;
            if (panelMaterial != null)
            {
                panelMaterial.color = hasSimulatedAnomaly ? anomalyColor : inspectedNormalColor;
            }

            if (defectMarker != null)
            {
                Renderer r = defectMarker.GetComponent<Renderer>();
                if (r != null && anomalyType == AnomalyType.ThermalHotspot)
                {
                    r.material.color = new Color(1.0f, 0.15f, 0.05f); // High alert red-orange
                }
            }
        }

        /// <summary>
        /// Resets the panel state for repeated test runs.
        /// </summary>
        public void ResetPanel()
        {
            isInspected = false;
            if (panelMaterial != null)
            {
                panelMaterial.color = normalColor;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GetPrimaryInspectionPoint(), 0.3f);
            Gizmos.DrawLine(transform.position, GetPrimaryInspectionPoint());

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetSecondaryInspectionPoint(), 0.2f);
            Gizmos.DrawLine(transform.position, GetSecondaryInspectionPoint());
        }
    }
}
