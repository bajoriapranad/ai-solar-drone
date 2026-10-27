using System.Collections.Generic;
using UnityEngine;
using SolarDrone.Core;
using SolarDrone.Drone;
using SolarDrone.Environment;
using SolarDrone.Agent;
using SolarDrone.CameraControl;
using SolarDrone.UI;

namespace SolarDrone
{
    /// <summary>
    /// Autonomous Simulation Bootstrapper.
    /// Automatically generates the 3D environment (ground, 6 solar panels, base station, drone, cameras, and HUD)
    /// at runtime if not already built in the scene.
    /// This allows opening any fresh scene, adding this component, and hitting Play immediately!
    /// </summary>
    public class SimulationBootstrap : MonoBehaviour
    {
        [Header("Scene Generation Options")]
        [SerializeField] private bool autoBuildEnvironmentOnStart = true;
        [SerializeField] private int panelRows = 2;
        [SerializeField] private int panelCols = 3;
        [SerializeField] private float rowSpacing = 8.0f;
        [SerializeField] private float colSpacing = 6.0f;

        private void Awake()
        {
            if (autoBuildEnvironmentOnStart)
            {
                BuildCompleteSimulation();
            }
        }

        [ContextMenu("Build Simulation Setup")]
        public void BuildCompleteSimulation()
        {
            // 1. Setup Lighting & Ground
            SetupEnvironment();

            // 2. Setup Base Station
            BaseStation baseStation = CreateBaseStation();

            // 3. Setup Solar Farm (6 panels in rows)
            SolarFarmManager farmManager = CreateSolarFarm();

            // 4. Setup Drone
            GameObject droneObj = CreateDrone(baseStation.HomePosition);
            DroneMovement movement = droneObj.GetComponent<DroneMovement>();
            DroneBattery battery = droneObj.GetComponent<DroneBattery>();
            InspectionLogic inspectionLogic = droneObj.GetComponent<InspectionLogic>();
            DroneAgentFSM agentFSM = droneObj.GetComponent<DroneAgentFSM>();

            // Link agent dependencies
            var fsmFields = typeof(DroneAgentFSM).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            SetPrivateField(agentFSM, "movement", movement);
            SetPrivateField(agentFSM, "battery", battery);
            SetPrivateField(agentFSM, "farmManager", farmManager);
            SetPrivateField(agentFSM, "baseStation", baseStation);
            SetPrivateField(agentFSM, "inspectionLogic", inspectionLogic);

            // 5. Setup Cameras (Overview & Drone Gimbal)
            SetupCameras(droneObj.transform);

            // 6. Setup UI
            SetupUI(agentFSM);

            Debug.Log("<color=cyan>[BOOTSTRAP] 3D Solar Drone Inspection Simulation initialized successfully!</color>");
        }

        private void SetupEnvironment()
        {
            // Ground Plane
            if (GameObject.Find("Ground") == null)
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(8f, 1f, 8f);

                Renderer rend = ground.GetComponent<Renderer>();
                rend.material = new Material(Shader.Find("Standard"));
                rend.material.color = new Color(0.20f, 0.25f, 0.22f); // Earthy terrain
            }

            // Directional Light
            if (FindAnyObjectByType<Light>() == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Light sun = lightObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                sun.intensity = 1.1f;
                sun.color = new Color(1f, 0.98f, 0.90f);
            }
        }

        private BaseStation CreateBaseStation()
        {
            GameObject baseObj = GameObject.Find("BaseStation");
            if (baseObj != null) return baseObj.GetComponent<BaseStation>();

            baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "BaseStation";
            baseObj.transform.position = new Vector3(-8f, 0.1f, -6f);
            baseObj.transform.localScale = new Vector3(3f, 0.1f, 3f);

            Renderer rend = baseObj.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Standard"));
            rend.material.color = new Color(0.95f, 0.80f, 0.20f); // Bright yellow pad

            BaseStation station = baseObj.AddComponent<BaseStation>();
            return station;
        }

        private SolarFarmManager CreateSolarFarm()
        {
            GameObject farmObj = GameObject.Find("SolarFarm");
            if (farmObj != null) return farmObj.GetComponent<SolarFarmManager>();

            farmObj = new GameObject("SolarFarm");
            SolarFarmManager manager = farmObj.AddComponent<SolarFarmManager>();
            List<SolarPanel> panels = new List<SolarPanel>();

            int count = 1;
            for (int r = 0; r < panelRows; r++)
            {
                for (int c = 0; c < panelCols; c++)
                {
                    string pId = $"P{count}";
                    Vector3 pos = new Vector3(
                        (c - 1) * colSpacing + 2.0f,
                        0.5f,
                        r * rowSpacing - 2.0f
                    );

                    // Flag P2 as Thermal Hotspot and P5 as Physical Crack for anomaly reaction testing
                    bool hasDefect = (count == 2 || count == 5);
                    AnomalyType anomaly = count == 2 ? AnomalyType.ThermalHotspot : (count == 5 ? AnomalyType.PhysicalCrack : AnomalyType.None);

                    SolarPanel panel = CreatePanelGameObject(farmObj.transform, pId, pos, hasDefect, anomaly);
                    panels.Add(panel);
                    count++;
                }
            }

            manager.RegisterPanels(panels);
            return manager;
        }

        private SolarPanel CreatePanelGameObject(Transform parent, string id, Vector3 pos, bool hasAnomaly, AnomalyType anomaly)
        {
            // Panel Mount Structure
            GameObject mount = new GameObject($"SolarMount_{id}");
            mount.transform.SetParent(parent);
            mount.transform.position = pos;

            // Frame / Stanchion
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.name = "Stand";
            stand.transform.SetParent(mount.transform);
            stand.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            stand.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
            stand.GetComponent<Renderer>().material.color = Color.gray;

            // Solar Photovoltaic Surface (Tilted 30 degrees)
            GameObject panelSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panelSurface.name = $"Panel_{id}";
            panelSurface.transform.SetParent(mount.transform);
            panelSurface.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            panelSurface.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            panelSurface.transform.localScale = new Vector3(3.5f, 0.1f, 2.0f);

            Renderer rend = panelSurface.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Standard"));
            rend.material.color = new Color(0.12f, 0.22f, 0.45f); // Deep solar blue

            SolarPanel panelComponent = panelSurface.AddComponent<SolarPanel>();
            panelComponent.Setup(id, hasAnomaly, anomaly);

            return panelComponent;
        }

        private GameObject CreateDrone(Vector3 spawnPosition)
        {
            GameObject drone = GameObject.Find("DroneAgent");
            if (drone != null) return drone;

            drone = new GameObject("DroneAgent");
            drone.transform.position = spawnPosition;

            // Central fuselage
            GameObject fuselage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fuselage.name = "Fuselage";
            fuselage.transform.SetParent(drone.transform);
            fuselage.transform.localPosition = Vector3.zero;
            fuselage.transform.localScale = new Vector3(0.8f, 0.25f, 0.8f);

            Renderer fuseRend = fuselage.GetComponent<Renderer>();
            fuseRend.material = new Material(Shader.Find("Standard"));
            fuseRend.material.color = new Color(0.9f, 0.92f, 0.95f);

            // 4 Quadcopter Rotor Arms
            Vector3[] armOffsets = new Vector3[]
            {
                new Vector3(0.55f, 0.05f, 0.55f),
                new Vector3(-0.55f, 0.05f, 0.55f),
                new Vector3(0.55f, 0.05f, -0.55f),
                new Vector3(-0.55f, 0.05f, -0.55f)
            };

            for (int i = 0; i < armOffsets.Length; i++)
            {
                GameObject rotor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rotor.name = $"Rotor_{i + 1}";
                rotor.transform.SetParent(drone.transform);
                rotor.transform.localPosition = armOffsets[i];
                rotor.transform.localScale = new Vector3(0.35f, 0.04f, 0.35f);
                rotor.GetComponent<Renderer>().material.color = new Color(0.15f, 0.15f, 0.15f);

                // Attach dynamic propeller rotation
                var rotator = rotor.AddComponent<PropellerRotator>();
                rotator.Setup(i % 2 == 0);
            }

            // Directional Nose indicator (Cyan)
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nose.name = "NoseSensor";
            nose.transform.SetParent(drone.transform);
            nose.transform.localPosition = new Vector3(0f, 0.05f, 0.45f);
            nose.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            nose.GetComponent<Renderer>().material.color = Color.cyan;

            // Scanner Gimbal Downward Beam
            GameObject scannerObj = new GameObject("ScannerBeam");
            scannerObj.transform.SetParent(drone.transform);
            scannerObj.transform.localPosition = new Vector3(0f, -0.15f, 0.3f);
            scannerObj.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            scannerObj.AddComponent<DroneScannerBeam>();

            // Components
            drone.AddComponent<DroneMovement>();
            drone.AddComponent<DroneBattery>();
            drone.AddComponent<InspectionLogic>();
            drone.AddComponent<DroneAgentFSM>();

            return drone;
        }

        private void SetupCameras(Transform droneTransform)
        {
            // 1. Overview Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            mainCam.transform.position = new Vector3(0f, 15f, -16f);
            mainCam.transform.rotation = Quaternion.Euler(42f, 0f, 0f);

            // 2. Drone Gimbal Camera
            GameObject gimbalCamObj = GameObject.Find("DroneGimbalCamera");
            Camera gimbalCam;
            if (gimbalCamObj == null)
            {
                gimbalCamObj = new GameObject("DroneGimbalCamera");
                gimbalCam = gimbalCamObj.AddComponent<Camera>();
            }
            else
            {
                gimbalCam = gimbalCamObj.GetComponent<Camera>();
            }

            // Camera Manager
            GameObject camMgrObj = GameObject.Find("CameraManager");
            if (camMgrObj == null) camMgrObj = new GameObject("CameraManager");
            DroneCameraManager camMgr = camMgrObj.GetComponent<DroneCameraManager>() ?? camMgrObj.AddComponent<DroneCameraManager>();

            SetPrivateField(camMgr, "overviewCamera", mainCam);
            SetPrivateField(camMgr, "gimbalCamera", gimbalCam);
            SetPrivateField(camMgr, "droneTransform", droneTransform);
            camMgr.SetupCameras();
        }

        private void SetupUI(DroneAgentFSM agentFSM)
        {
            GameObject uiObj = GameObject.Find("UI_Manager");
            if (uiObj == null) uiObj = new GameObject("UI_Manager");

            TelemetryUI telemetry = uiObj.GetComponent<TelemetryUI>() ?? uiObj.AddComponent<TelemetryUI>();
            SetPrivateField(telemetry, "agentFSM", agentFSM);

            if (uiObj.GetComponent<DecisionLogUI>() == null)
            {
                uiObj.AddComponent<DecisionLogUI>();
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            field?.SetValue(target, value);
        }
    }
}
