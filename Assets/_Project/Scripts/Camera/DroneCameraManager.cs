using UnityEngine;
using SolarDrone.Core;

namespace SolarDrone.CameraControl
{
    /// <summary>
    /// Coordinates the tactical solar farm overview camera and the drone gimbal inspection camera.
    /// Supports Picture-in-Picture (PiP) or view toggling via keyboard/UI.
    /// </summary>
    public class DroneCameraManager : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private Camera overviewCamera;
        [SerializeField] private Camera gimbalCamera;

        [Header("Gimbal Settings")]
        [SerializeField] private Transform droneTransform;
        [SerializeField] private Vector3 gimbalOffset = new Vector3(0f, -0.2f, 0.3f);
        [SerializeField] private float gimbalPitchAngle = 45.0f; // Look down at 45 degrees

        [Header("Picture-in-Picture (PiP)")]
        [SerializeField] private bool enablePiP = true;
        [SerializeField] private Rect pipViewportRect = new Rect(0.74f, 0.04f, 0.24f, 0.26f);

        [Header("Thermal IR Mode")]
        [SerializeField] private bool isThermalMode = false;

        private bool isGimbalPrimary = false;
        private Texture2D thermalBarTexture;

        public bool IsThermalMode => isThermalMode;

        private void Start()
        {
            SetupCameras();
            CreateThermalGradientTexture();
        }

        private void LateUpdate()
        {
            // Position the gimbal camera smoothly on the drone
            if (droneTransform != null && gimbalCamera != null)
            {
                gimbalCamera.transform.position = droneTransform.position + droneTransform.TransformDirection(gimbalOffset);
                gimbalCamera.transform.rotation = droneTransform.rotation * Quaternion.Euler(gimbalPitchAngle, 0f, 0f);
            }

            // Keyboard shortcut 'C' to toggle primary view
            if (Input.GetKeyDown(KeyCode.C))
            {
                TogglePrimaryCamera();
            }

            // Keyboard shortcut 'T' to toggle simulated Thermal / FLIR mode
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleThermalMode();
            }
        }

        public void SetupCameras()
        {
            if (overviewCamera == null)
            {
                overviewCamera = Camera.main;
            }

            if (gimbalCamera != null)
            {
                if (enablePiP)
                {
                    gimbalCamera.enabled = true;
                    gimbalCamera.depth = 1; // Render on top of overview camera
                    gimbalCamera.rect = pipViewportRect;
                }
                else
                {
                    gimbalCamera.enabled = false;
                    gimbalCamera.rect = new Rect(0, 0, 1, 1);
                }
            }

            if (overviewCamera != null)
            {
                overviewCamera.depth = 0;
                overviewCamera.rect = new Rect(0, 0, 1, 1);
            }
        }

        public void TogglePrimaryCamera()
        {
            isGimbalPrimary = !isGimbalPrimary;

            if (overviewCamera == null || gimbalCamera == null) return;

            if (isGimbalPrimary)
            {
                // Gimbal is main fullscreen, overview is PiP
                overviewCamera.depth = 1;
                overviewCamera.rect = pipViewportRect;

                gimbalCamera.depth = 0;
                gimbalCamera.rect = new Rect(0, 0, 1, 1);
                DroneEventBus.PublishDecisionLogged("[CAMERA] Switched primary view to Drone Gimbal Camera.", LogSeverity.Info);
            }
            else
            {
                // Overview is main fullscreen, gimbal is PiP
                overviewCamera.depth = 0;
                overviewCamera.rect = new Rect(0, 0, 1, 1);

                gimbalCamera.depth = 1;
                gimbalCamera.rect = pipViewportRect;
                DroneEventBus.PublishDecisionLogged("[CAMERA] Switched primary view to Tactical Overview Camera.", LogSeverity.Info);
            }
        }

        public void ToggleThermalMode()
        {
            isThermalMode = !isThermalMode;

            if (gimbalCamera != null)
            {
                if (isThermalMode)
                {
                    gimbalCamera.backgroundColor = new Color(0.02f, 0.02f, 0.12f);
                    DroneEventBus.PublishDecisionLogged("[CAMERA] FLIR Radiometric Thermal IR mode ENABLED.", LogSeverity.Warning);
                }
                else
                {
                    gimbalCamera.backgroundColor = new Color(0.19f, 0.30f, 0.47f);
                    DroneEventBus.PublishDecisionLogged("[CAMERA] Standard Optical RGB mode restored.", LogSeverity.Info);
                }
            }
        }

        private void CreateThermalGradientTexture()
        {
            thermalBarTexture = new Texture2D(128, 1);
            for (int i = 0; i < 128; i++)
            {
                float t = i / 127.0f;
                // Ironbow palette: Black -> Blue -> Purple -> Orange -> Yellow -> White
                Color col = Color.Lerp(Color.blue, Color.magenta, t * 2f);
                if (t > 0.5f)
                {
                    col = Color.Lerp(Color.red, Color.yellow, (t - 0.5f) * 2f);
                }
                thermalBarTexture.SetPixel(i, 0, col);
            }
            thermalBarTexture.Apply();
        }

        private void OnGUI()
        {
            if (!isThermalMode) return;

            // Thermal HUD overlay banner
            Rect rect = isGimbalPrimary 
                ? new Rect(Screen.width - 240, 40, 220, 50) 
                : new Rect(Screen.width * pipViewportRect.x + 10, Screen.height * (1f - pipViewportRect.y - pipViewportRect.height) + 10, 200, 45);

            GUI.color = new Color(1f, 0.4f, 0.1f, 0.9f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(rect.x + 8, rect.y + 4, rect.width - 16, 20), "● FLIR IR THERMAL [30Hz]");
            if (thermalBarTexture != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 8, rect.y + 24, rect.width - 16, 12), thermalBarTexture);
            }
        }
    }
}
