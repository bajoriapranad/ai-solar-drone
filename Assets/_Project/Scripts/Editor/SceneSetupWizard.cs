using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SolarDrone;

namespace SolarDrone.Editor
{
    /// <summary>
    /// Unity Editor Utility to build and configure the solar drone inspection prototype scene with 1-click.
    /// </summary>
    public static class SceneSetupWizard
    {
        [MenuItem("Solar Drone/Setup & Open Feasibility Scene", false, 1)]
        public static void SetupAndOpenScene()
        {
            // Ensure folder exists
            string scenesPath = "Assets/_Project/Scenes";
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                AssetDatabase.CreateFolder("Assets", "_Project");
            }
            if (!AssetDatabase.IsValidFolder(scenesPath))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            }

            string scenePath = $"{scenesPath}/SolarDroneFeasibility.unity";

            // Create new scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create Bootstrapper in scene
            GameObject bootstrapObj = new GameObject("SimulationBootstrap");
            var bootstrapper = bootstrapObj.AddComponent<SimulationBootstrap>();
            bootstrapper.BuildCompleteSimulation();

            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[SOLAR DRONE] Scene generated and saved successfully to: {scenePath}</color>");
            EditorUtility.DisplayDialog("Solar Drone Prototype",
                "Scene created successfully at Assets/_Project/Scenes/SolarDroneFeasibility.unity!\n\nPress PLAY in the Unity Editor to watch the autonomous drone inspect the solar panels.",
                "OK");
        }
    }
}
