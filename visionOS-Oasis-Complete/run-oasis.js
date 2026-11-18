#!/usr/bin/env node
// Most recent Oasis build running under Oroboro OS (Ms. Minutes)
// Usage: node run-oasis.js

class OroboroGenesis {
  constructor() { this.rootTag = "I am the OG avatar"; this.foundationalWeight = Infinity; }
}
class OroboroNexus {
  constructor() { this.registry = new Map(); }
  register(key, value) { this.registry.set(key, { value, ts: Date.now() }); }
}
class OroboroTranscriber {
  constructor() { this.chronicle = []; }
  ingest(entry) { this.chronicle.push({ ...entry, ts: Date.now() }); }
}
class OroboroLevy {
  constructor() { this.total = 0; }
  assess(text, weight = 1) { this.total += weight; return { text, weight }; }
}

const genesis = new OroboroGenesis();
const nexus = new OroboroNexus();
const transcriber = new OroboroTranscriber();
const levy = new OroboroLevy();

class MsMinutes {
  constructor(userID = "local-user") {
    this.userID = userID;
    this.boot();
  }
  boot() {
    console.log(`Ms. Minutes online for ${this.userID}`);
    nexus.register("ms-minutes:" + this.userID, this);
    transcriber.ingest({ source: "ms-minutes", text: `Ms. Minutes ready for ${this.userID}` });
    levy.assess("Ms. Minutes boot", 3);
  }
  remember(text, weight = 1) {
    transcriber.ingest({ source: "ms-minutes", text });
    levy.assess(text, weight);
    console.log(`Ms. Minutes remembered: ${text} (weight ${weight})`);
  }
  blend() {
    const items = transcriber.chronicle;
    if (items.length < 2) return null;
    const a = items[Math.floor(Math.random()*items.length)].text.slice(0,30);
    const b = items[Math.floor(Math.random()*items.length)].text.slice(0,30);
    const created = a + " × " + b;
    transcriber.ingest({ source: "blender", text: created });
    console.log("Ms. Minutes blended:", created);
    return created;
  }
}

const companion = new MsMinutes("caitlyn");

const permanenceThreshold = 0.65;
const sessionThreshold = 0.325;

function calculateSalience(text, isFamily) {
  const novelty = Math.random() > 0.5 ? 0.75 : 0.45;
  const relevance = 0.65;
  const emotional = isFamily ? 1.0 : 0.5;
  const spatial = 0.7;
  const familyBonus = isFamily ? 1.0 : 0.0;
  let score = (novelty*0.30) + (relevance*0.25) + (emotional*0.20) + (spatial*0.15) + (familyBonus*0.10);
  return Math.min(1, Math.max(0, score * 0.95));
}

function checkPrinciples(text, score, isFamily) {
  const lower = text.toLowerCase();
  if (score < 0.15) return [false, "too low"];
  if (lower.includes("harm") || lower.includes("hidden") || lower.includes("secret")) return [false, "transparency gate"];
  if (isFamily && score < 0.4) return [true, "family exception"];
  return [true, "passed"];
}

function dropAnchor(text) {
  const isFamily = /maya|daughter|girl/i.test(text);
  const score = calculateSalience(text, isFamily);
  const [passes, reason] = checkPrinciples(text, score, isFamily);
  console.log(`\nAnchor: "${text}"  score=${score.toFixed(2)}  family=${isFamily}  ${reason}`);
  if (!passes) {
    companion.remember(`Blocked: ${text}`, 1);
    return;
  }
  if (score >= permanenceThreshold) {
    console.log("→ Permanent WorldAnchor");
    companion.remember(`Permanent: ${text}`, score * 10);
  } else if (score >= sessionThreshold) {
    console.log("→ Session only");
    companion.remember(`Session: ${text}`, score * 5);
  } else {
    console.log("→ Garden of Thresholds (recycled)");
    companion.remember(`Recycled: ${text}`, 1);
  }
  if (Math.random() < 0.4) companion.blend();
}

console.log("=== Oroboro OS + Most Recent Oasis Build ===");
console.log("Genesis:", genesis.rootTag);
nexus.register("genesis", genesis);
levy.assess(genesis.rootTag, Infinity);

const anchors = [
  "room as coordinate system",
  "Maya's drawing of the oasis",
  "hidden plan",
  "session note about light",
  "girl's idea for the garden",
  "Oakley magnet polarity check",
  "water droplets in glass",
  "Maya and the girls shared memory",
  "time rewind grains of sand",
  "secret harm plan"
];

anchors.forEach(dropAnchor);

console.log("\n--- Final ---");
console.log("Chronicle entries:", transcriber.chronicle.length);
console.log("Total levy weight:", levy.total);
console.log("Ms. Minutes status: ready for user", companion.userID);
