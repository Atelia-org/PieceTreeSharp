/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *
 *  TypeScript Source:
 *  - ts/src/vs/editor/contrib/find/browser/replacePattern.ts (Lines: 1-340, ReplacePattern/ReplacePiece/parseReplaceString)
 *  - ts/src/vs/base/common/search.ts (Lines: 8-50, buildReplaceStringWithCasePreserved)
 *--------------------------------------------------------------------------------------------*/

using System.Text;

namespace PieceTree.TextBuffer.Core;

/// <summary>
/// Enum representing the kind of replace pattern.
/// </summary>
internal enum ReplacePatternKind
{
    StaticValue = 0,
    DynamicPieces = 1
}

/// <summary>
/// A replace piece can either be a static string or an index to a specific match.
/// Ported from TypeScript: ReplacePiece class.
/// </summary>
public class ReplacePiece
{
    public static ReplacePiece StaticValue(string value)
    {
        return new ReplacePiece(value, -1, null);
    }

    public static ReplacePiece MatchIndex(int index)
    {
        return new ReplacePiece(null, index, null);
    }

    public static ReplacePiece CaseOps(int index, string[] caseOps)
    {
        return new ReplacePiece(null, index, caseOps);
    }

    public string? StaticValueData { get; }
    public int MatchIndexValue { get; }
    public string[]? CaseOpsValue { get; }

    private ReplacePiece(string? staticValue, int matchIndex, string[]? caseOps)
    {
        StaticValueData = staticValue;
        MatchIndexValue = matchIndex;
        if (caseOps == null || caseOps.Length == 0)
        {
            CaseOpsValue = null;
        }
        else
        {
            CaseOpsValue = new string[caseOps.Length];
            Array.Copy(caseOps, CaseOpsValue, caseOps.Length);
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is ReplacePiece other)
        {
            if (StaticValueData != other.StaticValueData)
            {
                return false;
            }

            if (MatchIndexValue != other.MatchIndexValue)
            {
                return false;
            }

            if (CaseOpsValue == null && other.CaseOpsValue == null)
            {
                return true;
            }

            if (CaseOpsValue == null || other.CaseOpsValue == null)
            {
                return false;
            }

            if (CaseOpsValue.Length != other.CaseOpsValue.Length)
            {
                return false;
            }

            for (int i = 0; i < CaseOpsValue.Length; i++)
            {
                if (CaseOpsValue[i] != other.CaseOpsValue[i])
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StaticValueData, MatchIndexValue, CaseOpsValue?.Length ?? 0);
    }
}

/// <summary>
/// Replace pattern implementation.
/// Ported from TypeScript: ReplacePattern class.
/// </summary>
public class ReplacePattern
{
    private readonly ReplacePatternKind _kind;
    private readonly string? _staticValue;
    private readonly ReplacePiece[]? _pieces;

    public static ReplacePattern FromStaticValue(string value)
    {
        return new ReplacePattern([ReplacePiece.StaticValue(value)]);
    }

    public bool HasReplacementPatterns => _kind == ReplacePatternKind.DynamicPieces;

    public ReplacePattern(ReplacePiece[]? pieces)
    {
        if (pieces == null || pieces.Length == 0)
        {
            _kind = ReplacePatternKind.StaticValue;
            _staticValue = string.Empty;
            _pieces = null;
        }
        else if (pieces.Length == 1 && pieces[0].StaticValueData != null)
        {
            _kind = ReplacePatternKind.StaticValue;
            _staticValue = pieces[0].StaticValueData;
            _pieces = null;
        }
        else
        {
            _kind = ReplacePatternKind.DynamicPieces;
            _staticValue = null;
            _pieces = pieces;
        }
    }

    public string BuildReplaceString(string[]? matches, bool preserveCase = false)
    {
        if (_kind == ReplacePatternKind.StaticValue)
        {
            if (preserveCase)
            {
                return BuildReplaceStringWithCasePreserved(matches, _staticValue!);
            }
            else
            {
                return _staticValue!;
            }
        }

        StringBuilder result = new();
        for (int i = 0; i < _pieces!.Length; i++)
        {
            ReplacePiece piece = _pieces[i];
            if (piece.StaticValueData != null)
            {
                // static value ReplacePiece
                result.Append(piece.StaticValueData);
                continue;
            }

            // match index ReplacePiece
            string match = Substitute(piece.MatchIndexValue, matches);
            if (piece.CaseOpsValue != null && piece.CaseOpsValue.Length > 0)
            {
                List<char> repl = [];
                int lenOps = piece.CaseOpsValue.Length;
                int opIdx = 0;
                for (int idx = 0; idx < match.Length; idx++)
                {
                    if (opIdx >= lenOps)
                    {
                        repl.AddRange(match.Substring(idx).ToCharArray());
                        break;
                    }
                    switch (piece.CaseOpsValue[opIdx])
                    {
                        case "U":
                            repl.Add(char.ToUpperInvariant(match[idx]));
                            break;
                        case "u":
                            repl.Add(char.ToUpperInvariant(match[idx]));
                            opIdx++;
                            break;
                        case "L":
                            repl.Add(char.ToLowerInvariant(match[idx]));
                            break;
                        case "l":
                            repl.Add(char.ToLowerInvariant(match[idx]));
                            opIdx++;
                            break;
                        default:
                            repl.Add(match[idx]);
                            break;
                    }
                }
                match = new string(repl.ToArray());
            }
            result.Append(match);
        }

        return result.ToString();
    }

    private static string Substitute(int matchIndex, string[]? matches)
    {
        if (matches == null)
        {
            return string.Empty;
        }
        if (matchIndex == 0)
        {
            return matches[0];
        }

        string remainder = string.Empty;
        while (matchIndex > 0)
        {
            if (matchIndex < matches.Length)
            {
                // A match can be undefined (null in C#)
                string match = matches[matchIndex] ?? string.Empty;
                return match + remainder;
            }
            remainder = (matchIndex % 10).ToString() + remainder;
            matchIndex = matchIndex / 10;
        }
        return "$" + remainder;
    }

    /// <summary>
    /// Build replace string with case preserved.
    /// Ported from TypeScript: buildReplaceStringWithCasePreserved function.
    /// </summary>
    public static string BuildReplaceStringWithCasePreserved(string[]? matches, string pattern)
    {
        if (matches != null && matches.Length > 0 && matches[0] != string.Empty)
        {
            bool containsHyphens = ValidateSpecificSpecialCharacter(matches, pattern, '-');
            bool containsUnderscores = ValidateSpecificSpecialCharacter(matches, pattern, '_');

            if (containsHyphens && !containsUnderscores)
            {
                return BuildReplaceStringForSpecificSpecialCharacter(matches, pattern, '-');
            }
            else if (!containsHyphens && containsUnderscores)
            {
                return BuildReplaceStringForSpecificSpecialCharacter(matches, pattern, '_');
            }

            string match = matches[0];

            if (match.ToUpperInvariant() == match)
            {
                return pattern.ToUpperInvariant();
            }
            else if (match.ToLowerInvariant() == match)
            {
                return pattern.ToLowerInvariant();
            }
            else if (pattern.Length > 0)
            {
                char firstMatchChar = match[0];
                char firstPatternChar = pattern[0];

                bool isFirstMatchUpper =
                    char.ToUpperInvariant(firstMatchChar) == firstMatchChar &&
                    char.ToLowerInvariant(firstMatchChar) != firstMatchChar;
                bool isFirstMatchLower =
                    char.ToLowerInvariant(firstMatchChar) == firstMatchChar &&
                    char.ToUpperInvariant(firstMatchChar) != firstMatchChar;

                if (isFirstMatchUpper)
                {
                    return char.ToUpperInvariant(firstPatternChar) + pattern.Substring(1);
                }
                else if (isFirstMatchLower)
                {
                    return char.ToLowerInvariant(firstPatternChar) + pattern.Substring(1);
                }
            }

            // we don't understand its pattern yet.
            return pattern;
        }
        else
        {
            return pattern;
        }
    }

    private static bool ValidateSpecificSpecialCharacter(string[] matches, string pattern, char specialCharacter)
    {
        bool doesContainSpecialCharacter = matches[0].IndexOf(specialCharacter) != -1
            && pattern.IndexOf(specialCharacter) != -1;
        return doesContainSpecialCharacter
            && matches[0].Split(specialCharacter).Length == pattern.Split(specialCharacter).Length;
    }

    private static string BuildReplaceStringForSpecificSpecialCharacter(string[] matches, string pattern, char specialCharacter)
    {
        string[] splitPatternAtSpecialCharacter = pattern.Split(specialCharacter);
        string[] splitMatchAtSpecialCharacter = matches[0].Split(specialCharacter);
        StringBuilder replaceString = new();

        for (int index = 0; index < splitPatternAtSpecialCharacter.Length; index++)
        {
            replaceString.Append(
                BuildReplaceStringWithCasePreserved(
                    [splitMatchAtSpecialCharacter[index]],
                    splitPatternAtSpecialCharacter[index]
                )
            );
            replaceString.Append(specialCharacter);
        }

        return replaceString.ToString().Substring(0, replaceString.Length - 1);
    }

    private static bool ContainsUppercaseCharacter(string target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return false;
        }

        foreach (char character in target)
        {
            if (char.ToUpperInvariant(character) == character && char.ToLowerInvariant(character) != character)
            {
                return true;
            }
        }

        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is ReplacePattern other)
        {
            if (_kind != other._kind)
            {
                return false;
            }

            if (_kind == ReplacePatternKind.StaticValue)
            {
                return _staticValue == other._staticValue;
            }
            else
            {
                if (_pieces == null && other._pieces == null)
                {
                    return true;
                }

                if (_pieces == null || other._pieces == null)
                {
                    return false;
                }

                if (_pieces.Length != other._pieces.Length)
                {
                    return false;
                }

                for (int i = 0; i < _pieces.Length; i++)
                {
                    if (!_pieces[i].Equals(other._pieces[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_kind, _staticValue, _pieces?.Length ?? 0);
    }
}

/// <summary>
/// Helper builder for parsing replace strings.
/// Ported from TypeScript: ReplacePieceBuilder class.
/// </summary>
internal class ReplacePieceBuilder
{
    private readonly string _source;
    private int _lastCharIndex;
    private readonly List<ReplacePiece> _result;
    private string _currentStaticPiece;

    public ReplacePieceBuilder(string source)
    {
        _source = source;
        _lastCharIndex = 0;
        _result = [];
        _currentStaticPiece = string.Empty;
    }

    public void EmitUnchanged(int toCharIndex)
    {
        EmitStatic(_source.Substring(_lastCharIndex, toCharIndex - _lastCharIndex));
        _lastCharIndex = toCharIndex;
    }

    public void EmitStatic(string value, int toCharIndex)
    {
        EmitStatic(value);
        _lastCharIndex = toCharIndex;
    }

    private void EmitStatic(string value)
    {
        if (value.Length == 0)
        {
            return;
        }
        _currentStaticPiece += value;
    }

    public void EmitMatchIndex(int index, int toCharIndex, List<string> caseOps)
    {
        if (_currentStaticPiece.Length != 0)
        {
            _result.Add(ReplacePiece.StaticValue(_currentStaticPiece));
            _currentStaticPiece = string.Empty;
        }
        _result.Add(ReplacePiece.CaseOps(index, caseOps.ToArray()));
        _lastCharIndex = toCharIndex;
    }

    public ReplacePattern Finalize()
    {
        EmitUnchanged(_source.Length);
        if (_currentStaticPiece.Length != 0)
        {
            _result.Add(ReplacePiece.StaticValue(_currentStaticPiece));
            _currentStaticPiece = string.Empty;
        }
        return new ReplacePattern(_result.ToArray());
    }
}

/// <summary>
/// Parse a replace string and build a ReplacePattern.
/// Ported from TypeScript: parseReplaceString function.
/// 
/// Supported patterns:
/// \n           => inserts a LF
/// \t           => inserts a TAB
/// \\           => inserts a "\".
/// \u           => upper-cases one character in a match.
/// \U           => upper-cases ALL remaining characters in a match.
/// \l           => lower-cases one character in a match.
/// \L           => lower-cases ALL remaining characters in a match.
/// $$           => inserts a "$".
/// $&amp; and $0    => inserts the matched substring.
/// $n           => Where n is a non-negative integer lesser than 100, inserts the nth parenthesized submatch string
/// </summary>
public static class ReplacePatternParser
{
    private enum CharCode
    {
        Backslash = 92,    // '\'
        DollarSign = 36,   // '$'
        Ampersand = 38,    // '&'
        Digit0 = 48,       // '0'
        Digit1 = 49,       // '1'
        Digit9 = 57,       // '9'
        n = 110,           // 'n'
        t = 116,           // 't'
        u = 117,           // 'u'
        U = 85,            // 'U'
        l = 108,           // 'l'
        L = 76             // 'L'
    }

    public static ReplacePattern ParseReplaceString(string? replaceString)
    {
        if (string.IsNullOrEmpty(replaceString))
        {
            return new ReplacePattern(null);
        }

        List<string> caseOps = [];
        ReplacePieceBuilder result = new(replaceString);

        for (int i = 0; i < replaceString.Length; i++)
        {
            int chCode = replaceString[i];

            if (chCode == (int)CharCode.Backslash)
            {
                // move to next char
                i++;

                if (i >= replaceString.Length)
                {
                    // string ends with a \
                    break;
                }

                int nextChCode = replaceString[i];

                switch (nextChCode)
                {
                    case (int)CharCode.Backslash:
                        // \\ => inserts a "\"
                        result.EmitUnchanged(i - 1);
                        result.EmitStatic("\\", i + 1);
                        break;
                    case (int)CharCode.n:
                        // \n => inserts a LF
                        result.EmitUnchanged(i - 1);
                        result.EmitStatic("\n", i + 1);
                        break;
                    case (int)CharCode.t:
                        // \t => inserts a TAB
                        result.EmitUnchanged(i - 1);
                        result.EmitStatic("\t", i + 1);
                        break;
                    case (int)CharCode.u:
                    case (int)CharCode.U:
                    case (int)CharCode.l:
                    case (int)CharCode.L:
                        // Case modification operations
                        result.EmitUnchanged(i - 1);
                        result.EmitStatic(string.Empty, i + 1);
                        caseOps.Add(((char)nextChCode).ToString());
                        break;
                }

                continue;
            }

            if (chCode == (int)CharCode.DollarSign)
            {
                // move to next char
                i++;

                if (i >= replaceString.Length)
                {
                    // string ends with a $
                    break;
                }

                int nextChCode = replaceString[i];

                if (nextChCode == (int)CharCode.DollarSign)
                {
                    // $$ => inserts a "$"
                    result.EmitUnchanged(i - 1);
                    result.EmitStatic("$", i + 1);
                    continue;
                }

                if (nextChCode == (int)CharCode.Digit0 || nextChCode == (int)CharCode.Ampersand)
                {
                    // $& and $0 => inserts the matched substring.
                    result.EmitUnchanged(i - 1);
                    result.EmitMatchIndex(0, i + 1, caseOps);
                    caseOps.Clear();
                    continue;
                }

                if (nextChCode >= (int)CharCode.Digit1 && nextChCode <= (int)CharCode.Digit9)
                {
                    // $n
                    int matchIndex = nextChCode - (int)CharCode.Digit0;

                    // peek next char to probe for $nn
                    if (i + 1 < replaceString.Length)
                    {
                        int nextNextChCode = replaceString[i + 1];
                        if (nextNextChCode >= (int)CharCode.Digit0 && nextNextChCode <= (int)CharCode.Digit9)
                        {
                            // $nn
                            // move to next char
                            i++;
                            matchIndex = (matchIndex * 10) + (nextNextChCode - (int)CharCode.Digit0);

                            result.EmitUnchanged(i - 2);
                            result.EmitMatchIndex(matchIndex, i + 1, caseOps);
                            caseOps.Clear();
                            continue;
                        }
                    }

                    result.EmitUnchanged(i - 1);
                    result.EmitMatchIndex(matchIndex, i + 1, caseOps);
                    caseOps.Clear();
                    continue;
                }
            }
        }

        return result.Finalize();
    }
}
