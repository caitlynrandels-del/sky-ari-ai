
using UnityEngine;
namespace SkyAri.Core {
    public class MetaToolRegistry : IToolExecutor {
        System.Collections.Generic.Dictionary<string, System.Func<string, string>> tools = new();
        public void Register(string name, System.Func<string,string> fn) => tools[name]=fn;
        public string Execute(string name, string args) {
            if(!tools.ContainsKey(name)) return "{\"error\":\"Tool not found\"}";
            try { return tools[name](args); } catch(System.Exception e) { return "{\"error\":\""+e.Message+"\"}"; }
        }
        // Meta-specific tools - world is coordinate system
        public void RegisterMetaTools(OVRSpatialAnchor anchorPrefab) {
            Register("create_spatial_anchor", (json) => {
                // parse x,y,z from json
                var pos = JsonUtility.FromJson<Vector3>(json);
                // Instantiate anchor at pos
                return "{\"anchor_created_at\":"+JsonUtility.ToJson(pos)+"}";
            });
            Register("query_nearby_memories", (json) => "{\"memories\":[]}");
            Register("set_passthrough_opacity", (json) => "{\"ok\":true}");
        }
    }

    public class MetaHomeostasis : IHomeostasisRegulator {
        // From Automated immune/Metabolic System.txt - Garden of Thresholds
        public float energyThreshold = 0.2f;
        public MetabolicState Tick(float dt) {
            float battery = OVRManager.batteryLevel;
            float thermal = 0f;
            // If battery low or thermal high, raise threshold -> system becomes pickier (Oroboro breathing)
            bool shouldRest = battery < 0.15f || thermal > 0.8f;
            return new MetabolicState {
                batteryLevel = battery,
                thermalLevel = thermal,
                userComfort = shouldRest ? 0.3f : 0.9f,
                shouldRest = shouldRest
            };
        }
        public float RegulateSalienceThreshold(MetabolicState s) {
            // Under high load, threshold rises - from CombinedAiSalience: load_pressure
            return s.shouldRest ? 0.85f : 0.65f;
        }
    }
}
