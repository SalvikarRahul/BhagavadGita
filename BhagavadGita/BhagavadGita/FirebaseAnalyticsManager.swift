//
//  FirebaseAnalyticsManager.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 15/03/24.
//

import Foundation
import Firebase

final class FirebaseAnalyticsManager: AnalyticsEventLoggerProtocol {
    func initialize() {
    }

    func logEvent(event: any EventProtocol) {
        Analytics.logEvent(event.name, parameters: event.param)
    }
}
