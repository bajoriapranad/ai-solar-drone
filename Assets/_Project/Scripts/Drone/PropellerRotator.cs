using UnityEngine;
using SolarDrone.Core;

namespace SolarDrone.Drone
{
    /// <summary>
    /// Animates the quadcopter rotor blades.
    /// Rotates propellers dynamically based on drone state (high RPM during flight/inspection, idle spin at base).
    /// </summary>
    public class PropellerRotator : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float flightRpm = 1800.0f;
        [SerializeField] private float idleRpm = 240.0f;
        [SerializeField] private bool rotateClockwise = true;

        private DroneState currentState = DroneState.IdleAtBase;
        private float currentSpeed = 0f;

        public void Setup(bool clockwise)
        {
            rotateClockwise = clockwise;
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
        }

        private void Update()
        {
            float targetRpm;

            switch (currentState)
            {
                case DroneState.IdleAtBase:
                case DroneState.MissionComplete:
                    targetRpm = 0f;
                    break;
                case DroneState.TakingOff:
                case DroneState.NavigatingToPanel:
                case DroneState.PrimaryInspection:
                case DroneState.SecondaryInspection:
                case DroneState.ReturningToBase:
                    targetRpm = flightRpm;
                    break;
                case DroneState.Landing:
                    targetRpm = idleRpm * 2f;
                    break;
                default:
                    targetRpm = idleRpm;
                    break;
            }

            // Smooth speed acceleration/deceleration
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetRpm, 1200.0f * Time.deltaTime);

            if (currentSpeed > 0.1f)
            {
                float direction = rotateClockwise ? 1f : -1f;
                transform.Rotate(Vector3.up, direction * currentSpeed * 6f * Time.deltaTime, Space.Self);
            }
        }
    }
}
