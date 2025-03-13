namespace Nex.Tests.Core

open Xunit
open Nex.Core.Utils.Globbing

module GlobTests =

    [<Theory>]

    // 1. Basic Wildcards (* and ?)
    [<InlineData("*", "file.txt", true)>]
    [<InlineData("*", "image.png", true)>]
    [<InlineData("*", "dir/sub/file.txt", false)>] // "*" doesn't match paths
    [<InlineData("*.txt", "file.txt", true)>]
    [<InlineData("*.txt", "file.md", false)>]
    [<InlineData("?at", "cat", true)>]
    [<InlineData("?at", "bat", true)>]
    [<InlineData("?at", "at", false)>] // Should be exactly one character before "at"

    // Character Classes: Simple Matching
    [<InlineData("[cb]at", "cat", true)>] // Matches 'c'
    [<InlineData("[cb]at", "bat", true)>] // Matches 'b'
    [<InlineData("[cb]at", "rat", false)>] // 'r' is not in [cb]

    [<InlineData("[abc]file.txt", "afile.txt", true)>] // Matches 'a'
    [<InlineData("[abc]file.txt", "bfile.txt", true)>] // Matches 'b'
    [<InlineData("[abc]file.txt", "zfile.txt", false)>] // 'z' is not in [abc]

    // Character Classes: Case Sensitivity
    [<InlineData("[a-z]at", "cat", true)>] // Matches lowercase
    [<InlineData("[a-z]at", "Bat", false)>] // Case-sensitive: 'B' is uppercase
    [<InlineData("[A-Z]at", "Cat", true)>] // Matches uppercase
    [<InlineData("[A-Z]at", "cat", false)>] // Lowercase 'c' doesn't match [A-Z]

    // Character Classes: Numeric Ranges
    [<InlineData("[0-9]file.txt", "3file.txt", true)>] // Matches '3'
    [<InlineData("[0-9]file.txt", "10file.txt", false)>] // Only first character matters
    [<InlineData("[1-5]log.txt", "3log.txt", true)>] // Matches '3'
    [<InlineData("[1-5]log.txt", "6log.txt", false)>] // '6' is out of range

    // Character Classes: Special Characters
    [<InlineData("[!@#]file.txt", "!file.txt", true)>] // Matches '!'
    [<InlineData("[!@#]file.txt", "@file.txt", true)>] // Matches '@'
    [<InlineData("[!@#]file.txt", "#file.txt", true)>] // Matches '#'
    [<InlineData("[!@#]file.txt", "afile.txt", false)>] // 'a' is not in the set

    // Negation in Character Classes
    [<InlineData("[!c]at", "bat", true)>] // Matches because 'b' is not 'c'
    [<InlineData("[!c]at", "cat", false)>] // Should not match because 'c' is excluded
    [<InlineData("[!0-9]file.txt", "afile.txt", true)>] // Matches 'a' (not a number)
    [<InlineData("[!0-9]file.txt", "5file.txt", false)>] // '5' is in the excluded range

    // Complex Multi-Character Classes
    [<InlineData("[a-zA-Z]name.txt", "Zname.txt", true)>] // Matches uppercase 'Z'
    [<InlineData("[a-zA-Z]name.txt", "9name.txt", false)>] // '9' is not a letter
    [<InlineData("[a-z0-9]data.json", "xdata.json", true)>] // Matches 'x'
    [<InlineData("[a-z0-9]data.json", "Data.json", false)>] // 'D' is uppercase, not matched
    [<InlineData("[A-Za-z0-9]data.json", "5data.json", true)>] // Matches '5'

    [<InlineData("[A-Za-z!@#]test.log", "!test.log", true)>] // Matches '!'
    [<InlineData("[A-Za-z!@#]test.log", "3test.log", false)>] // '3' is not in set

    // Edge Cases
    [<InlineData("[0-9][0-9]data.json", "42data.json", true)>] // Matches '42'
    [<InlineData("[0-9][0-9]data.json", "4data.json", false)>] // Needs two digits
    [<InlineData("[a-zA-Z][0-9]file.txt", "a9file.txt", true)>] // Matches 'a9'
    [<InlineData("[a-zA-Z][0-9]file.txt", "99file.txt", false)>] // Both need to match constraints

    // 3. Recursive Wildcards (**)
    [<InlineData("**/*.png", "assets/files/photos/1.png", true)>]
    [<InlineData("**/*.txt", "docs/file.txt", true)>]
    [<InlineData("**/*.txt", "file.txt", true)>]
    [<InlineData("**/*.txt", "docs/subdir/file.txt", true)>]

    // 4. Nested Paths and Wildcards
    [<InlineData("docs/**", "docs/file.txt", true)>]
    [<InlineData("docs/**", "docs/subdir/file.txt", true)>]
    [<InlineData("docs/**", "src/file.txt", false)>] // Not under "docs"
    [<InlineData("docs/*/*.txt", "docs/subdir/file.txt", true)>]
    [<InlineData("docs/*/*.txt", "docs/file.txt", false)>] // Requires one subdir

    // 5. Windows and Unix Paths
    [<InlineData("C:\\Users\\*\\Documents\\*.txt", "C:\\Users\\John\\Documents\\notes.txt", true)>]
    [<InlineData("C:\\Windows\\System32\\drivers\\*.sys", "C:\\Windows\\System32\\drivers\\net.sys", true)>]
    [<InlineData("/home/user/*.txt", "/home/user/report.txt", true)>]
    [<InlineData("/var/log/**/*.log", "/var/log/nginx/error.log", true)>]

    // 6. File Extensions and Multiple Formats
    [<InlineData("*.{js,ts}", "script.js", true)>]
    [<InlineData("*.{js,ts}", "script.ts", true)>]
    [<InlineData("*.{js,ts}", "script.cs", false)>] // Not a matching extension
    [<InlineData("*.{jpg,png,gif}", "photo.jpg", true)>]
    [<InlineData("*.{jpg,png,gif}", "photo.bmp", false)>] // Only jpg, png, gif allowed

    // 7. Negation and Exclusion Patterns
    [<InlineData("!*.tmp", "file.tmp", false)>] // Should be explicitly excluded
    [<InlineData("!backup/**/*.old", "backup/2023/data.old", false)>]
    [<InlineData("!docs/**/*.md", "docs/README.md", false)>]
    [<InlineData("!docs/**/*.md", "docs/subdir/file.md", false)>]

    // 8. Edge Cases and Invalid Inputs
    [<InlineData("file?.txt", "file.txt", false)>] // "?" requires one extra character
    [<InlineData("log[1-3].log", "log4.log", false)>] // Outside range
    [<InlineData("**/docs/**", "docsfile.txt", false)>] // Partial match not allowed
    [<InlineData("docs/**/subdir/*.txt", "docs/subdir", false)>] // Should be a file
    [<InlineData("*.*", "noextension", false)>] // No dot in filename
    [<InlineData("[!0-9].txt", "5.txt", false)>] // "5" is in the exclusion range
    [<InlineData("[!a-z].txt", "file.txt", false)>] // "f" is in range, so should fail
    [<InlineData("docs/**/subdir/*.txt", "src/docs/subdir/file.txt", false)>] // Outside allowed path]
    let ``Globbing pattern match tests`` (pattern: string) (path: string) (expected: bool) =
        let result = isMatch pattern path
        Assert.Equal(expected, result)
