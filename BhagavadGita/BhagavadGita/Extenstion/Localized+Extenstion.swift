//
//  Localized+Extenstion.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 18/03/24.
// Localized strings extension for easy localization handling

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
