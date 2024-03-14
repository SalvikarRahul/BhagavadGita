//
//  ContentView.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 13/03/24.
//

import SwiftUI
import BGUtility

struct ContentView: View {
    @ObservedObject var viewModel = ContentViewModel()
    var body: some View {
        VStack {
            Image(systemName: "globe")
                .imageScale(.large)
                .foregroundStyle(.tint)
            Text("Hello, world!")
        }
        .padding()
        .onAppear {
            viewModel.onAppear()
        }
    }
}

#Preview {
    ContentView()
}
