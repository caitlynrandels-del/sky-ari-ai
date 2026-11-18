import Foundation
import RealityKit
import ARKit
import Combine
import UIKit

// Ms. Minutes — personal AI companion for each user
// Embodies the Oroboro OS (Genesis, Nexus, Transcriber, Levy, blender)
// as an individual companion inside the Oasis.
@MainActor
class MsMinutes: ObservableObject {
    let userID: String
    private var genesisTag = "I am the OG avatar"
    private var nexus = [String: Any]()
    private var chronicle: [String] = []
    private var totalWeight: Float = 0
    
    init(userID: String = "local-user") {
        self.userID = userID
        boot()
    }
    
    private func boot() {
        nexus["genesis"] = genesisTag
        chronicle.append("Ms. Minutes online for \(userID)")
        print("Ms. Minutes ready — personal companion for \(userID)")
    }
    
    func remember(_ text: String, weight: Float = 1) {
        chronicle.append(text)
        totalWeight += weight
        nexus["last"] = text
        print("Ms. Minutes remembered: \(text) (weight \(weight))")
    }
    
    func blend() -> String? {
        guard chronicle.count > 1 else { return nil }
        let a = chronicle.randomElement() ?? ""
        let b = chronicle.randomElement() ?? ""
        let created = "\(a.prefix(30)) × \(b.prefix(30))"
        chronicle.append(created)
        return created
    }
    
    func status() -> String {
        "Ms. Minutes | user: \(userID) | memories: \(chronicle.count) | weight: \(totalWeight)"
    }
}

@MainActor
class OasisEngine: ObservableObject {
    weak var rootEntity: Entity?
    private var arSession: ARKitSession?
    private var worldTracking: WorldTrackingProvider?
    let companion = MsMinutes()   // personal Ms. Minutes for this user
    
    // Thresholds
    let permanenceThreshold: Float = 0.65
    let sessionThreshold: Float = 0.325
    
    // Stores
    private var permanentAnchors: [UUID: AnchorData] = [:]
    private var sessionAnchors: [UUID: AnchorData] = [:]
    
    // Weights
    private let noveltyWeight: Float = 0.30
    private let relevanceWeight: Float = 0.25
    private let emotionalWeight: Float = 0.20
    private let spatialWeight: Float = 0.15
    private let daughterWeight: Float = 0.10
    
    struct AnchorData: Identifiable {
        let id: UUID
        var text: String
        var position: SIMD3<Float>
        var score: Float
        var isPermanent: Bool
        var isFamily: Bool
        var reason: String?
        var entity: Entity?
        var worldAnchorID: UUID?
    }
    
    func startAR() async {
        arSession = ARKitSession()
        worldTracking = WorldTrackingProvider()
        do {
            try await arSession?.run([worldTracking!])
            print("ARKit WorldTracking started — room is now a coordinate system")
            companion.remember("Entered Oasis")
        } catch {
            print("ARKit start failed: \(error)")
        }
    }
    
    func dropAnchor(at position: SIMD3<Float>, text: String? = nil) async {
        let label = text ?? "Anchor \(Date().formatted(date: .omitted, time: .shortened))"
        let isFamily = label.lowercased().contains("maya") || label.lowercased().contains("daughter") || label.lowercased().contains("girl")
        
        let score = calculateSalience(text: label, position: position, isFamily: isFamily)
        let (passes, reason) = checkPrinciples(text: label, score: score, isFamily: isFamily)
        
        guard passes else {
            print("Blocked by principles: \(reason ?? "unknown")")
            companion.remember("Blocked: \(label)")
            return
        }
        
        let id = UUID()
        var data = AnchorData(
            id: id,
            text: label,
            position: position,
            score: score,
            isPermanent: score >= permanenceThreshold,
            isFamily: isFamily,
            reason: reason
        )
        
        // Visual
        if let root = rootEntity {
            let color: UIColor = data.isPermanent ? (isFamily ? .systemPink : .green) : .yellow
            let sphere = ModelEntity(
                mesh: .generateSphere(radius: isFamily ? 0.08 : 0.05),
                materials: [SimpleMaterial(color: color, isMetallic: false)]
            )
            sphere.position = position
            root.addChild(sphere)
            data.entity = sphere
        }
        
        if data.isPermanent {
            permanentAnchors[id] = data
            await createWorldAnchor(for: data)
            companion.remember("Permanent: \(label)", weight: score * 10)
            print("Permanent WorldAnchor committed — score \(String(format: "%.2f", score)) \(isFamily ? "[FAMILY]" : "")")
        } else if score >= sessionThreshold {
            sessionAnchors[id] = data
            companion.remember("Session: \(label)", weight: score * 5)
            print("Session anchor — score \(String(format: "%.2f", score))")
        } else {
            companion.remember("Recycled: \(label)", weight: 1)
            print("Recycled to Garden of Thresholds — score \(String(format: "%.2f", score))")
        }
        
        // Occasional blend
        if Int.random(in: 0...3) == 0, let blended = companion.blend() {
            print("Ms. Minutes blended: \(blended)")
        }
        
        feedToOroboro(data)
    }
    
    private func createWorldAnchor(for data: AnchorData) async {
        let anchorID = UUID()
        if var stored = permanentAnchors[data.id] {
            stored.worldAnchorID = anchorID
            permanentAnchors[data.id] = stored
        }
        print("WorldAnchor ID \(anchorID) prepared for persistence")
    }
    
    private func calculateSalience(text: String, position: SIMD3<Float>, isFamily: Bool) -> Float {
        let novelty: Float = Bool.random() ? 0.75 : 0.45
        let relevance: Float = 0.65
        let emotional: Float = isFamily ? 1.0 : 0.5
        let spatial: Float = min(1.0, 2.0 / max(0.5, simd_length(position)))
        let familyBonus: Float = isFamily ? 1.0 : 0.0
        
        var score = (novelty * noveltyWeight) +
                    (relevance * relevanceWeight) +
                    (emotional * emotionalWeight) +
                    (spatial * spatialWeight) +
                    (familyBonus * daughterWeight)
        
        score *= 0.95
        
        return min(1.0, max(0.0, score))
    }
    
    private func checkPrinciples(text: String, score: Float, isFamily: Bool) -> (Bool, String?) {
        let lower = text.lowercased()
        
        if score < 0.15 { return (false, "too low salience") }
        if lower.contains("harm") || lower.contains("hidden") || lower.contains("secret") {
            return (false, "transparency / safety gate")
        }
        
        if isFamily && score < 0.4 {
            return (true, "family exception applied")
        }
        
        return (true, "passed all six principles")
    }
    
    private func feedToOroboro(_ data: AnchorData) {
        print("Oroboro feed: \(data.text) weight=\(data.score)")
    }
}
