// Source: ts/src/vs/editor/contrib/snippet/browser/snippetParser.ts
// - Class: FormatString (Lines: 362-438)
// - Class: Transform (Lines: 316-361)
// Ported: 2025-12-05 (Direct translation from TypeScript)

using System.Text.RegularExpressions;

namespace PieceTree.TextBuffer.Snippet;

/// <summary>
/// Represents a format string used in snippet transformations.
/// Supports shorthand functions (upcase, downcase, capitalize, pascalcase, camelcase)
/// and conditional replacements (if, else, if-else).
/// 
/// Syntax:
/// - $n - simple capture group reference
/// - ${n} - capture group reference with braces
/// - ${n:/upcase} - uppercase transformation
/// - ${n:/downcase} - lowercase transformation  
/// - ${n:/capitalize} - capitalize first letter
/// - ${n:/pascalcase} - PascalCase transformation
/// - ${n:/camelcase} - camelCase transformation
/// - ${n:+if} - if capture group matched, use "if" value
/// - ${n:-else} - if capture group NOT matched, use "else" value
/// - ${n:?if:else} - if capture group matched use "if", else use "else"
/// 
/// Based on TS FormatString class (snippetParser.ts L362-438).
/// </summary>
public sealed class FormatString : Marker
{
    /// <summary>
    /// The capture group index (0-based: $0, $1, $2, etc.)
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The shorthand transformation name: upcase, downcase, capitalize, pascalcase, camelcase.
    /// Null if no shorthand is specified.
    /// </summary>
    public string? ShorthandName { get; }

    /// <summary>
    /// The value to use if the capture group matched (for conditional replacements).
    /// Used in ${n:+if} and ${n:?if:else} syntax.
    /// </summary>
    public string? IfValue { get; }

    /// <summary>
    /// The value to use if the capture group did NOT match (for conditional replacements).
    /// Used in ${n:-else} and ${n:?if:else} syntax.
    /// </summary>
    public string? ElseValue { get; }

    /// <summary>
    /// Creates a new FormatString.
    /// </summary>
    /// <param name="index">The capture group index.</param>
    /// <param name="shorthandName">Optional shorthand transformation name.</param>
    /// <param name="ifValue">Optional if-value for conditional replacement.</param>
    /// <param name="elseValue">Optional else-value for conditional replacement.</param>
    public FormatString(int index, string? shorthandName = null, string? ifValue = null, string? elseValue = null)
    {
        Index = index;
        ShorthandName = shorthandName;
        IfValue = ifValue;
        ElseValue = elseValue;
    }

    /// <summary>
    /// Resolves the format string with a given capture group value.
    /// </summary>
    /// <param name="value">The captured value (can be null or empty if group didn't match).</param>
    /// <returns>The resolved string.</returns>
    public string Resolve(string? value)
    {
        // TS: FormatString.resolve() (snippetParser.ts L375-394)
        if (ShorthandName == "upcase")
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.ToUpperInvariant();
        }
        else if (ShorthandName == "downcase")
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.ToLowerInvariant();
        }
        else if (ShorthandName == "capitalize")
        {
            // Handle empty, single-char, and multi-char strings
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Length == 1) return value.ToUpperInvariant();
            return char.ToUpperInvariant(value[0]) + value[1..];
        }
        else if (ShorthandName == "pascalcase")
        {
            return string.IsNullOrEmpty(value) ? string.Empty : ToPascalCase(value);
        }
        else if (ShorthandName == "camelcase")
        {
            return string.IsNullOrEmpty(value) ? string.Empty : ToCamelCase(value);
        }
        else if (!string.IsNullOrEmpty(value) && IfValue is not null)
        {
            // Has value and ifValue is defined -> return ifValue
            return IfValue;
        }
        else if (string.IsNullOrEmpty(value) && ElseValue is not null)
        {
            // No value and elseValue is defined -> return elseValue
            return ElseValue;
        }
        else
        {
            return value ?? string.Empty;
        }
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// "bar-foo" -> "BarFoo"
    /// "snake_case" -> "SnakeCase"
    /// </summary>
    private static string ToPascalCase(string value)
    {
        // TS: FormatString._toPascalCase() (snippetParser.ts L396-406)
        // Match alphanumeric sequences
        MatchCollection matches = Regex.Matches(value, @"[a-z0-9]+", RegexOptions.IgnoreCase);
        if (matches.Count == 0)
        {
            return value;
        }

        return string.Concat(matches.Select(m =>
            char.ToUpperInvariant(m.Value[0]) + m.Value[1..]));
    }

    /// <summary>
    /// Converts a string to camelCase.
    /// "bar-foo" -> "barFoo"
    /// "snake_case" -> "snakeCase"
    /// </summary>
    private static string ToCamelCase(string value)
    {
        // TS: FormatString._toCamelCase() (snippetParser.ts L408-420)
        // Match alphanumeric sequences
        MatchCollection matches = Regex.Matches(value, @"[a-z0-9]+", RegexOptions.IgnoreCase);
        if (matches.Count == 0)
        {
            return value;
        }

        return string.Concat(matches.Select((m, index) =>
        {
            if (index == 0)
            {
                return char.ToLowerInvariant(m.Value[0]) + m.Value[1..];
            }
            return char.ToUpperInvariant(m.Value[0]) + m.Value[1..];
        }));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Empty;
    }

    /// <inheritdoc />
    public override string ToTextmateString()
    {
        // TS: FormatString.toTextmateString() (snippetParser.ts L422-435)
        string result = "${" + Index;

        if (ShorthandName is not null)
        {
            result += ":/" + ShorthandName;
        }
        else if (IfValue is not null && ElseValue is not null)
        {
            result += ":?" + IfValue + ":" + ElseValue;
        }
        else if (IfValue is not null)
        {
            result += ":+" + IfValue;
        }
        else if (ElseValue is not null)
        {
            result += ":-" + ElseValue;
        }

        result += "}";
        return result;
    }

    /// <inheritdoc />
    public override Marker Clone()
    {
        // TS: FormatString.clone() (snippetParser.ts L437-438)
        return new FormatString(Index, ShorthandName, IfValue, ElseValue);
    }
}

/// <summary>
/// Represents a regex-based transformation applied to placeholder/variable values.
/// A Transform contains a regex pattern and format string children that determine
/// how matched text is replaced.
/// 
/// Syntax: ${var/regex/format/flags}
/// Example: ${TM_FILENAME/(.*)\\..+$/$1/} - extracts filename without extension
/// 
/// Based on TS Transform class (snippetParser.ts L316-361).
/// </summary>
public sealed class Transform : Marker
{
    /// <summary>
    /// The regex pattern used for matching.
    /// </summary>
    public Regex Regexp { get; set; } = new Regex(string.Empty);

    /// <summary>
    /// Resolves the transformation by applying the regex to the input value
    /// and replacing matches using the format string children.
    /// </summary>
    /// <param name="value">The input value to transform.</param>
    /// <returns>The transformed string.</returns>
    public string Resolve(string value)
    {
        // TS: Transform.resolve() (snippetParser.ts L320-335)
        bool didMatch = false;

        string result = Regexp.Replace(value, match =>
        {
            didMatch = true;
            // Build groups array: [fullMatch, group1, group2, ...]
            string[] groups = new string[match.Groups.Count];
            for (int i = 0; i < match.Groups.Count; i++)
            {
                groups[i] = match.Groups[i].Value;
            }
            return Replace(groups);
        });

        // TS: when the regex didn't match and when the transform has
        // else branches, then run those
        if (!didMatch && Children.Any(child => child is FormatString fs && fs.ElseValue is not null))
        {
            result = Replace([]);
        }

        return result;
    }

    /// <summary>
    /// Performs the replacement using the format string children.
    /// </summary>
    /// <param name="groups">The captured groups from the regex match.</param>
    /// <returns>The replacement string.</returns>
    private string Replace(string[] groups)
    {
        // TS: Transform._replace() (snippetParser.ts L337-348)
        string result = string.Empty;

        foreach (Marker marker in Children)
        {
            if (marker is FormatString formatString)
            {
                string capturedValue = formatString.Index < groups.Length
                    ? groups[formatString.Index]
                    : string.Empty;
                result += formatString.Resolve(capturedValue);
            }
            else
            {
                result += marker.ToString();
            }
        }

        return result;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        // TS: Transform.toString() (snippetParser.ts L350-352)
        return string.Empty;
    }

    /// <inheritdoc />
    public override string ToTextmateString()
    {
        // TS: Transform.toTextmateString() (snippetParser.ts L354-356)
        string flags = (Regexp.Options.HasFlag(RegexOptions.IgnoreCase) ? "i" : string.Empty)
                     + (IsGlobal() ? "g" : string.Empty);
        string childrenStr = string.Concat(Children.Select(c => c.ToTextmateString()));
        // Note: Regex.ToString() returns the pattern string (e.g., "^(.)|-(.)")
        return $"/{Regexp}/{childrenStr}/{flags}";
    }

    /// <inheritdoc />
    public override Marker Clone()
    {
        // TS: Transform.clone() (snippetParser.ts L358-361)
        Transform ret = new()
        {
            // Note: Regex.ToString() returns the pattern, which we use to create a new Regex
            Regexp = new Regex(
                Regexp.ToString(),
                (Regexp.Options.HasFlag(RegexOptions.IgnoreCase) ? RegexOptions.IgnoreCase : RegexOptions.None))
        };

        foreach (Marker child in Children)
        {
            ret.AppendChild(child.Clone());
        }

        return ret;
    }

    /// <summary>
    /// Checks if the regex is global (i.e., replaces all matches).
    /// In .NET, this is controlled by Regex.Replace vs ReplaceFirst.
    /// We track this as a property since .NET doesn't have a 'g' flag.
    /// </summary>
    private bool IsGlobal()
    {
        // Note: In TS, regex has .global property. In .NET, Regex.Replace replaces all by default.
        // For now, we assume global replacement. A more complete implementation would
        // track this separately.
        return true;
    }
}

/// <summary>
/// Base class for snippet AST markers (Text, Placeholder, Variable, Transform, etc.)
/// This is a simplified version for Transform/FormatString; the full implementation
/// would include all marker types.
/// 
/// Based on TS Marker class (snippetParser.ts L120-175).
/// </summary>
public abstract class Marker
{
    /// <summary>
    /// Parent marker in the AST.
    /// </summary>
    public Marker? Parent { get; set; }

    /// <summary>
    /// Child markers.
    /// </summary>
    protected List<Marker> _children = [];

    /// <summary>
    /// Gets the children of this marker.
    /// </summary>
    public IReadOnlyList<Marker> Children => _children;

    /// <summary>
    /// Appends a child marker. If both this and the last child are Text markers,
    /// they are merged.
    /// </summary>
    /// <param name="child">The child to append.</param>
    /// <returns>This marker for chaining.</returns>
    public Marker AppendChild(Marker child)
    {
        // TS: Marker.appendChild() (snippetParser.ts L128-140)
        if (child is Text textChild && _children.Count > 0 && _children[^1] is Text lastText)
        {
            // Merge adjacent text nodes
            lastText.Value += textChild.Value;
        }
        else
        {
            child.Parent = this;
            _children.Add(child);
        }
        return this;
    }

    /// <summary>
    /// Replaces a child marker with a list of other markers.
    /// </summary>
    /// <param name="child">The child to replace.</param>
    /// <param name="others">The replacement markers.</param>
    public void Replace(Marker child, IEnumerable<Marker> others)
    {
        // TS: Marker.replace() (snippetParser.ts L142-155)
        Marker? parent = child.Parent;
        if (parent == null) return;

        int idx = parent._children.IndexOf(child);
        if (idx < 0) return;

        List<Marker> othersList = others.ToList();
        parent._children.RemoveAt(idx);
        parent._children.InsertRange(idx, othersList);

        // Fix parent references
        FixParent(othersList, parent);
    }

    private static void FixParent(IEnumerable<Marker> children, Marker parent)
    {
        foreach (Marker child in children)
        {
            child.Parent = parent;
            FixParent(child.Children, child);
        }
    }

    /// <summary>
    /// Returns the string representation of this marker and its children.
    /// </summary>
    public override string ToString()
    {
        // TS: Marker.toString() (snippetParser.ts L168-170)
        return string.Concat(Children.Select(c => c.ToString()));
    }

    /// <summary>
    /// Returns the TextMate snippet string representation.
    /// </summary>
    public abstract string ToTextmateString();

    /// <summary>
    /// Returns the length of this marker's text content.
    /// </summary>
    public virtual int Len()
    {
        // TS: Marker.len() (snippetParser.ts L172-174)
        return 0;
    }

    /// <summary>
    /// Creates a deep clone of this marker.
    /// </summary>
    public abstract Marker Clone();
}

/// <summary>
/// Represents plain text in a snippet.
/// Based on TS Text class (snippetParser.ts L177-205).
/// </summary>
public sealed class Text : Marker
{
    /// <summary>
    /// The text value.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Creates a new Text marker.
    /// </summary>
    /// <param name="value">The text value.</param>
    public Text(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Escapes special characters in text for TextMate snippet syntax.
    /// </summary>
    public static string Escape(string value)
    {
        // TS: Text.escape() (snippetParser.ts L179-181)
        return Regex.Replace(value, @"[\$}\|\\]", "\\$&");
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }

    /// <inheritdoc />
    public override string ToTextmateString()
    {
        // TS: Text.toTextmateString() (snippetParser.ts L191-193)
        return Escape(Value);
    }

    /// <inheritdoc />
    public override int Len()
    {
        // TS: Text.len() (snippetParser.ts L195-197)
        return Value.Length;
    }

    /// <inheritdoc />
    public override Marker Clone()
    {
        // TS: Text.clone() (snippetParser.ts L199-201)
        return new Text(Value);
    }
}
