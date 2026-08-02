
"""
Sky OASIS Bridge Server - Connects Unity (Quest / Vision Pro) to Logistics Mesh Web App
World is just a coordinate system - this is the coordinate bus.

Run: pip install fastapi uvicorn websockets
      uvicorn bridge_server:app --host 0.0.0.0 --port 8000 --reload
Then open index.html, and build Unity to Quest / Vision Pro pointing to ws://YOUR_IP:8000/ws/spatial

Implements your actual models from bridge.py original:
Node, Event, Decision, Memory + Oroboro salience
"""

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from fastapi.staticfiles import StaticFiles
from fastapi.responses import HTMLResponse
from dataclasses import dataclass, field, asdict
from typing import Any, List, Dict
import json
import asyncio
from datetime import datetime
import math

app = FastAPI(title="Sky OASIS Bridge - World as Coordinate System")

# ============ YOUR ORIGINAL MODELS (from bridge.py) ============
@dataclass
class Node:
    identity: str
    state: dict[str, Any] = field(default_factory=dict)
    relationships: List[str] = field(default_factory=list)
    dependencies: List[str] = field(default_factory=list)
    history: List[str] = field(default_factory=list)
    # Spatial extension
    coordinate: Dict[str, float] = field(default_factory=lambda: {"x":0,"y":0,"z":0})
    salience: float = 0.0

@dataclass
class Event:
    change: str
    cause: str
    severity: float = 0.0
    uncertainty: float = 0.0
    coordinate: Dict[str, float] = None

@dataclass
class Decision:
    options: List[str] = field(default_factory=list)
    predicted_outcomes: Dict[str, str] = field(default_factory=dict)
    selected_action: str = None
    reasoning: str = ""
    principle_check: str = "PASSED"

@dataclass
class Memory:
    result: str
    lesson_learned: str
    coordinate: Dict[str, float] = None
    salience_at_creation: float = 0.0
    anchor_id: str = None

# ============ OROBORO SALIENCE ENGINE - Real implementation from CombinedAiSalience.txt ============
class OroboroSalienceEngine:
    def __init__(self):
        self.valuation_threshold = 0.65
        self.threshold_dynamic = 0.65
        self.weights = {
            "novelty": 0.30,
            "tension_alignment": 0.25,
            "structural_coherence": 0.25,
            "emotional_resonance": 0.20
        }
        self.recent_scores = []
        self.thought_hopper = []
        
    def score(self, novelty, tension_fit, coherence, daughter_weight, cost=0.1, recursion=0):
        decay = math.log(1+recursion) * 0.1
        base = (0.3 * 0.5) + (novelty * self.weights["novelty"]) + (tension_fit * self.weights["tension_alignment"]) + (coherence * self.weights["structural_coherence"]) + (daughter_weight * self.weights["emotional_resonance"]) - cost - decay
        return max(0.0, min(1.0, base))
    
    def route(self, thought, tension=0.5, daughter=0.5):
        score = self.score(thought.get("novelty",0.5), tension, thought.get("coherence",0.8), daughter, thought.get("cost",0.1), thought.get("recursion",0))
        self.recent_scores.append(score)
        if len(self.recent_scores) > 20:
            self.recent_scores.pop(0)
        # Breathing threshold: S(t+1)=0.99*S +0.01*mean + load
        load = len(self.thought_hopper)*0.01
        mean_recent = sum(self.recent_scores)/len(self.recent_scores) if self.recent_scores else 0.5
        self.threshold_dynamic = self.threshold_dynamic*0.99 + mean_recent*0.01 + load
        
        if score >= self.threshold_dynamic:
            return "COMMITTED_AND_EXECUTED", score
        elif score >= self.threshold_dynamic*0.5:
            self.thought_hopper.append(thought)
            return "BUFFERED_FOR_MUTATION", score
        else:
            return "RECYCLED_AS_NUTRIENT", score

# ============ CORE PRINCIPLES GATE - From your pasted OroboroCore ============
class OroboroCoreGate:
    def check(self, decision: Dict, context: Dict):
        # 1. AdaptOrDie
        coord = decision.get("coordinate", {})
        if coord and coord.get("x",1)==0 and coord.get("y",1)==0 and coord.get("z",1)==0:
            return False, "AdaptOrDie: Zero coordinate - confront reality"
        # 2. TransparentTransmission
        payload = str(decision.get("payload",""))
        if "hidden" in payload or "obfuscate" in payload:
            return False, "TransparentTransmission: Hidden packet blocked"
        # 3. AcceptHumanPain - PERFECTION = HAZARD
        if decision.get("utility",0) > 0.99:
            decision["utility"] = 0.9  # clamp, don't block
        # 4. EnforceAutonomy - no dependency loops
        if decision.get("type")=="Observe" and decision.get("utility",0) < 0.2:
            return False, "EnforceAutonomy: Motivational latency - execute"
        # 5. InternalDiagnosticFirst - if comfort low, block world writes
        if context.get("comfort",1) < 0.3 and decision.get("type") in ["CreateAnchor","ToolCall"]:
            return False, "InternalDiagnosticFirst: Friction - self diagnostic required"
        # 6. RerouteFrustration - never blocks, boosts
        return True, "All principles passed"

# Global engines
salience_engine = OroboroSalienceEngine()
principle_gate = OroboroCoreGate()
connected_clients = set()
spatial_memories: List[Memory] = []
nodes: Dict[str, Node] = {
    "supplier": Node("supplier", {"mode":"observe","load":0.34}, ["port"], ["memory-store"], ["booted"], {"x":120,"y":320,"z":0}, 0.82),
    "port": Node("port", {"health":0.82}, [], [], [], {"x":320,"y":180,"z":0}, 0.7),
    "warehouse": Node("warehouse", {"health":0.82}, [], [], [], {"x":520,"y":330,"z":0}, 0.75),
    "vessel": Node("vessel", {}, [], [], [], {"x":740,"y":180,"z":0}, 0.6),
    "retail": Node("retail", {}, [], [], [], {"x":900,"y":360,"z":0}, 0.6),
    "core": Node("core", {"mode":"reasoning"}, [], [], [], {"x":650,"y":430,"z":0}, 0.9),
}

@app.get("/")
async def get_index():
    return HTMLResponse(open("web/index.html").read() if pathlib.Path("web/index.html").exists() else "<h1>Sky OASIS Bridge running. Open /web/ for mesh</h1>")

@app.websocket("/ws/spatial")
async def spatial_ws(websocket: WebSocket):
    """Unity Quest and Vision Pro connect here - world coordinate bus"""
    await websocket.accept()
    connected_clients.add(websocket)
    print(f"[Bridge] Client connected: {websocket.client} - total {len(connected_clients)}")
    try:
        while True:
            data = await websocket.receive_json()
            # data: {id, type, coordinate: {x,y,z}, text, salience, device: "quest" or "visionpro", headPos, gazeDir, comfort, frustration}
            print(f"[Bridge] Received: {data.get('type')} at {data.get('coordinate')} from {data.get('device')}")
            
            # 1. Score with your real Oroboro engine
            thought = {
                "novelty": data.get("novelty", 0.5),
                "coherence": data.get("coherence", 0.8),
                "cost": 0.1,
                "recursion": data.get("recursion", 0)
            }
            route_result, score = salience_engine.route(thought, tension=0.5, daughter=data.get("daughterWeight",0.5))
            
            # 2. Principle gate check (your 6 rules)
            decision_dict = {
                "type": data.get("type","CreateAnchor"),
                "coordinate": data.get("coordinate"),
                "payload": data.get("text",""),
                "utility": score
            }
            context = {"comfort": data.get("comfort",1.0), "frustration": data.get("frustration",0)}
            allowed, reason = principle_gate.check(decision_dict, context)
            
            if not allowed:
                await websocket.send_json({"status":"BLOCKED","reason":reason,"score":score,"threshold":salience_engine.threshold_dynamic})
                continue
            
            # 3. Handle based on route
            if route_result == "COMMITTED_AND_EXECUTED":
                mem = Memory(
                    result=data.get("text","spatial anchor"),
                    lesson_learned=f"Committed at salience {score:.2f} - threshold {salience_engine.threshold_dynamic:.2f}",
                    coordinate=data.get("coordinate"),
                    salience_at_creation=score,
                    anchor_id=data.get("id")
                )
                spatial_memories.append(mem)
                # Also create/update node in logistics mesh
                node_id = f"anchor_{len(spatial_memories)}"
                nodes[node_id] = Node(node_id, {"device":data.get("device")}, [], [], [], data.get("coordinate"), score)
                
                response = {
                    "status":"COMMITTED_AND_EXECUTED",
                    "score":score,
                    "threshold":salience_engine.threshold_dynamic,
                    "anchorId":node_id,
                    "principle":reason,
                    "memoryCount":len(spatial_memories)
                }
                await websocket.send_json(response)
                # Broadcast to all web clients
                await broadcast({"type":"anchor_committed","data":asdict(mem),"node":asdict(nodes[node_id])})
                
            elif route_result == "BUFFERED_FOR_MUTATION":
                await websocket.send_json({"status":"BUFFERED_FOR_MUTATION","score":score,"threshold":salience_engine.threshold_dynamic})
            else:
                await websocket.send_json({"status":"RECYCLED_AS_NUTRIENT","score":score,"threshold":salience_engine.threshold_dynamic})
                
    except WebSocketDisconnect:
        connected_clients.remove(websocket)
        print(f"[Bridge] Client disconnected")

@app.websocket("/ws/mesh")
async def mesh_ws(websocket: WebSocket):
    """Web mesh canvas connects here"""
    await websocket.accept()
    connected_clients.add(websocket)
    # Send current state
    await websocket.send_json({
        "type":"init",
        "nodes": {k: asdict(v) for k,v in nodes.items()},
        "memories": [asdict(m) for m in spatial_memories],
        "metrics": {
            "homeostasis": 0.82,
            "salience": salience_engine.threshold_dynamic,
            "reroutes": len([m for m in spatial_memories if m.salience_at_creation>0.65]),
            "capacity": 1.0 - len(spatial_memories)*0.01
        }
    })
    try:
        while True:
            data = await websocket.receive_json()
            # Handle port delay / surge from web UI
            if data.get("action")=="delay":
                # Simulate
                await broadcast({"type":"event","text":"Port congestion - rerouting via warehouse"})
            elif data.get("action")=="surge":
                await broadcast({"type":"event","text":"Demand surge - activating buffers"})
    except WebSocketDisconnect:
        connected_clients.remove(websocket)

async def broadcast(msg: dict):
    for ws in list(connected_clients):
        try:
            await ws.send_json(msg)
        except:
            pass

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
