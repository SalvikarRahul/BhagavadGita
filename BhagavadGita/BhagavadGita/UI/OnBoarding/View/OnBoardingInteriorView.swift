//
//  OnBoardingInteriorView.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 18/03/24.
//

import Foundation
import SwiftUI

struct OnBoardingInteriorView: View {
    var onBoardingModel: OnBoardingModel
    @State private var isAnimating = false
    var body: some View {
        VStack(spacing: 20) {
             Spacer().frame(height: 10)
            Image(onBoardingModel.image)
                .resizable()
                .scaledToFit()
                .scaleEffect(isAnimating ? 1.0: 0.5)
            Text(LocalizedStringKey(onBoardingModel.title))
                .foregroundColor(.black)
                .multilineTextAlignment(.center)
                .font(.title)
                .fontWeight(.heavy)
                .padding(.horizontal, 16)
            Text(LocalizedStringKey(onBoardingModel.detail))
                .foregroundColor(.gray)
                .multilineTextAlignment(.center)
                .font(.title3)
                .padding(.horizontal, 16)
            Spacer()
        }.onAppear {
            withAnimation(.easeOut(duration: 0.5)) {
                isAnimating = true
            }
        }
    }
}
