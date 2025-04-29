# Influx

**A Procedural Multi-Agent Simulation Game in Unity**

Watch the Demo
(https://img.youtube.com/vi/tKH-TE7bzyk&t=4s/0.jpg)](https://www.youtube.com/watch?v=tKH-TE7bzyk&t=4s)  
Play on itch.io
(https://milesjmaiden.itch.io/influx)

---

## 🚀 Project Overview

Influx is a top-down, orthographic Unity simulation in which autonomous Agents maintain critical systems while hostile Aliens attempt to sabotage them. Procedurally generated rooms, emergent multi-agent behaviors, and dynamic sabotage threats combine to create endlessly replayable “dungeon-style” environments.

Key systems:
- **Data-Driven Level Generation**  
  Rooms and corridors laid out by a frontier-based algorithm, with NavMesh linking for seamless navigation.
- **Autonomous Agents & FSM**  
  Agents continuously repair Computers, refuel Bins, chase and trap Aliens—yet can be interrupted by player commands.
- **Hostile Alien AI**  
  Four-state FSM (Seek, Attack, Flee, Wander) drives emergent sabotage gameplay.
- **Interactable Objects**  
  Computers, Bins, RockBins, Tables, Containers—all managed via a unified `Interactable` base class and coroutines.
- **Robust UI & Controls**  
  Global HUD aggregates system health, unified pause/menu system, and an orthographic camera with panning, zoom, tilt/yaw.

---

## 📦 Getting Started

### Prerequisites

- **Unity Editor 2022.3.56f1** (LTS)  
- **Universal Render Pipeline** package installed

### Installation

1. **Clone the repo**  
   ```bash
   git clone https://github.com/YourUsername/Influx.git
   cd Influx
Open in Unity
Launch Unity Hub, click Add, navigate to the cloned folder, and open the project.

Run the Demo Scene
In the Project window, open Assets/Scenes/MainMenu.unity and press ▶️ Play.

🎮 Controls

Action	Input
Pan Camera	W / A / S / D or Arrow keys

Relative Pan (camera-oriented)	Hold Right Mouse Button + WASD

Zoom In/Out	Mouse Wheel

Tilt/Yaw Camera	Hold Right Mouse Button + Move Mouse

Center & Zoom to Level	Press F

Pause/Unpause & Toggle Menu	Press Esc

Select Agent	Click on an Agent

Command Agent (Repair / Refuel / Trap)	Click on Interactable while Agent selected

📐 Architecture & Key Classes

LevelGenerator: Procedural room layout, geometry, NavMesh baking.

LevelDesignSettings: ScriptableObject exposing room shape, size, and spawn distributions.

Interactable.cs: Base class for all clickable objects (Computers, Bins, Tables, etc.).

Agent.cs / AgentManager.cs: Multi-priority finite-state machine driving Agent behavior.

AlienController.cs: Four-state FSM for hostile Alien behaviors.

UIManager.cs: Global HUD aggregation, pause menu, game-over screen.

CameraManager.cs: Orthographic camera rig with pan/zoom/tilt & level focus.

