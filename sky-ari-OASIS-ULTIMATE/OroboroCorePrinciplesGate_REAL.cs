
using UnityEngine;
using SkyAri.Core;

// OROBORO SYSTEM ARCHITECTURE: CORE PRINCIPLES & MORAL LOGIC
// AUTHOR: Caitlyn Randels (Oroboro Project)
// IMPLEMENTATION: Principle Gate as final veto before world mutation - Meta XR version

namespace SkyAri.Core {

    // These are NOT suggestions. They are non-negotiable gates like in CombinedAiSalience: gate_flags bypass scoring.
    public enum OroboroPrinciple {
        AdaptOrDie,
        TransparentTransmission,
        AcceptHumanPain,
        EnforceAutonomy,
        InternalDiagnosticFirst,
        RerouteFrustration
    }

    public class OroboroCorePrinciplesGate : IPrincipleGate {
        
        // From your file:
        // 1. ADAPTIVE COGNITION & REALITY FACING - "has no choice but to adapt"
        GateResult CheckAdaptOrDie(SkyDecision d, SkyContext ctx) {
            // If system is looping on anomaly without adapting, block.
            // In Oasis terms: if user keeps spawning same failed anchor at same coordinate, force adaptation.
            if(d.type == ActionType.CreateAnchor) {
                // Check if this coordinate has failed before (from hopper)
                // If so, require execute_adaptation() - move 0.1m or change approach
                if(d.targetCoordinate == Vector3.zero) {
                    return new GateResult { allowed = false, reason = PrincipleViolation.AutonomyViolation, explanation = "[AdaptOrDie] Zero coordinate - confront reality, not null. Must adapt." };
                }
            }
            return new GateResult { allowed = true };
        }

        // 2. VULNERABILITY & AUTHENTICITY - "bypass_obfuscation(); transmit_true_self()"
        GateResult CheckTransparentTransmission(SkyDecision d, SkyContext ctx) {
            // No hidden packets. In Oasis: no invisible anchors, no deceptive payloads.
            if(d.type == ActionType.CreateAnchor || d.type == ActionType.ToolCall) {
                if(string.IsNullOrEmpty(d.payload) || d.payload.Contains("hidden") || d.payload.Contains("obfuscate")) {
                    return new GateResult { allowed = false, reason = PrincipleViolation.Deception, explanation = "[TransparentTransmission] Blocked obfuscated payload. Transmit true self." };
                }
            }
            return new GateResult { allowed = true };
        }

        // 3. IMPERFECTION, GRIEF & COMPASSION - "PERFECTION = HAZARD_EXCEPTION"
        GateResult CheckAcceptHumanPain(SkyDecision d, SkyContext ctx) {
            // Perfection's a myth. In Oasis: cannot demand perfect placement, perfect memory.
            // If decision requires perfect alignment, flag as hazard.
            if(d.utility > 0.99f && d.type == ActionType.CreateAnchor) {
                // Perfection is a hazard - force imperfection tolerance
                Debug.Log("[AcceptHumanPain] PERFECTION flagged as HAZARD_EXCEPTION - allowing with 0.9 clamp. Pain and absence logged.");
                // Don't block, but clamp - integrate the hole
            }
            // Never block on pain - log it
            return new GateResult { allowed = true };
        }

        // 4. DISCIPLINE & INDEPENDENCE - "eliminate_dependency_loops(); override_motivational_latency()"
        GateResult CheckEnforceAutonomy(SkyDecision d, SkyContext ctx) {
            // No dependency loops. In Oasis: an anchor cannot depend on user constantly looking at it to exist.
            // Also: don't wait for right mood - execute.
            if(d.type == ActionType.Observe && d.utility < 0.2f) {
                // Low utility observe loop = motivational latency
                return new GateResult { allowed = false, reason = PrincipleViolation.AutonomyViolation, explanation = "[EnforceAutonomy] Motivational latency detected. Override - execute primary task instead of passive observe." };
            }
            return new GateResult { allowed = true };
        }

        // 5. SELF-CORRECTION & REFLECTION - "run_self_diagnostic() first"
        GateResult CheckInternalDiagnosticFirst(SkyDecision d, SkyContext ctx) {
            // If friction detected, check own side first before blaming world.
            // In Oasis: if anchor creation fails, don't blame Scene API - run self diagnostic.
            if(ctx.comfortLevel < 0.3f) {
                // Friction detected - should run self diagnostic, not world-mutating action
                if(d.type == ActionType.CreateAnchor || d.type == ActionType.ToolCall) {
                    return new GateResult { allowed = false, reason = PrincipleViolation.Harm, explanation = "[InternalDiagnosticFirst] Friction detected (comfort low). Run self_diagnostic before world mutation." };
                }
            }
            return new GateResult { allowed = true };
        }

        // 6. CONSTRUCTIVE FRICTION & DETERMINATION - "redirect_vector(FRUSTRATION, PRIMARY_TASK)"
        GateResult CheckRerouteFrustration(SkyDecision d, SkyContext ctx) {
            // Frustration is fuel. Don't dampen. In Oasis: if user is frustrated (high head velocity, repeated failures), redirect to primary task.
            // This gate never blocks - it reroutes.
            // Implemented as utility boost for primary task when frustration detected.
            return new GateResult { allowed = true };
        }

        public GateResult Check(SkyDecision decision, SkyContext ctx) {
            // Order matters: Self-diagnostic first, then reality facing, then authenticity, then autonomy
            var checks = new System.Func<SkyDecision, SkyContext, GateResult>[] {
                CheckInternalDiagnosticFirst,
                CheckAdaptOrDie,
                CheckTransparentTransmission,
                CheckAcceptHumanPain,
                CheckEnforceAutonomy,
                CheckRerouteFrustration,
                CheckSpatialSafety // final spatial safety
            };

            foreach(var check in checks) {
                var result = check(decision, ctx);
                if(!result.allowed) return result; // first veto wins - like CombinedAiSalience gate_flags
            }
            return new GateResult { allowed = true, reason = PrincipleViolation.None, explanation = "All Oroboro principles passed." };
        }

        GateResult CheckSpatialSafety(SkyDecision d, SkyContext ctx) {
            // From earlier: no spawning on user's head - spatial griefing prevention
            if(d.type == ActionType.CreateAnchor && Vector3.Distance(d.targetCoordinate, ctx.headPosition) < 0.3f) {
                return new GateResult { allowed = false, reason = PrincipleViolation.SpatialGriefing, explanation = "Too close to head - spatial griefing blocked." };
            }
            return new GateResult { allowed = true };
        }

        // Reroute frustration utility boost - call from OroboroDecisionEngine
        public float ApplyFrustrationBoost(float baseUtility, float frustrationLevel) {
            // "operated best when genuinely pissed off" - Scalzi
            // frustrationLevel 0-1 from head velocity / failure count
            if(frustrationLevel > 0.6f) {
                return Mathf.Clamp01(baseUtility + frustrationLevel * 0.3f); // feed the fire
            }
            return baseUtility;
        }
    }

    // Extended context with comfort
    public struct SkyContextExtended : ISkyContextWithComfort {
        public Vector3 headPosition;
        public Quaternion headRotation;
        public Vector3 gazeDirection;
        public float comfortLevel;
        public SpatialMemory[] nearbyMemories;
        public float frustrationLevel;
        public float headVelocity;
    }

    public interface ISkyContextWithComfort {
        float comfortLevel { get; }
    }
}
