import SwiftUI
import RealityKit
import ARKit

struct OasisImmersiveView: View {
    @StateObject private var engine = OasisEngine()
    
    var body: some View {
        RealityView { content in
            let root = Entity()
            content.add(root)
            engine.rootEntity = root
            
            // Demo floor (replaced by real ARKit planes on device)
            let plane = ModelEntity(
                mesh: .generatePlane(width: 5, depth: 5),
                materials: [SimpleMaterial(color: .white.withAlphaComponent(0.15), isMetallic: false)]
            )
            plane.position = [0, -0.5, -1.5]
            root.addChild(plane)
        }
        .gesture(
            SpatialTapGesture()
                .targetedToAnyEntity()
                .onEnded { value in
                    Task {
                        // SpatialTapGesture gives a 2D CGPoint; convert to a 3D position in front of the user
                        let x = Float(value.location.x - 200) / 200.0   // rough screen-to-world mapping
                        let y = Float(200 - value.location.y) / 200.0
                        let position = SIMD3<Float>(x, y, -1.5)
                        await engine.dropAnchor(at: position)
                    }
                }
        )
        .task {
            await engine.startAR()
        }
    }
}
