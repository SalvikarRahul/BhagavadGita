//
//  Localized+Extenstion.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 18/03/24.
//

import Foundation

extension String {
    // Localizations Extenstion
    var localized: String {
        return NSLocalizedString(self, comment: "")
    }

    func localizedWithComment(comment: String = "") -> String {
        return NSLocalizedString(self, comment: comment)
    }
}
