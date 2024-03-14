//
//  ContentView.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 13/03/24.
//

import SwiftUI
import BGUtility

struct ContentView: View {
    var body: some View {
        VStack {
            Image(systemName: "globe")
                .imageScale(.large)
                .foregroundStyle(.tint)
            Text("Hello, world!")
        }
        .padding()
        .onAppear {
            let numbers = [1, 2, 3, 4, 5]
            let num1 = numbers[safeIndex: 0]
            print(num1)

        }
    }
}

#Preview {
    ContentView()
}
