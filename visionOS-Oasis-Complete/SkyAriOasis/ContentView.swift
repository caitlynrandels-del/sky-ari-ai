import SwiftUI

struct ContentView: View {
    @Environment(\.openImmersiveSpace) var openImmersiveSpace
    @State private var opened = false
    
    var body: some View {
        VStack(spacing: 28) {
            Text("Sky-Ari OASIS")
                .font(.extraLargeTitle)
            
            Text("Complete Build")
                .font(.title2)
                .foregroundStyle(.secondary)
            
            VStack(alignment: .leading, spacing: 8) {
                Label("World as coordinate system", systemImage: "globe")
                Label("Salience scoring + 6 principles", systemImage: "shield")
                Label("Family / daughter weight", systemImage: "heart")
                Label("Permanent WorldAnchors", systemImage: "pin")
                Label("Garden of Thresholds recycle", systemImage: "leaf")
            }
            .padding()
            
            Text("Gaze and pinch (or tap) to drop an anchor.\nFamily-related text gets extra score and pink markers.")
                .multilineTextAlignment(.center)
            
            Button(opened ? "Oasis Open" : "Enter Oasis") {
                Task {
                    await openImmersiveSpace(id: "OasisSpace")
                    opened = true
                }
            }
            .buttonStyle(.borderedProminent)
            .disabled(opened)
        }
        .padding(40)
    }
}
