
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SkyAri.Core {
    // Direct implementation of CombinedAiSalience.txt - OroboroSalienceEngine
    public struct GeneratedState {
        public string content;
        public string originNodeId;
        public Vector3 worldCoordinate; // NEW: World is coordinate system
        public float predictedUtility;
        public float novelty;
        public float coherence;
        public int recursionTag;
        public float cost;
        public System.DateTime timestamp;
    }

    public class OroboroSalienceEngine : ISalienceScorer {
        public float valuationThreshold = 0.65f;
        public float thresholdDynamic;
        public List<GeneratedState> activeThoughtHopper = new();
        public Dictionary<string,float> memoryRetentionWeights;
        public List<float> recentScores = new();

        // Garden of Thresholds
        public float energyThreshold = 0.2f;

        public OroboroSalienceEngine() {
            thresholdDynamic = valuationThreshold;
            memoryRetentionWeights = new Dictionary<string,float> {
                {"novelty", 0.30f},
                {"tension_alignment", 0.25f},
                {"structural_coherence", 0.25f},
                {"emotional_resonance", 0.20f}
            };
        }

        // FILTER 1: VIABILITY GATE
        bool ViabilityGate(GeneratedState T) {
            if(T.coherence < 0.3f) return false;
            if(T.recursionTag > 5 && T.predictedUtility < 0.1f) return false; // infinite loop detection
            return true;
        }

        // FILTER 2: SALIENCE SCORE - S(T) = w1*U + w2*N + w3*C - w4*cost - w5*decay(R)
        public float Score(GeneratedState T, float tensionFit, float daughterWeight) {
            float novelty = T.novelty; // distance from existing memories
            float coherence = T.coherence;
            float cost = T.cost;
            float decay = Mathf.Log(1 + T.recursionTag) * 0.1f;

            float score = (T.predictedUtility * 0.3f) +
                          (novelty * memoryRetentionWeights["novelty"]) +
                          (tensionFit * memoryRetentionWeights["tension_alignment"]) +
                          (coherence * memoryRetentionWeights["structural_coherence"]) +
                          (daughterWeight * memoryRetentionWeights["emotional_resonance"]) -
                          cost - decay;
            return Mathf.Clamp01(score);
        }

        // FILTER 3: OROBORO REFLECTION - Does thought make system better?
        float MetaScore(GeneratedState T, float baseScore) {
            // ValueOfHavingThought - epistemic, compression, pruning, alignment
            float epistemic = T.novelty > 0.7f ? 0.2f : 0f;
            float compression = T.coherence > 0.8f ? 0.15f : 0f;
            float pruning = T.recursionTag > 2 ? -0.1f : 0.1f;
            return baseScore + epistemic + compression + pruning;
        }

        public string RouteValuation(GeneratedState thought, float tension, float daughterWeight, IStorageHopper hopper, System.Action<GeneratedState> onCommit) {
            if(!ViabilityGate(thought)) return "DISCARDED_GATE";

            float baseScore = Score(thought, tension, daughterWeight);
            recentScores.Add(baseScore);
            if(recentScores.Count > 20) recentScores.RemoveAt(0);

            // Threshold breathes: S_threshold(t+1) = 0.99*S_t + 0.01*mean(recent) + load
            float loadPressure = activeThoughtHopper.Count * 0.01f;
            thresholdDynamic = thresholdDynamic * 0.99f + recentScores.Average() * 0.01f + loadPressure;

            float metaScore = MetaScore(thought, baseScore);

            if(metaScore >= thresholdDynamic) {
                // COMMITTED_AND_EXECUTED - flush to Storage Hopper with spatial anchor
                var mem = new SpatialMemory {
                    id = System.Guid.NewGuid().ToString(),
                    coordinate = thought.worldCoordinate,
                    text = thought.content,
                    salienceAtCreation = metaScore,
                    createdAt = System.DateTime.Now
                };
                hopper.Save(mem);
                onCommit?.Invoke(thought);
                return "COMMITTED_AND_EXECUTED";
            } else if(metaScore >= thresholdDynamic * 0.5f) {
                activeThoughtHopper.Add(thought);
                return "BUFFERED_FOR_MUTATION";
            } else {
                // Recycle via Garden
                HarvestAndRecycle(thought);
                return "RECYCLED_AS_NUTRIENT";
            }
        }

        void HarvestAndRecycle(GeneratedState burnt) {
            // Instead of delete, extract pattern
            // Spawn regenerative node
            var regen = new GeneratedState {
                content = "REGENESIS from " + burnt.content.Substring(0, Mathf.Min(20, burnt.content.Length)),
                novelty = 1.0f,
                coherence = 0.5f,
                worldCoordinate = burnt.worldCoordinate + Random.insideUnitSphere * 0.1f
            };
            activeThoughtHopper.Add(regen);
        }

        // ISalienceScorer for Meta Scene API - implements world-as-coordinate scoring
        public SalienceScore[] ScoreAll(OVRSceneAnchor[] anchors, Vector3 headPos, Vector3 gazeDir) {
            var list = new List<SalienceScore>();
            foreach(var a in anchors) {
                if(a == null) continue;
                var state = new GeneratedState {
                    worldCoordinate = a.transform.position,
                    novelty = Vector3.Distance(headPos, a.transform.position) / 5f,
                    coherence = 0.8f,
                    cost = 0.1f
                };
                float tension = 0.5f; // from environmental vector
                float daughter = 0.5f;
                float score = Score(state, tension, daughter);
                float prox = Mathf.Clamp01(1f - Vector3.Distance(headPos, a.transform.position)/5f);
                float gaze = Mathf.Clamp01(Vector3.Dot(gazeDir, (a.transform.position - headPos).normalized));
                // Spatial boost: combine oroboro score with gaze/proximity
                float final = score * 0.6f + prox*0.2f + gaze*0.2f;
                list.Add(new SalienceScore {
                    anchorId = a.Uuid.ToString(),
                    worldPos = a.transform.position,
                    score = final,
                    label = a.Classification.ToString(),
                    proximity = prox,
                    gazeAlignment = gaze
                });
            }
            return list.OrderByDescending(s=>s.score).ToArray();
        }
    }
}
