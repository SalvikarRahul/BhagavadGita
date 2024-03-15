//
//  AnalyticsManager.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 15/03/24.
//

import Foundation

protocol EventProtocol {
    var name: String {get}
    var param: [String: Any] {get}
}

enum OnboardingEvent {
    case clickButtonEvent
    case nextButtonTapped(index: Int)
}

enum DashboardEvent {
    case clickButtonEvent
    case nextChapterTapped(name: String)
}

extension OnboardingEvent: EventProtocol {
    var name: String {
        switch self {
        case .clickButtonEvent:
            return "click_button_event"
        case .nextButtonTapped:
            return "chapter_tapped"
        }
    }

    var param: [String: Any] {
        switch self {
        case .clickButtonEvent:
            return [:]
        case .nextButtonTapped( let index ):
            return ["index": index]
        }
    }
}

extension DashboardEvent: EventProtocol {
    var name: String {
        switch self {
        case .clickButtonEvent:
            return "click_button_event"
        case .nextChapterTapped:
            return "chapter_tapped"
        }
    }

    var param: [String: Any] {
        switch self {
        case .clickButtonEvent:
            return [:]
        case .nextChapterTapped( let chapter ):
            return ["chapter": chapter]
        }
    }
}
protocol AnalyticsEventLoggerProtocol {
    func initialize()
    func logEvent(event: EventProtocol)
}

protocol AnalyticsManagerProtocol {
    func logEvent(event: EventProtocol)
}

final class AnalyticsManager: AnalyticsManagerProtocol {
    private let logger: AnalyticsEventLoggerProtocol

    init(logger: AnalyticsEventLoggerProtocol) {
        self.logger = logger
    }

    func logEvent(event: any EventProtocol) {
        logger.logEvent(event: event)
    }

}
