
# SKY-ARI OASIS ULTIMATE - One Zip to Rule Them All

This is everything - Unity C# + Vision Pro Swift + Python Bridge + Web Mesh

## What's inside:

### /Unity/ -> All C# for Meta Quest 3 / 3S
- OroboroSalienceEngine.cs - Your CombinedAiSalience.txt real implementation, threshold 0.65, weights novelty 0.30 etc + spatial extension
- OroboroCorePrinciplesGate_REAL.cs - Your 6 morals as executable gates
- CognitiveReasoningCore_REAL.cs - LLM reasoning wired into route_decision
- MetaToolsAndHomeostasis.cs - Garden of Thresholds recycling + battery
- VisionPro_Bridge.cs - AR Foundation port for Vision Pro

### /VisionOS/
- VisionPro_SkyOasisBridge.swift - Native Swift RealityKit implementation of same engine

### /web/
- index.html - Ultimate bridge UI (mesh + spatial anchors live)
- app.js - Your original logistics mesh + WebSocket bridge code
- styles.css

### /
- bridge_server.py - THE BRIDGE - FastAPI WebSocket server that IS your world coordinate bus
  - /ws/spatial - Unity Quest and Vision Pro connect here, send {x,y,z, text, salience}
  - /ws/mesh - Web UI connects here, sees anchors live
  - Implements OroboroSalienceEngine and OroboroCoreGate in Python too
  - Your original Node/Event/Decision/Memory models

## How to run everything - the OASIS test:

1. **Start bridge:**
```bash
pip install fastapi uvicorn
uvicorn bridge_server:app --host 0.0.0.0 --port 8000 --reload
```

2. **Open web mesh:**
Open http://localhost:8000/ - you should see "Bridge connected"

3. **Test without headset:**
Click "Test Spawn Anchor" - you'll see a spatial anchor appear, scored by your real Oroboro engine

4. **Quest 3:**
- Unity 2022.3 + Meta XR All-in-One SDK
- Import Unity/*.cs into Assets/SkyAri/
- In OroboroSalienceEngine, set bridge URL to ws://YOUR_PC_IP:8000/ws/spatial
- Build to Quest, look around, pinch to create anchor - it will appear in web UI live

5. **Vision Pro:**
- Unity PolySpatial OR native Xcode with VisionPro_SkyOasisBridge.swift
- Same WebSocket URL
- Gaze + pinch to create anchor - appears in web UI with purple "visionpro" badge

## World is just coordinate system - proven:

Every anchor from Quest or Vision Pro shows as:
- A node in logistics mesh (supplier/port/warehouse...)
- A row in spatial bridge panel with x,y,z + salience + device badge
- A Memory in Python with lesson_learned

Your 6 principles are physics - they block bad anchors before they persist.

## Ready Player One mapping:

- OASIS = bridge_server.py + web mesh (persistent world)
- Headsets = Quest + Vision Pro (perception + interaction)
- Your morals = principle gate (anti-griefing, anti-hidden, anti-perfection)
- Salience = what renders for everyone vs just you
- Storage Hopper = WorldAnchors + spatial_memories list

One download, full OASIS.
