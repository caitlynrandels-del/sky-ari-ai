const canvas = document.getElementById('meshCanvas');
const ctx = canvas.getContext('2d');
const homeostasisValue = document.getElementById('homeostasisValue');
const salienceValue = document.getElementById('salienceValue');
const rerouteValue = document.getElementById('rerouteValue');
const capacityValue = document.getElementById('capacityValue');
const eventFeed = document.getElementById('eventFeed');
const decisionText = document.getElementById('decisionText');
const traceList = document.getElementById('traceList');

const state = {
  nodes: [],
  edges: [],
  events: [],
  trace: [],
  reroutes: 0,
  homeostasis: 0.82,
  salience: 0.41,
  capacity: 0.62,
  time: 0,
  selectedNodeId: null,
};

function createNode(id, name, x, y) {
  return { id, name, x, y, health: 0.82, load: 0.32 };
}

function createEdge(from, to) {
  return { from, to, active: false, pulse: 0 };
}

function clamp(v, min, max) {
  return Math.max(min, Math.min(max, v));
}

function initMesh() {
  state.nodes = [
    createNode('supplier', 'Supplier Node', 120, 320),
    createNode('port', 'Port Hub', 320, 180),
    createNode('warehouse', 'Regional Warehouse', 520, 330),
    createNode('vessel', 'Cargo Vessel', 740, 180),
    createNode('retail', 'Retail Outlet', 900, 360),
    createNode('core', 'Cognitive Core', 650, 430),
  ];

  state.edges = [
    createEdge('supplier', 'port'),
    createEdge('supplier', 'warehouse'),
    createEdge('port', 'warehouse'),
    createEdge('port', 'vessel'),
    createEdge('warehouse', 'retail'),
    createEdge('vessel', 'retail'),
    createEdge('warehouse', 'core'),
    createEdge('core', 'retail'),
  ];

  state.events = [];
  state.trace = [];
  state.reroutes = 0;
  state.homeostasis = 0.82;
  state.salience = 0.41;
  state.capacity = 0.62;
  state.time = 0;

  pushEvent('Mesh booted. Perception loop online.');
  pushTrace('Core loop: ingest → score → decide → reconfigure → remember');
  updateMetrics();
}

function resizeCanvas() {
  const rect = canvas.getBoundingClientRect();
  const dpr = window.devicePixelRatio || 1;
  canvas.width = rect.width * dpr;
  canvas.height = rect.height * dpr;
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
}

function pushEvent(text) {
  state.events.unshift(text);
  state.events = state.events.slice(0, 6);
  eventFeed.innerHTML = '';
  state.events.forEach((entry) => {
    const li = document.createElement('li');
    li.textContent = entry;
    eventFeed.appendChild(li);
  });
}

function pushTrace(text) {
  state.trace.unshift(text);
  state.trace = state.trace.slice(0, 4);
  traceList.innerHTML = '';
  state.trace.forEach((entry) => {
    const li = document.createElement('li');
    li.textContent = entry;
    traceList.appendChild(li);
  });
}

function findNode(id) {
  return state.nodes.find((node) => node.id === id);
}

function updateMetrics() {
  homeostasisValue.textContent = state.homeostasis.toFixed(2);
  salienceValue.textContent = state.salience.toFixed(2);
  rerouteValue.textContent = String(state.reroutes);
  capacityValue.textContent = state.capacity.toFixed(2);
}

function applyScenario(type) {
  let targetNode;
  let severity;
  let incidentText;
  let decisionTextValue;
  let gateText;

  if (type === 'delay') {
    targetNode = findNode('port');
    severity = 0.72;
    incidentText = 'Port congestion detected. Cross-harbor latency rising.';
    decisionTextValue = 'Decision: reroute priority freight through the warehouse corridor and lower non-critical throughput.';
    gateText = 'Safety gate: preserve continuity while protecting the most time-sensitive flows.';
  } else if (type === 'surge') {
    targetNode = findNode('warehouse');
    severity = 0.64;
    incidentText = 'Demand surge detected at the regional warehouse.';
    decisionTextValue = 'Decision: activate adaptive buffers and redistribute excess load toward the retail corridor.';
    gateText = 'Safety gate: favor resilience over raw speed when strain exceeds available headroom.';
  } else {
    initMesh();
    decisionText.textContent = 'Loop reset. The mesh is observing fresh conditions.';
    pushEvent('Loop reset. Fresh state established.');
    return;
  }

  targetNode.health = clamp(targetNode.health - severity * 0.2, 0.25, 1);
  targetNode.load = clamp(targetNode.load + severity * 0.2, 0.2, 1);
  state.homeostasis = clamp(state.homeostasis - severity * 0.08 + 0.02, 0.2, 1);
  state.salience = clamp(0.35 + severity * 0.6 + (1 - targetNode.health) * 0.16, 0.2, 1);
  state.capacity = clamp(state.capacity - severity * 0.1 + 0.03, 0.2, 1);
  state.reroutes += 1;

  pushEvent(incidentText);
  pushTrace(`Salience scored at ${state.salience.toFixed(2)}.`);
  pushTrace(decisionTextValue);
  pushTrace(gateText);
  decisionText.textContent = `${decisionTextValue}\n${gateText}`;
  updateMetrics();
}

function drawEdges() {
  ctx.save();
  ctx.lineWidth = 2.2;
  state.edges.forEach((edge) => {
    const from = findNode(edge.from);
    const to = findNode(edge.to);
    const gradient = ctx.createLinearGradient(from.x, from.y, to.x, to.y);
    gradient.addColorStop(0, edge.active ? '#6be8ff' : 'rgba(112, 183, 255, 0.3)');
    gradient.addColorStop(1, 'rgba(141, 134, 255, 0.18)');
    ctx.strokeStyle = gradient;
    ctx.beginPath();
    ctx.moveTo(from.x, from.y);
    ctx.lineTo(to.x, to.y);
    ctx.stroke();

    const pulse = (Math.sin(state.time * 2.2 + edge.pulse) + 1) / 2;
    const px = from.x + (to.x - from.x) * pulse;
    const py = from.y + (to.y - from.y) * pulse;
    ctx.beginPath();
    ctx.arc(px, py, 3 + edge.active * 1.7, 0, Math.PI * 2);
    ctx.fillStyle = edge.active ? '#ffd66b' : '#6be8ff';
    ctx.fill();
  });
  ctx.restore();
}

function drawNodes() {
  state.nodes.forEach((node) => {
    const selected = state.selectedNodeId === node.id;
    const color = node.health < 0.55 ? 'rgba(255,111,145,0.28)' : 'rgba(107,232,255,0.24)';
    ctx.save();
    ctx.beginPath();
    ctx.arc(node.x, node.y, 30, 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.fill();
    ctx.lineWidth = selected ? 4 : 2;
    ctx.strokeStyle = selected ? '#ffffff' : 'rgba(255,255,255,0.28)';
    ctx.stroke();

    ctx.fillStyle = '#f9fbff';
    ctx.font = '13px Inter';
    ctx.textAlign = 'center';
    ctx.fillText(node.name, node.x, node.y + 48);

    ctx.fillStyle = node.health < 0.55 ? '#ff6f91' : '#8cecff';
    ctx.beginPath();
    ctx.arc(node.x, node.y, 15, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  });
}

function drawGrid() {
  ctx.save();
  ctx.strokeStyle = 'rgba(255,255,255,0.04)';
  ctx.lineWidth = 1;
  for (let x = 0; x <= canvas.clientWidth; x += 40) {
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.lineTo(x, canvas.clientHeight);
    ctx.stroke();
  }
  for (let y = 0; y <= canvas.clientHeight; y += 40) {
    ctx.beginPath();
    ctx.moveTo(0, y);
    ctx.lineTo(canvas.clientWidth, y);
    ctx.stroke();
  }
  ctx.restore();
}

function updateEdgeState() {
  const strained = state.nodes.filter((node) => node.health < 0.55);
  state.edges.forEach((edge) => {
    const impacted = strained.some((node) => node.id === edge.from || node.id === edge.to);
    edge.active = impacted;
    edge.pulse = state.time * 0.7;
  });
}

function animate() {
  ctx.clearRect(0, 0, canvas.clientWidth, canvas.clientHeight);
  drawGrid();
  drawEdges();
  drawNodes();
  state.time += 0.016;
  updateEdgeState();
  requestAnimationFrame(animate);
}

function bindActions() {
  document.querySelectorAll('button').forEach((button) => {
    button.addEventListener('click', () => {
      const action = button.dataset.action;
      if (action === 'delay') applyScenario('delay');
      if (action === 'surge') applyScenario('surge');
      if (action === 'reset') applyScenario('reset');
    });
  });

  canvas.addEventListener('click', (event) => {
    const rect = canvas.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    const hit = state.nodes.find((node) => Math.hypot(node.x - x, node.y - y) < 30);
    state.selectedNodeId = hit ? hit.id : null;
    if (hit) {
      pushEvent(`${hit.name} under observation.`);
    }
  });
}

window.addEventListener('resize', resizeCanvas);
window.addEventListener('load', () => {
  resizeCanvas();
  initMesh();
  bindActions();
  animate();
});
