using System;
using System.Collections;
using UnityEngine;

namespace SolarDrone.Drone
{
    /// <summary>
    /// Kinematic drone flight controller handling smooth 3D waypoint navigation,
    /// heading orientation, altitude changes, takeoff, and landing.
    /// </summary>
    public class DroneMovement : MonoBehaviour
    {
        [Header("Flight Dynamics")]
        [SerializeField] private float flightSpeed = 5.5f;
        [SerializeField] private float turnSpeed = 4.5f;
        [SerializeField] private float arrivalThreshold = 0.12f;
        [SerializeField] private float maxPitchTilt = 15.0f; // Forward tilt during acceleration
        [SerializeField] private float maxRollBank = 12.0f;  // Roll tilt during horizontal turns
        [SerializeField] private float hoverOscillationSpeed = 2.4f;
        [SerializeField] private float hoverOscillationAmount = 0.04f;

        private Coroutine activeFlightRoutine;
        private bool isFlying = false;
        private Vector3 currentVelocity = Vector3.zero;
        private float hoverTime = 0f;

        public bool IsFlying => isFlying;
        public float FlightSpeed => flightSpeed;
        public Vector3 CurrentVelocity => currentVelocity;

        private void Update()
        {
            // Natural atmospheric micro-hover oscillation when airborne and not in full flight
            if (!isFlying && transform.position.y > 0.5f)
            {
                hoverTime += Time.deltaTime * hoverOscillationSpeed;
                float verticalFloat = Mathf.Sin(hoverTime) * hoverOscillationAmount * Time.deltaTime;
                float microWobble = Mathf.Sin(hoverTime * 1.3f) * 0.3f;
                transform.position += new Vector3(0f, verticalFloat, 0f);
                transform.Rotate(Vector3.forward, microWobble * Time.deltaTime, Space.Self);
            }
        }

        /// <summary>
        /// Commands the drone to fly to a target 3D coordinate with smooth acceleration and braking.
        /// </summary>
        public void FlyTo(Vector3 targetPosition, Action onArrival = null)
        {
            StopCurrentFlight();
            activeFlightRoutine = StartCoroutine(SmoothFlightRoutine(targetPosition, onArrival));
        }

        /// <summary>
        /// Vertical ascent from base station to cruise altitude.
        /// </summary>
        public void Takeoff(Vector3 takeoffPoint, Action onComplete = null)
        {
            FlyTo(takeoffPoint, onComplete);
        }

        /// <summary>
        /// Controlled landing descent to base station pad.
        /// </summary>
        public void Land(Vector3 basePosition, Action onComplete = null)
        {
            FlyTo(basePosition, onComplete);
        }

        public void StopCurrentFlight()
        {
            if (activeFlightRoutine != null)
            {
                StopCoroutine(activeFlightRoutine);
                activeFlightRoutine = null;
            }
            isFlying = false;
            currentVelocity = Vector3.zero;
        }

        private IEnumerator SmoothFlightRoutine(Vector3 destination, Action onArrival)
        {
            isFlying = true;
            Vector3 startPos = transform.position;
            float totalDistance = Vector3.Distance(startPos, destination);
            float smoothTime = Mathf.Max(0.8f, totalDistance / flightSpeed);
            float elapsed = 0f;

            // Track horizontal yaw for bank angles
            Vector3 previousPosition = transform.position;

            while (Vector3.Distance(transform.position, destination) > arrivalThreshold)
            {
                elapsed += Time.deltaTime;

                // Smooth S-Curve (SmoothStep) easing for realistic quadcopter acceleration & braking
                float t = Mathf.Clamp01(elapsed / smoothTime);
                float easeFactor = Mathf.SmoothStep(0f, 1f, t);

                // SmoothDamp toward destination for natural inertia
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    destination,
                    ref currentVelocity,
                    0.45f,
                    flightSpeed,
                    Time.deltaTime
                );

                // Calculate instantaneous movement vector
                Vector3 moveDelta = transform.position - previousPosition;
                previousPosition = transform.position;
                Vector3 horizontalDelta = new Vector3(moveDelta.x, 0f, moveDelta.z);

                if (horizontalDelta.sqrMagnitude > 0.0001f)
                {
                    // Target yaw facing flight direction
                    Quaternion targetHeading = Quaternion.LookRotation(horizontalDelta.normalized, Vector3.up);

                    // Dynamic quadcopter pitch (forward tilt based on speed)
                    float currentHorizontalSpeed = horizontalDelta.magnitude / Time.deltaTime;
                    float speedRatio = Mathf.Clamp01(currentHorizontalSpeed / flightSpeed);
                    float targetPitch = speedRatio * maxPitchTilt;

                    // Dynamic quadcopter roll (bank into turns)
                    float targetRoll = 0f;
                    Vector3 cross = Vector3.Cross(transform.forward, horizontalDelta.normalized);
                    if (cross.sqrMagnitude > 0.001f)
                    {
                        targetRoll = -Mathf.Sign(cross.y) * Mathf.Clamp(cross.magnitude * maxRollBank, 0f, maxRollBank);
                    }

                    Quaternion tiltRotation = targetHeading * Quaternion.Euler(targetPitch, 0f, targetRoll);
                    transform.rotation = Quaternion.Slerp(transform.rotation, tiltRotation, turnSpeed * Time.deltaTime);
                }

                yield return null;
            }

            // Snapping to precise destination and leveling pitch/roll smoothly
            transform.position = destination;
            Quaternion levelRotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
            float levelTimer = 0f;

            while (levelTimer < 0.35f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, levelRotation, turnSpeed * 2.5f * Time.deltaTime);
                levelTimer += Time.deltaTime;
                yield return null;
            }

            transform.rotation = levelRotation;
            isFlying = false;
            currentVelocity = Vector3.zero;
            activeFlightRoutine = null;
            onArrival?.Invoke();
        }
    }
}
