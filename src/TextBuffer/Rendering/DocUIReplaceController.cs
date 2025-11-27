/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *
 *  TypeScript Source:
 *  - Conceptually based on VS Code's find/replace controller pattern
 *  - No direct 1:1 TypeScript source (C# architectural adapter)
 *--------------------------------------------------------------------------------------------*/

using System.Text.RegularExpressions;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Rendering;

/// <summary>
/// DocUI Replace Controller provides high-level replace operations
/// integrating ReplacePattern with TextModel for find/replace scenarios.
/// 
/// TODO(B2): Integrate with FindModel state for incremental replace
/// TODO(B2): Add WordSeparator context for word boundary support
/// </summary>
public class DocUIReplaceController
{
    private readonly TextModel _model;

    public DocUIReplaceController(TextModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Replace a single match with the given replace pattern.
    /// </summary>
    /// <param name="pattern">The replace pattern (can contain $1, $2, etc.)</param>
    /// <param name="searchResult">The regex match result</param>
    /// <param name="preserveCase">Whether to preserve case from the match</param>
    /// <returns>The replacement string</returns>
    public string Replace(string pattern, Match searchResult, bool preserveCase = false)
    {
        ReplacePattern replacePattern = ReplacePatternParser.ParseReplaceString(pattern);

        string[]? matches = null;
        if (searchResult.Success)
        {
            matches = new string[searchResult.Groups.Count];
            for (int i = 0; i < searchResult.Groups.Count; i++)
            {
                matches[i] = searchResult.Groups[i].Value;
            }
        }

        return replacePattern.BuildReplaceString(matches, preserveCase);
    }

    /// <summary>
    /// Replace all matches with the given replace pattern.
    /// </summary>
    /// <param name="pattern">The replace pattern (can contain $1, $2, etc.)</param>
    /// <param name="searchResults">The list of regex match results</param>
    /// <param name="preserveCase">Whether to preserve case from matches</param>
    /// <returns>List of replacement strings</returns>
    public List<string> ReplaceAll(string pattern, IEnumerable<Match> searchResults, bool preserveCase = false)
    {
        List<string> replacements = [];
        foreach (Match match in searchResults)
        {
            replacements.Add(Replace(pattern, match, preserveCase));
        }
        return replacements;
    }

    /// <summary>
    /// Execute a replace operation and apply it to the text model.
    /// </summary>
    /// <param name="pattern">The replace pattern</param>
    /// <param name="searchResult">The match to replace</param>
    /// <param name="preserveCase">Whether to preserve case</param>
    /// <exception cref="NotImplementedException">Edit pipeline integration pending</exception>
    public void ExecuteReplace(string pattern, Match searchResult, bool preserveCase = false)
    {
        // TODO(B2): Integrate with TextModel's edit operations and decoration updates
        // Until the edit pipeline is wired, throw to prevent silent data-loss scenarios.
        // Implementation plan:
        //   1. var replacement = Replace(pattern, searchResult, preserveCase);
        //   2. var startPosition = _model.GetPositionAt(searchResult.Index);
        //   3. var endPosition = _model.GetPositionAt(searchResult.Index + searchResult.Length);
        //   4. _model.PushEditOperations(null, new[] { new EditOperation(...) }, null);
        throw new NotImplementedException(
            "ExecuteReplace requires TextModel.PushEditOperations integration (tracked in Batch #2 scope)");
    }
}

/// <summary>
/// Helper class for DocUI replace pattern testing and debugging.
/// </summary>
public static class DocUIReplaceHelper
{
    /// <summary>
    /// Quick replace helper for testing scenarios.
    /// </summary>
    public static string QuickReplace(string input, string searchPattern, string replacePattern, bool preserveCase = false)
    {
        Regex regex = new(searchPattern);
        Match match = regex.Match(input);

        if (!match.Success)
        {
            return input;
        }

        ReplacePattern pattern = ReplacePatternParser.ParseReplaceString(replacePattern);
        string[]? matches = new string[match.Groups.Count];
        for (int i = 0; i < match.Groups.Count; i++)
        {
            matches[i] = match.Groups[i].Value;
        }

        string replacement = pattern.BuildReplaceString(matches, preserveCase);
        return input.Substring(0, match.Index) + replacement + input.Substring(match.Index + match.Length);
    }
}
