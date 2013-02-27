Toml.net
========

A TOML parser in C#
Based on the spec at https://github.com/mojombo/toml
Needs some cleanup and a refactor or three.

TODO:
 - Add tests
 - Add Get methods to properly handle arrays - currently just returns the array string.
 - Toml.Document.ToJson
 - Toml.Document.FromJson
 - void Document.Serialize<T>(T value)
 - T Document.Deserialize<T>(Stream source)
 - Re-Write the Parser
 - Remove Group class and store everything in the root document
 - Add support for multiline strings, whenever a decision is made regarding handling (currently supports "1st line" + "2nd line" + "etc.."

Pull Requests encouraged.

Works as of mojombo/toml commit:
e3656ad493400895f4460f1244a25f8f8e31a32a
