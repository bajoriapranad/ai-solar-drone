# Autonomous Solar Farm Inspection Drone (AI Agent)

An autonomous solar farm inspection drone simulation built in Unity. The drone is controlled by an **Autonomous Goal-Based Agent with a Hybrid (FSM + Heuristic) Architecture** designed to autonomously navigate, inspect solar panels, detect defects, and manage real-time energy budgets for fail-safe operations.

---

## Architecture Overview

The drone agent operates as a self-governing intelligent system:

- **Agent Type:** Autonomous Goal-Based Agent
- **Decision Engine:** Finite State Machine (FSM) + Spatial Path Heuristics
- **Sensory & Inspection Logic:** Multi-stage optical and thermal anomaly scanning (detecting Thermal Hotspots, Physical Cracks, and Heavy Soiling)
- **Safety Subsystem:** Real-time dynamic return-to-base (RTB) energy budgeting preventing field battery depletion

### Mission Lifecycle (FSM)
```
IdleAtBase -> TakingOff -> NavigatingToPanel -> PrimaryInspection -> [SecondaryCloseUpInspection] -> ReturningToBase -> Landing -> MissionComplete
```

---

## Key Features

1. **Autonomous Mission Control:** Full lifecycle from automated takeoff to precision landing at the base station pad.
2. **Nearest-Neighbor Path Optimization:** Dynamically prioritizes the nearest uninspected panel to minimize flight distance and energy expenditure.
3. **Adaptive Two-Stage Inspection:** Performs a primary survey scan; upon detecting a defect, immediately adapts and maneuvers into a close-up orbital scan for high-resolution diagnostic verification.
4. **Dynamic Energy-Aware Budgeting:**
   $$\text{Energy Required} = \left(\frac{\text{Distance to Base}}{\text{Flight Speed}}\right) \times \text{Discharge Rate} + \text{Safety Buffer}$$
   Guarantees that the drone will abort to base whenever energy hits the threshold required to return home.
5. **Decoupled Event Architecture:** Uses a centralized `DroneEventBus` to stream flight telemetry, scanning events, and AI decision logs to the HUD/UI.

---

## Project Structure

```
Assets/_Project/
├── Scripts/
│   ├── Agent/           # DroneAgentFSM.cs, InspectionLogic.cs
│   ├── Camera/          # Multi-camera manager (Chase, Orbit, Scanner view)
│   ├── Core/            # DroneEventBus.cs, Enums.cs
│   ├── Drone/           # DroneMovement.cs, DroneBattery.cs, DroneScannerBeam.cs
│   ├── Editor/          # SceneSetupWizard.cs
│   ├── Environment/     # SolarFarmManager.cs, SolarPanel.cs, BaseStation.cs
│   └── UI/              # TelemetryHUD.cs, EventLogUI.cs
```

---

## Setup & Running in Unity

1. Open the project in **Unity 6 (6000.x)** or compatible Unity Editor version.
2. Open the main simulation scene (`Assets/_Project/Scenes/SolarFarmInspection.unity` or generated via the Scene Setup Wizard).
3. Press **Play** in the Unity Editor.
4. Press **Spacebar** (or enable auto-start) to launch the autonomous mission.
