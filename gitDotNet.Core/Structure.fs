namespace gitDotNet.Core

module Structure =
    open System.Text.RegularExpressions
    open System.IO

    let private refPattern : Regex =
            new Regex("^ref\: (?<ref>[^\n]+)\n{0,1}", RegexOptions.Compiled);

    let getHead(gitPath : string) : string =
            let headText = File.ReadAllText(Path.Combine(gitPath, "HEAD"))
            let headTextMatch = refPattern.Match(headText)
            headTextMatch.Groups.["ref"].Value;