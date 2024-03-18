//
//  OnBoardingViewModel.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 18/03/24.
//

import Foundation

class OnBoardingViewModel: ObservableObject {
    @Published var onBoardingData = [OnBoardingModel]()
    private let localJsonLoader: LocalJsonLoader
    init() {
        localJsonLoader = LocalJsonLoader()
        loadOnBoardingData()
    }

    func loadOnBoardingData() {
        guard let response: [OnBoardingModel] = localJsonLoader.load("OnBoarding") else {
            return
        }

        onBoardingData = response
    }
}
