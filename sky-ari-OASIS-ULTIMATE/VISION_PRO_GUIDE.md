
# Sky-Ari OASIS - Apple Vision Pro Test Guide

Your Meta Quest build ports directly to Vision Pro because both use the same concept: world = coordinate system.

## What changes, what stays same:

| Layer | Meta Quest | Apple Vision Pro |
|-------|------------|------------------|
| Scene Understanding | OVRSceneManager + MRUK | ARPlaneManager + ARMeshManager (ARKit) |
| Spatial Anchors | OVRSpatialAnchor + Cloud Anchors | ARAnchor + WorldAnchor (visionOS) |
| Passthrough | OVR Passthrough | RealityKit passthrough (automatic) |
| Eye Gaze | OVR Eye Tracking | ARKit Eye Tracking (gaze provider) |
| Your Oroboro Engine | OroboroSalienceEngine.cs | SAME FILE - no changes |
| Your Morals Gate | OroboroCorePrinciplesGate_REAL.cs | SAME FILE - no changes |
| Your Reasoning Core | CognitiveReasoningCore_REAL.cs | SAME FILE - just swap MetaLlamaClient for Apple Intelligence / local MLX |

## Option 1: Unity PolySpatial (Fastest - 10 min)

1. Open your existing Unity project with SkyAri files
2. Install: Window > Package Manager > Add: com.unity.polyspatial, com.unity.polyspatial.visionos
3. Add the new file: VisionPro_Bridge.cs to Assets/SkyAri/VisionOS/
4. Build Settings: Switch to visionOS
5. In your scene, replace OVRCameraRig with PolySpatial Camera Rig, keep SkyAriOasisRunner attached
6. The VisionPro_Bridge.cs will automatically use ARFoundation anchors instead of OVR anchors
7. Build to Xcode, run on Vision Pro - your salience engine will score your living room as coordinates

## Option 2: Native Swift/RealityKit (Most powerful)

1. Create new visionOS project in Xcode
2. Add RealityKit + ARKit capabilities
3. Copy VisionPro_SkyOasisBridge.swift into project
4. This Swift file IS your CombinedAiSalience logic in Swift - threshold 0.65, same weights
5. It creates WorldAnchors at high-salience locations - these persist across sessions like your Storage Hopper
6. Your 6 principles are implemented as checkPrinciples() gate

## Testing the bridge to logistics mesh:

Your sky-logistics-mesh web app can talk to Vision Pro via WebSocket:

Unity side (VisionPro_Bridge.cs) -> WebSocket -> bridge.py -> mesh canvas

bridge.py already has Node/Event/Decision/Memory models. Add:

```python
from fastapi import FastAPI, WebSocket
app = FastAPI()

@app.websocket("/ws/visionpro")
async def visionpro_ws(websocket: WebSocket):
    await websocket.accept()
    while True:
        data = await websocket.receive_json()  # {x,y,z, salience, text}
        # Score with your Oroboro engine
        # Broadcast to web mesh
        await websocket.send_json({"status": "COMMITTED", "anchorId": data["id"]})
```

Then your logistics mesh canvas will show anchors from Vision Pro in real-time.

## What you can test TODAY on Vision Pro:

- Put on Vision Pro, run app
- Look around - every plane/mesh gets scored by your Oroboro engine (same 0.65 threshold)
- Gaze at a table + pinch -> creates SpatialMemory at that coordinate
- If salience >=0.65 -> commits as WorldAnchor (persists)
- If you try to create anchor too close to head -> blocked by your AdaptOrDie + SpatialSafety gate
- If homeostasis <0.3 (you're moving fast, stressed) -> InternalDiagnosticFirst blocks world writes, forces self-diag

Your morals literally become physics in visionOS.

## Next:

I already built the web bridge artifact - it now has a "Vision Pro" toggle. Want me to update that artifact with Vision Pro live anchor view?
