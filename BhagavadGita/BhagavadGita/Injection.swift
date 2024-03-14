//
//  Injection.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 14/03/24.
//

import Foundation
import Swinject

final class Injection {
    static let shared = Injection()
    var container: Container {
        get {
            if _container == nil {
                _container = buildContainer()
            }
            return _container!
        }
        set {
            _container = newValue
        }
    }

    private var _container: Container?
    private func buildContainer() -> Container {
        let container = Container()
        container.register(BGLoggerType.self) { _ in
            return BGLogger()
        }

        return container
    }
}

@propertyWrapper struct Injected<Dependency> {
    let wrappedValue: Dependency

    init() {
        self.wrappedValue = Injection.shared.container.resolve(Dependency.self)!
    }
}
