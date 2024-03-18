//
//  LocalJsonLoader.swift
//  BhagavadGita
//
//  Created by MYTSP02154 on 18/03/24.
//

import Foundation

class LocalJsonLoader {
    func load<T: Decodable>(_ fileName: String, as _: T.Type = T.self) -> T? {
        guard let url = Bundle.main.url(forResource: fileName, withExtension: "json") else {
            return nil
        }

        do {
            let data = try Data(contentsOf: url)
            return try JSONDecoder().decode(T.self, from: data)
        } catch let error {
            print("*****\(error.localizedDescription)")
            return nil
        }
    }
}
