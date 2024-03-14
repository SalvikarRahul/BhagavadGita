//
//  ContentViewModel.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 14/03/24.
//

import Foundation

class ContentViewModel: ObservableObject {
    @Injected private var logger: BGLoggerType

    func onAppear() {
        logger.warning("\(API.baseURL) -> \(ConfigurationManager.environment)")
    }
}
