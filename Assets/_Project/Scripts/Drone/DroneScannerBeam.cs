using UnityEngine;
using SolarDrone.Core;

namespace SolarDrone.Drone
{
    /// <summary>
    /// Visual scanner projector mounted on the drone's sensor gimbal.
    /// Emits high-tech scanning spotlights onto panels during Primary and Secondary inspection phases.
    /// </summary>
    public class DroneScannerBeam : MonoBehaviour
    {
        [Header("Beam Light Source")]
        [SerializeField] private Light scannerLight;

        [Header("Beam Configurations")]
        [SerializeField] private Color primaryScanColor = new Color(0.15f, 0.85f, 1.0f);   // High-tech Cyan
        [SerializeField] private Color secondaryScanColor = new Color(1.0f, 0.40f, 0.10f); // Diagnostic Amber/Orange
        [SerializeField] private float primarySpotAngle = 48.0f;
        [SerializeField] private float secondarySpotAngle = 32.0f;

        private DroneState currentState = DroneState.IdleAtBase;
        private float targetIntensity = 0f;
        private float currentIntensity = 0f;

        private void Awake()
        {
            if (scannerLight == null)
            {
                scannerLight = GetComponent<Light>();
                if (scannerLight == null)
                {
                    scannerLight = gameObject.AddComponent<Light>();
                    scannerLight.type = LightType.Spot;
                    scannerLight.range = 10.0f;
                    scannerLight.intensity = 0f;
                    scannerLight.spotAngle = primarySpotAngle;
                    scannerLight.color = primaryScanColor;
                }
            }
            scannerLight.enabled = true;
            scannerLight.intensity = 0f;
        }

        private void OnEnable()
        {
            DroneEventBus.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            DroneEventBus.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(DroneState state)
        {
            currentState = state;

            switch (state)
            {
                case DroneState.PrimaryInspection:
                    scannerLight.color = primaryScanColor;
                    scannerLight.spotAngle = primarySpotAngle;
                    targetIntensity = 4.0f;
                    break;

                case DroneState.SecondaryInspection:
                    scannerLight.color = secondaryScanColor;
                    scannerLight.spotAngle = secondarySpotAngle;
                    targetIntensity = 6.0f;
                    break;

                default:
                    targetIntensity = 0f;
                    break;
            }
        }

        private void Update()
        {
            // Smoothly fade light intensity in and out
            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, 12.0f * Time.deltaTime);

            if (currentIntensity > 0.05f)
            {
                // Subtle pulse to simulate active radar/laser sensor sweep
                float pulse = 1.0f + 0.15f * Mathf.Sin(Time.time * 8.0f);
                scannerLight.intensity = currentIntensity * pulse;
            }
            else
            {
                scannerLight.intensity = 0f;
            }
        }
    }
}
