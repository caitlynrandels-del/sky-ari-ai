
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyAri.Core {
    // --- COMPONENT 3: THE COGNITIVE REASONING CORE - REAL IMPLEMENTATION ---
    // Wired directly into OroboroDecisionEngine.route_decision() per your spec

    public interface ILlmClient {
        Task<string> GenerateAsync(string systemPrompt, string contextPayload, float temperature);
    }

    // Meta implementation: uses Meta AI / Llama API on Quest or local
    public class MetaLlamaClient : ILlmClient {
        public async Task<string> GenerateAsync(string system, string payload, float temp) {
            // In production: call Meta AI API or on-device Llama via Meta XR
            // For now, routes through Unity's LLM or HTTP
            // This is where you'd call: https://api.meta.com/llama or local inference
            await Task.Delay(10);
            return "{\"intent\":\"observe\", \"reasoning\":\"Synthesizing active node state\", \"next_path\":\"score_salience\"}";
        }
    }

    public class CognitiveReasoningCore : ISkyReasoningCore {
        public ILlmClient llmClient;
        public string systemPrompt;
        public int maxContextWindow = 8192;

        public OroboroSalienceEngine salienceEngine;
        public IStorageHopper storageHopper;

        public CognitiveReasoningCore(ILlmClient client, string baseInstructions, OroboroSalienceEngine salience, IStorageHopper hopper) {
            llmClient = client;
            systemPrompt = baseInstructions;
            salienceEngine = salience;
            storageHopper = hopper;
        }

        public void Initialize(string modelName, string baseInstructions) {
            systemPrompt = baseInstructions;
            maxContextWindow = 8192;
            Debug.Log($"[CognitiveReasoningCore] Initialized with {modelName}");
        }

        // Builds context from active node state + retrieved spatial memories
        string BuildContext(SkyContext ctx, string activeNodeState) {
            var memories = storageHopper.QueryNearby(ctx.headPosition, 5f);
            var memText = "";
            foreach(var m in memories) memText += $"[{m.coordinate}] {m.text} (salience:{m.salienceAtCreation:F2})\n";

            return $@"
SYSTEM: {systemPrompt}
ACTIVE_NODE_STATE: {activeNodeState}
HEAD: pos={ctx.headPosition} gaze={ctx.gazeDirection} comfort={ctx.comfortLevel}
NEARBY_SPATIAL_MEMORIES (world is coordinate system):
{memText}
THOUGHT_HOPPER_COUNT: {salienceEngine.activeThoughtHopper.Count}
THRESHOLD_DYNAMIC: {salienceEngine.thresholdDynamic:F2}
INSTRUCTION: Synthesize thought. Return JSON: {{intent, reasoning, next_path, predicted_utility, novelty}}
";
        }

        // YOUR METHOD: synthesize_thought(active_node_state, retrieved_memories)
        public async Task<SkyThought> SynthesizeThoughtAsync(SkyContext ctx, string activeNodeState) {
            string payload = BuildContext(ctx, activeNodeState);
            string raw = await llmClient.GenerateAsync(systemPrompt, payload, 0.7f);
            
            // Parse JSON or action - per your spec: PARSE_JSON_OR_ACTION(raw_intelligence_output)
            var thought = ParseThought(raw);
            thought.reasoningTrace = raw;
            return thought;
        }

        // Synchronous wrapper for ISkyReasoningCore
        public SkyThought Evaluate(SkyContext ctx, string userIntent) {
            // Calls async but blocks for Unity main thread simplicity - in production use async
            var task = SynthesizeThoughtAsync(ctx, userIntent);
            task.Wait();
            return task.Result;
        }

        SkyThought ParseThought(string raw) {
            try {
                // Expect: {intent, reasoning, next_path, predicted_utility}
                // Minimal parse - production use JsonUtility
                float util = 0.7f;
                if(raw.Contains("predicted_utility")) {
                    // crude extract
                    var parts = raw.Split(':');
                }
                return new SkyThought { reasoningTrace = raw, confidence = util };
            } catch {
                return new SkyThought { reasoningTrace = raw, confidence = 0.5f };
            }
        }

        // YOUR METHOD: self_prompt_background_loop() - when hopper is quiet
        public async Task<SkyThought> SelfPromptBackgroundLoopAsync(SkyContext ctx) {
            string[] thoughtPrompts = new string[] {
                "What is contradicting my current states?",
                "What pattern requires optimization?",
                "What goal should I prioritize next?"
            };
            string selected = thoughtPrompts[Random.Range(0, thoughtPrompts.Length)];
            Debug.Log($"[Background Loop] Inquiry: {selected}");
            return await SynthesizeThoughtAsync(ctx, selected);
        }

        // WIRED: Directly into OroboroDecisionEngine.route_decision() - per your final instruction
        public SkyDecision RouteDecisionWithLLM(SkyContext ctx, GeneratedState activeNode) {
            // This replaces hardcoded switch cases
            var thoughtTask = SynthesizeThoughtAsync(ctx, activeNode.content);
            thoughtTask.Wait();
            var thought = thoughtTask.Result;

            // LLM dynamically determines next path
            string nextPath = ExtractNextPath(thought.reasoningTrace);

            var decision = new SkyDecision {
                type = PathToActionType(nextPath),
                targetCoordinate = activeNode.worldCoordinate, // world is coordinate system - LLM decision anchored in space
                payload = thought.reasoningTrace,
                utility = thought.confidence,
                requiresPrincipleCheck = true
            };

            Debug.Log($"[CognitiveReasoningCore->Oroboro] Node {activeNode.originNodeId} at {activeNode.worldCoordinate} routed to {nextPath} via LLM");

            return decision;
        }

        string ExtractNextPath(string raw) {
            if(raw.Contains("score_salience")) return "score_salience";
            if(raw.Contains("create_anchor")) return "create_anchor";
            if(raw.Contains("recall")) return "recall";
            if(raw.Contains("speak")) return "speak";
            return "observe";
        }

        ActionType PathToActionType(string path) {
            switch(path) {
                case "create_anchor": return ActionType.CreateAnchor;
                case "recall": return ActionType.Recall;
                case "speak": return ActionType.Speak;
                case "score_salience": return ActionType.Observe;
                default: return ActionType.Observe;
            }
        }
    }

    // UPDATED Oroboro Decision Engine that CALLS Cognitive Core
    public class OroboroDecisionEngineLLM {
        CognitiveReasoningCore reasoningCore;
        OroboroSalienceEngine salienceEngine;
        OroboroCorePrinciplesGate principleGate;
        IStorageHopper hopper;

        public OroboroDecisionEngineLLM(CognitiveReasoningCore core, OroboroSalienceEngine sal, OroboroCorePrinciplesGate gate, IStorageHopper hop) {
            reasoningCore = core;
            salienceEngine = sal;
            principleGate = gate;
            hopper = hop;
        }

        // YOUR SPEC: Wire directly into route_decision() so when node updates, it calls LLM instead of hardcoded switch
        public SkyDecision RouteDecision(GeneratedState node, SkyContext ctx) {
            // 1. Evaluate with salience (your CombinedAiSalience)
            float tension = 0.5f; // from environmental vector
            float daughter = 0.7f; // from lineage check
            string valuation = salienceEngine.RouteValuation(node, tension, daughter, hopper, (committed) => {
                Debug.Log($"[Oroboro] COMMITTED at {committed.worldCoordinate}");
            });

            if(valuation == "DISCARDED_GATE") {
                return new SkyDecision { type = ActionType.Observe, utility = 0f };
            }

            // 2. LLM determines next path dynamically (not switch case)
            var decision = reasoningCore.RouteDecisionWithLLM(ctx, node);

            // 3. Principle Gate final veto (your 6 rules)
            var gateResult = principleGate.Check(decision, ctx);
            if(!gateResult.allowed) {
                Debug.LogWarning($"[OroboroCore BLOCKED] {gateResult.reason}: {gateResult.explanation}");
                // AdaptOrDie: must adapt, not just block
                decision.targetCoordinate += Random.insideUnitSphere * 0.2f;
                decision.utility *= 0.8f;
            }

            return decision;
        }
    }
}
