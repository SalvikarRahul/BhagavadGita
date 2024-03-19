//
//  GetStartedPopOverView.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 19/03/24.
//

import Foundation
import SwiftUI

struct GetStartedPopOverView: View {
    @Binding var showPopup: Bool
    @State private var selectedIndex = 0
    var body: some View {
        VStack {
            Image("abc")
                .frame(width: 89, height: 120)
            Text("Onboarding-choosePreferredLanguage")
                .font(.title)
                .multilineTextAlignment(.center)
                .foregroundColor(.black)
            Text("Onboarding-dontWorryYouCanChangeLater")
                .font(.subheadline)
                .multilineTextAlignment(.center)
                .foregroundColor(.gray)
                .padding(.horizontal, 20)
                .padding(.top, 20)
            VStack(spacing: 20) {
                RadioButtonView(index: 0, text: "Onboarding-english", selectedIndex: $selectedIndex)
                RadioButtonView(index: 1, text: "Onboarding-hindi", selectedIndex: $selectedIndex)
            }.padding(.all, 40)
            Button {
                showPopup = false
            } label: {
                Text("Onboarding-okLetsUsGo")
                    .foregroundColor(.white)
                    .frame(maxWidth: .infinity)
                    .frame(height: 40)
                    .cornerRadius(4)
            }
            .background(Color.orange)
            .padding(.horizontal, 40)
            .padding(.bottom, 40)
        }
        .background(Color.white)
        .cornerRadius(20)
        .shadow(radius: 5)
        .padding()
    }
}

struct RadioButtonView: View {
    var index: Int
    var text: String
    @Binding var selectedIndex: Int

    var body: some View {
        Button {
            selectedIndex = index
        } label: {
            HStack(spacing: 20) {
                Image(systemName: selectedIndex == index ? "largecircle.fill.circle" : "circle")
                    .foregroundColor(.orange)
                Text(LocalizedStringKey(text))
                    .foregroundColor(.black)
                Spacer()
            }.padding(.horizontal, 40)
                .frame(maxWidth: .infinity)
        }
    }
}
