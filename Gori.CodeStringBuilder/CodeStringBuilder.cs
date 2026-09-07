using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Gori.CodeStringBuilder;

/// <summary>
/// A simple class for helping output text with line indentations and variables.
/// This is not a full re-formatter, just basic helper for use in source generators.
/// </summary>
public class CodeStringBuilder
{
    /// <summary>
    /// If this char is seen at end of a line it will increase the indent depth, including for this line.
    /// </summary>
    /// <remarks>
    /// Depends on <see cref="Gori.CodeStringBuilder.IndentControl"/> that is used.
    /// <list type="table">
    /// <item>
    /// <term><see cref="IndentControl.FullAuto"/></term>
    /// <description>Checked for at end of each line in text</description>
    /// </item>
    /// <item>
    /// <term><see cref="IndentControl.Auto"/></term>
    /// <description>Checked for at end of last line in text</description>
    /// </item>
    /// <item>
    /// <term><see cref="IndentControl.None"/></term>
    /// <description>Not used</description>
    /// </item>
    /// </list>
    /// </remarks>
    public const char IndentIncreasePre = '\x01';

    /// <summary>
    /// If this char is seen at end of a line it will increase the indent depth, for the next line.
    /// </summary>
    /// <inheritdoc cref="IndentIncreasePre"/>
    public const char IndentIncreasePost = '\x02';

    /// <summary>
    /// If this char is seen at end of a line it will decrease the indent depth, including for this line.
    /// </summary>
    /// <inheritdoc cref="IndentIncreasePre"/>
    public const char IndentDecreasePre = '\x03';

    /// <summary>
    /// If this char is seen at end of a line it will decrease the indent depth, for the next line.
    /// </summary>
    /// <inheritdoc cref="IndentIncreasePre"/>
    public const char IndentDecreasePost = '\x04';

    /// <summary>
    /// If this char is seen at end of a line it will force a blank line prior to this line if one doesn't already exist.
    /// </summary>
    /// <inheritdoc cref="IndentIncreasePre"/>
    public const char BlankLinePre = '\x1D';

    /// <summary>
    /// If this char is seen at end of a line it will force a blank line after this line if not added by this or next line.
    /// </summary>
    /// <inheritdoc cref="IndentIncreasePre"/>
    public const char BlankLinePost = '\x1E';

    /// <summary>
    /// Alias for <see cref="IndentIncreasePost"/>.
    /// </summary>
    public const char Increase = IndentIncreasePost;

    /// <summary>
    /// Alias for <see cref="IndentDecreasePre"/>.
    /// </summary>
    public const char Decrease = IndentDecreasePre;

    private readonly StringBuilder stringBuilder = new();
    private bool pendingBlankLine;
    private bool lastLineWasBlank;
    private Regex? variableRegex = new(@"\$\{(?<name>[a-zA-Z0-9_]+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Gets char used for indents.
    /// </summary>
    public char IndentChar { get; init; } = ' ';

    /// <summary>
    /// Gets how to apply indents by default. Can be overridden using <see cref="WriteLine(string?, IndentControl)"/> if required.
    /// </summary>
    public IndentControl IndentControl { get; init; } = IndentControl.Auto;

    /// <summary>
    /// Gets default variables to use when adding string with variables.
    /// NOTE: If you want variables to be case-insensitive, use a case-insensitive dictionary.
    ///
    /// These will be used if no matching variable is found in the variables parameter (missing key or is null) of Write methods.
    /// </summary>
    public IReadOnlyDictionary<string, string>? DefaultVariables { get; init; }

    /// <summary>
    /// Gets pattern to use when adding string with variables.
    /// Default is ${name} where name can be any combination of a-z, A-Z, 0-9 and _.
    ///
    /// Set to null to disable variable replacement.
    ///
    /// NOTE: MUST contain a named group "name" that will be used to lookup the variable in the dictionary.
    /// </summary>
    public string? VariablePattern
    {
        get => variableRegex?.ToString();

        init => variableRegex = value is not null ? new Regex(value, RegexOptions.Compiled | RegexOptions.CultureInvariant) : null;
    }

    /// <summary>
    /// Gets or sets current indent depth. Actual indent added will be Indents x IndentSize.
    /// </summary>
    public int Indents
    {
        get;

        set
        {
            field = value < 0 ? 0 : value;
            CurrentIndent = null!;
        }
    }

    /// <summary>
    /// Gets number of characters to add per Indent.
    /// </summary>
    public int IndentSize { get; init; } = 4;

    /// <summary>
    /// Gets char that if line starts with, no indentation will be applied.
    /// Note: Char is still output and control chars at end of line will still be processed and removed.
    /// Default: '\0' which disables this feature.
    /// </summary>
    public char NoIndentChar { get; init; }

    /// <summary>
    /// Gets char that if line ends with and only other characters are whitespace, will auto increase indent depth after writing.
    /// Default: '\0' which disables this feature.
    /// </summary>
    /// <remarks>
    /// Depends on <see cref="Gori.CodeStringBuilder.IndentControl"/> that is used.
    /// <list type="table">
    /// <item>
    /// <term><see cref="IndentControl.FullAuto"/></term>
    /// <description>Checked for on each line in text</description>
    /// </item>
    /// <item>
    /// <term><see cref="IndentControl.Auto"/></term>
    /// <description>Checked for last line in text</description>
    /// </item>
    /// <item>
    /// <term><see cref="IndentControl.None"/></term>
    /// <description>Not used</description>
    /// </item>
    /// </list>
    /// </remarks>
    public char ForceIndentOn { get; init; }

    /// <summary>
    /// If line ends with this char and only other characters are whitespace, will auto decrease indent depth before writing.
    /// Default: '\0' which disables this feature.
    /// </summary>
    /// <inheritdoc cref="ForceIndentOn"/>
    public char ForceIndentOff { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="ForceIndentOff"/> char should force a blank line after it.
    /// Will only add if another line is written and it isn't a blank line.
    /// </summary>
    public bool ForceBlankLineAfterOff { get; init; }

    /// <summary>
    /// Gets or sets the cached current indent string.
    /// Set to null! to clear cache, and have it recalculated next call to this property.
    /// </summary>
    private string CurrentIndent
    {
        get => field ??= new string(IndentChar, Indents * IndentSize);

        set;
    }

    /// <summary>
    /// Constructs a new CodeStringBuilder configured for C#.
    /// </summary>
    /// <param name="defaultVariables">Default variables to use in the builder.</param>
    /// <param name="fullAuto">Default IndentControl is <see cref="IndentControl.Auto"/>. Setting this to true will make it <see cref="IndentControl.FullAuto"/>.</param>
    /// <returns>CodeStringBuilder configured for C#.</returns>
    public static CodeStringBuilder CreateCSharpBuilder(IReadOnlyDictionary<string, string>? defaultVariables = null, bool fullAuto = false) => new()
    {
        IndentSize = 4,
        IndentChar = ' ',
        IndentControl = fullAuto ? IndentControl.FullAuto : IndentControl.Auto,
        ForceIndentOn = '{',
        ForceIndentOff = '}',
        ForceBlankLineAfterOff = true,
        NoIndentChar = '#',
        DefaultVariables = defaultVariables,
    };

    /// <summary>
    /// Returns string containing all code written to this instance.
    /// </summary>
    /// <returns>Contents written to string builder.</returns>
    public override string ToString() => stringBuilder.ToString();

    /// <summary>
    /// Clears current string builder and resets Indents to 0.
    /// </summary>
    public void Clear()
    {
        _ = stringBuilder.Clear();
        Indents = 0;
        pendingBlankLine = false;
        lastLineWasBlank = false;
    }

    /// <summary>
    /// Makes sure there is a blank line between previous written line and next. Will not add blank line if:
    ///  - Nothing has been written yet
    ///  - Nothing more is written
    ///  - Blank line is added by either previous or next WriteLine calls.
    /// </summary>
    /// <returns>This instance to allow chaining.</returns>
    public CodeStringBuilder ForceBlankLine()
    {
        pendingBlankLine = true;
        return this;
    }

    /// <summary>
    /// Write blank line.
    /// </summary>
    /// <returns>This instance to allow chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLine()
        => WriteLine(null, null, IndentControl);

    /// <inheritdoc cref="WriteLine(string?, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLine(string? text)
        => WriteLine(text, null, IndentControl);

    /// <inheritdoc cref="WriteLine(string?, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLine(string? text, IReadOnlyDictionary<string, string>? variables)
        => WriteLine(text, variables, IndentControl);

    /// <summary>
    /// Write line(s) if <paramref name="doWrite"/> is true.
    /// </summary>
    /// <param name="doWrite">
    /// If false, will skip this write. Useful when chaining commands,
    /// instead of breaking chain to insert if statement.
    /// </param>
    /// <param name="text">Line or lines to output.</param>
    /// <param name="variables">
    /// Dictionary of variable names and their replacement values.
    /// Variables in the text are replaced using the pattern defined by <see cref="VariablePattern"/>.
    /// NOTE: If you want variables to be case-insensitive, use a case-insensitive dictionary.
    ///
    /// Any VariablePattern without a matching key in this dictionary and <see cref="DefaultVariables"/> will be left unchanged in the output.
    /// </param>
    /// <param name="indentControl">Controls how indentation is applied. Overrides the instance default for this call.</param>
    /// <returns>This instance to allow chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(bool doWrite, Func<string?> text, IReadOnlyDictionary<string, string>? variables, IndentControl indentControl)
        => doWrite ? WriteLine(text(), variables, indentControl) : this;

    /// <inheritdoc cref="WriteLineIf(bool, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(bool doWrite, string? text)
        => doWrite ? WriteLine(text, null, IndentControl) : this;

    /// <inheritdoc cref="WriteLineIf(bool, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(bool doWrite, string? text, IReadOnlyDictionary<string, string>? variables)
        => doWrite ? WriteLine(text, variables, IndentControl) : this;

    /// <inheritdoc cref="WriteLineIf(bool, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(bool doWrite, Func<string?> text)
        => doWrite ? WriteLine(text(), null, IndentControl) : this;

    /// <inheritdoc cref="WriteLineIf(bool, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(bool doWrite, Func<string?> text, IReadOnlyDictionary<string, string>? variables)
        => doWrite ? WriteLine(text(), variables, IndentControl) : this;

    /// <inheritdoc cref="WriteLineIf(bool, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(bool doWrite, string? text, IndentControl indentControl)
        => doWrite ? WriteLine(text, null, indentControl) : this;

    /// <inheritdoc cref="WriteLineIf(bool, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(bool doWrite, string? text, IReadOnlyDictionary<string, string>? variables, IndentControl indentControl)
        => doWrite ? WriteLine(text, variables, indentControl) : this;

    /// <inheritdoc cref="WriteLineIf(bool, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(bool doWrite, Func<string?> text, IndentControl indentControl)
        => doWrite ? WriteLine(text(), null, indentControl) : this;

    /// <summary>
    /// Write line(s) if <paramref name="predicate"/> is true.
    /// </summary>
    /// <param name="predicate">
    /// If returns false, will skip this write. Useful when chaining commands,
    /// instead of breaking chain to insert if statement.
    /// </param>
    /// <param name="text">Line or lines to output.</param>
    /// <param name="variables">
    /// Dictionary of variable names and their replacement values.
    /// Variables in the text are replaced using the pattern defined by <see cref="VariablePattern"/>.
    /// NOTE: If you want variables to be case-insensitive, use a case-insensitive dictionary.
    ///
    /// Any VariablePattern without a matching key in this dictionary and <see cref="DefaultVariables"/> will be left unchanged in the output.
    /// </param>
    /// <param name="indentControl">Controls how indentation is applied. Overrides the instance default for this call.</param>
    /// <returns>This instance to allow chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(Func<bool> predicate, Func<string?> text, IReadOnlyDictionary<string, string>? variables, IndentControl indentControl)
        => predicate() ? WriteLine(text(), variables, indentControl) : this;

    /// <inheritdoc cref="WriteLineIf(Func{bool}, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(Func<bool> predicate, string? text)
        => predicate() ? WriteLine(text, null, IndentControl) : this;

    /// <inheritdoc cref="WriteLineIf(Func{bool}, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(Func<bool> predicate, string? text, IReadOnlyDictionary<string, string>? variables)
        => predicate() ? WriteLine(text, variables, IndentControl) : this;

    /// <inheritdoc cref="WriteLineIf(Func{bool}, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(Func<bool> predicate, Func<string?> text, IReadOnlyDictionary<string, string>? variables)
        => predicate() ? WriteLine(text(), variables, IndentControl) : this;

    /// <inheritdoc cref="WriteLineIf(Func{bool}, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(Func<bool> predicate, Func<string?> text)
        => predicate() ? WriteLine(text(), null, IndentControl) : this;

    /// <inheritdoc cref="WriteLineIf(Func{bool}, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(Func<bool> predicate, string? text, IndentControl indentControl)
        => predicate() ? WriteLine(text, null, indentControl) : this;

    /// <inheritdoc cref="WriteLineIf(Func{bool}, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(Func<bool> predicate, string? text, IReadOnlyDictionary<string, string>? variables, IndentControl indentControl)
        => predicate() ? WriteLine(text, variables, indentControl) : this;

    /// <inheritdoc cref="WriteLineIf(Func{bool}, Func{string?}, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLineIf(Func<bool> predicate, Func<string?> text, IndentControl indentControl)
        => predicate() ? WriteLine(text(), null, indentControl) : this;

    /// <inheritdoc cref="WriteLine(string?, IReadOnlyDictionary{string, string}?, IndentControl)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CodeStringBuilder WriteLine(string? text, IndentControl indentControl)
     => WriteLine(text, null, indentControl);

    /// <summary>
    /// Write line(s).
    /// </summary>
    /// <param name="text">Line or lines to output.</param>
    /// <param name="variables">
    /// Dictionary of variable names and their replacement values.
    /// Variables in the text are replaced using the pattern defined by <see cref="VariablePattern"/>.
    /// NOTE: If you want variables to be case-insensitive, use a case-insensitive dictionary.
    ///
    /// Any VariablePattern without a matching key in this dictionary and <see cref="DefaultVariables"/> will be left unchanged in the output.
    /// </param>
    /// <param name="indentControl">Controls how indentation is applied. Overrides the instance default for this call.</param>
    /// <returns>This instance to allow chaining.</returns>
    public CodeStringBuilder WriteLine(string? text, IReadOnlyDictionary<string, string>? variables, IndentControl indentControl)
    {
        if (text is null || text.Length == 0)
        {
            pendingBlankLine = false;
            lastLineWasBlank = true;
            _ = stringBuilder.AppendLine();
            return this;
        }

        if (variableRegex is not null && (DefaultVariables is not null || variables is not null))
        {
            text = variableRegex.Replace(text, match =>
            {
                string varName = match.Groups["name"].Value;
                return variables is not null && variables.TryGetValue(varName, out string? replacement) ? replacement
                     : DefaultVariables is not null && DefaultVariables.TryGetValue(varName, out replacement) ? replacement
                     : match.Value;
            });
        }

        switch (indentControl)
        {
            case IndentControl.BlankLineOnly:
                if (FirstLineIsWhitespace(text))
                {
                    pendingBlankLine = false;
                }
                else
                {
                    DoPendingBlankLine();
                }

                _ = stringBuilder.AppendLine(text);
                lastLineWasBlank = LineStartsOnlyWithWhitespace(text, text.Length - 1);
                break;

            case IndentControl.Auto:
            {
                string[] lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);

                // Process control chars on the last line only, updating it in-place.
                // For single-line: bracket detection applies (processBrackets=true).
                // For multi-line: brackets are skipped; ForceIndentOn is handled separately.
                // We detect which path first, then run processControlChars accordingly.

                // See if multiple non-blank lines and note first non-blank line
                int firstContentLine = -1;
                bool hasMultipleLines = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                    {
                        if (firstContentLine == -1)
                        {
                            firstContentLine = i;
                            continue;
                        }

                        hasMultipleLines = true;
                        break;
                    }
                }

                // Run processControlChars on the last line, updating it in-place.
                int lastIdx = lines.Length - 1;
                ReadOnlySpan<char> lastLineSpan = ProcessControlChars(
                    lines[lastIdx],
                    out int postWriteChange,
                    out bool triggeredOff,
                    out bool triggeredBlankPost,
                    processBrackets: !hasMultipleLines);

                if (lastLineSpan.Length != lines[lastIdx].Length)
                {
                    lines[lastIdx] = lastLineSpan.ToString();
                }

                if (!hasMultipleLines)
                {
                    // ── Single non-blank line: bracket detection ──
                    if (pendingBlankLine && firstContentLine == 0)
                    {
                        ReadOnlySpan<char> firstLine = lines[0];
                        bool lineWillTriggerOff = ForceIndentOff != '\0'
                            && firstLine[^1] == ForceIndentOff
                            && LineStartsOnlyWithWhitespace(firstLine, firstLine.Length - 2);

                        if (!lineWillTriggerOff)
                        {
                            DoPendingBlankLine();
                        }
                    }

                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            pendingBlankLine = false;
                            lastLineWasBlank = true;
                            _ = stringBuilder.AppendLine();
                            continue;
                        }

                        if (line[0] == NoIndentChar && NoIndentChar != '\0')
                        {
                            lastLineWasBlank = false;
                            _ = stringBuilder.AppendLine(line);
                            continue;
                        }

                        lastLineWasBlank = false;
                        _ = stringBuilder.Append(CurrentIndent)
                                          .AppendLine(line);
                    }

                    DoPostWriteChanges(postWriteChange, triggeredOff, triggeredBlankPost);
                }
                else
                {
                    // ── Multi-line: delta-based indent tracking, no bracket detection ──

                    // Base indent: first eligible (non-blank, non-NoIndentChar) line.
                    int baseIndents = 0;
                    foreach (string l in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(l) && !(NoIndentChar != '\0' && l[0] == NoIndentChar))
                        {
                            baseIndents = CountIndents(l);
                            break;
                        }
                    }

                    // Last eligible line indent for delta calculation.
                    int lastLineIndents = baseIndents;
                    string lastEligibleLine = string.Empty;
                    for (int i = lines.Length - 1; i >= 0; i--)
                    {
                        string l = lines[i];
                        if (!string.IsNullOrWhiteSpace(l) && !(NoIndentChar != '\0' && l[0] == NoIndentChar))
                        {
                            lastLineIndents = CountIndents(l);
                            lastEligibleLine = l;
                            break;
                        }
                    }

                    // If the last eligible line ends with ForceIndentOn (standalone),
                    // queue an extra post-write increment — processBrackets=false means
                    // processControlChars never sees it.
                    bool lastLineTriggersOn = ForceIndentOn != '\0'
                            && lastEligibleLine.Length > 0
                            && lastEligibleLine[^1] == ForceIndentOn
                            && LineStartsOnlyWithWhitespace(lastEligibleLine, lastEligibleLine.Length - 2);

                    // Similarly detect ForceIndentOff on the last eligible line so
                    // ForceBlankLineAfterOff is honoured even with processBrackets=false.
                    bool lastLineTriggersOff = ForceIndentOff != '\0'
                            && lastEligibleLine.Length > 0
                            && lastEligibleLine[^1] == ForceIndentOff
                            && LineStartsOnlyWithWhitespace(lastEligibleLine, lastEligibleLine.Length - 2);

                    if (baseIndents > 0)
                    {
                        if (baseIndents > Indents)
                        {
                            throw new ArgumentException($"Indents ({baseIndents}) on first line exceed current indent depth");
                        }

                        Indents -= baseIndents;
                    }

                    if (pendingBlankLine)
                    {
                        ReadOnlySpan<char> firstLine = lines[0];
                        if (!firstLine.IsWhiteSpace())
                        {
                            DoPendingBlankLine();
                        }
                    }

                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            pendingBlankLine = false;
                            lastLineWasBlank = true;
                            _ = stringBuilder.AppendLine();
                            continue;
                        }

                        if (line.Length > 0 && line[0] == NoIndentChar && NoIndentChar != '\0')
                        {
                            lastLineWasBlank = false;
                            _ = stringBuilder.AppendLine(line);
                            continue;
                        }

                        // Output indent = current level adjusted by the line's offset from base.
                        lastLineWasBlank = false;
                        _ = stringBuilder.Append(CurrentIndent)
                                          .AppendLine(line);
                    }

                    DoPostWriteChanges(postWriteChange + lastLineIndents + (lastLineTriggersOn ? 1 : 0), lastLineTriggersOff, triggeredBlankPost);
                }

                break;
            }

            case IndentControl.FullAuto:
            {
                string[] lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);

                foreach (string rawLine in lines)
                {
                    // FullAuto so trim any white space from start of line
                    // we will add indents back in as needed
                    string line = rawLine.TrimStart();

                    if (line.Length == 0)
                    {
                        pendingBlankLine = false;
                        lastLineWasBlank = true;
                        _ = stringBuilder.AppendLine();
                        continue;
                    }

                    // Process control chars at end of line; returns the span of content to actually write.
                    // A line whose only content was stripped control chars is treated as blank.
                    ReadOnlySpan<char> effectiveSpan = ProcessControlChars(line, out int postWriteChange, out bool lineTriggeredOff, out bool lineTriggeredBlankPost);

                    if (effectiveSpan.IsEmpty)
                    {
                        pendingBlankLine = false;
                        lastLineWasBlank = true;
                        _ = stringBuilder.AppendLine();

                        if (postWriteChange != 0)
                        {
                            Indents += postWriteChange;
                        }

                        continue;
                    }

                    // NoIndentChar lines are written without indentation.
                    // Control chars are still processed above before this check.
                    if (effectiveSpan[0] == NoIndentChar && NoIndentChar != '\0')
                    {
                        DoPendingBlankLine();
                        lastLineWasBlank = false;
                        string noIndentLine = effectiveSpan.Length == line.Length ? line : effectiveSpan.ToString();
                        _ = stringBuilder.AppendLine(noIndentLine);
                        DoPostWriteChanges(postWriteChange, lineTriggeredOff, lineTriggeredBlankPost);
                        continue;
                    }

                    if (!lineTriggeredOff)
                    {
                        DoPendingBlankLine();
                    }

                    lastLineWasBlank = false;
                    string effectiveLine = effectiveSpan.Length == line.Length ? line : effectiveSpan.ToString();
                    _ = stringBuilder.Append(CurrentIndent)
                                      .AppendLine(effectiveLine);

                    DoPostWriteChanges(postWriteChange, lineTriggeredOff, lineTriggeredBlankPost);
                }

                break;
            }

            case IndentControl.None:
            default:
                _ = stringBuilder.AppendLine(text);
                lastLineWasBlank = LineStartsOnlyWithWhitespace(text, text.Length - 1);
                break;
        }

        return this;
    }

    /// <summary>
    /// This checks if only whitespace exist starting at x for the line x is on.
    /// </summary>
    /// <param name="str">The text to scan.</param>
    /// <param name="x">Index of the char to start scanning backwards from.</param>
    /// <returns>True if only whitespace from the start of the line x is on until and including x.</returns>
    private static bool LineStartsOnlyWithWhitespace(ReadOnlySpan<char> str, int x)
    {
        for (int i = x; i >= 0; i--)
        {
            char c = str[i];

            // If we find new line then we can stop checking
            // This will also cover \r\n as will hit \n first
            if (c is '\n')
            {
                return true;
            }

            if (!char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        // So checked all chars without finding a non-whitespace
        // so it is a success
        return true;
    }

    /// <summary>
    /// Scans forward from the start of <paramref name="str"/> until the first newline
    /// or the end of the string, returning <see langword="true"/> if every character
    /// in that first line is whitespace (i.e. the first line is blank or whitespace-only).
    /// </summary>
    private static bool FirstLineIsWhitespace(ReadOnlySpan<char> str)
    {
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];

            // Hit a newline before any non-whitespace — first line is blank/whitespace-only
            if (c is '\n')
            {
                return true;
            }

            if (!char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        // Reached end without a newline and without a non-whitespace char
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DoPostWriteChanges(int postWriteChange, bool lineTriggeredOff, bool lineTriggeredBlankPost)
    {
        if (postWriteChange != 0)
        {
            Indents += postWriteChange;
        }

        if (lineTriggeredOff)
        {
            pendingBlankLine = ForceBlankLineAfterOff;
        }

        if (lineTriggeredBlankPost)
        {
            pendingBlankLine = true;
        }
    }

    /// <summary>
    /// Counts the number of complete indent levels (each <see cref="IndentSize"/> copies of
    /// <see cref="IndentChar"/>) at the start of <paramref name="line"/>.
    /// </summary>
    /// <param name="line">Line to check.</param>
    /// <param name="maxIndents">Max number of indents you care about. &lt;=0 to have no max.</param>
    private int CountIndents(string line, int maxIndents = 0)
    {
        int indents = 0;
        int start = 0;
        int end = IndentSize - 1;
        while (end < line.Length)
        {
            for (int s = start; s <= end; s++)
            {
                if (line[s] != IndentChar)
                {
                    return indents;
                }
            }

            indents++;

            if (indents == maxIndents)
            {
                return indents;
            }

            start += IndentSize;
            end += IndentSize;
        }

        return indents;
    }

    /// <summary>
    /// Checks end of text for control chars for handling.
    /// For FullAuto should be called for each line.
    /// For Auto should be called once for that input text.
    /// Processes multiple control chars at the end of the text, consuming them in reverse
    /// until a non-control char (or ForceIndentOn/Off) is reached.
    /// </summary>
    /// <param name="text">Text to check.</param>
    /// <param name="postWriteIndentChange">Outputs the number that should be added to <see cref="Indents"/> after writing this text.</param>
    /// <param name="triggeredOff">Outputs whether the last line was <see cref="ForceIndentOff"/> and needs to be actioned after write.</param>
    /// <param name="triggeredBlankPost">Outputs whether <see cref="BlankLinePost"/> was seen and a blank line should be queued after write.</param>
    /// <param name="processBrackets">
    /// When true (default) ForceIndentOn/Off chars are processed as usual.
    /// When false they are ignored; only explicit \x01–\x04 control chars and blank-line
    /// chars are consumed. Used by the multi-line Auto path which derives indent changes
    /// from the delta between the first and last line instead.
    /// </param>
    private ReadOnlySpan<char> ProcessControlChars(ReadOnlySpan<char> text, out int postWriteIndentChange, out bool triggeredOff, out bool triggeredBlankPost, bool processBrackets = true)
    {
        postWriteIndentChange = 0;
        triggeredOff = false;
        triggeredBlankPost = false;

        for (int x = text.Length - 1; x >= 0; x--)
        {
            char c = text[x];

            switch (c)
            {
                case '\0':
                    // We do not support null char for control chars
                    return text[..(x + 1)];

                case IndentIncreasePre:
                    Indents++;
                    break;

                case IndentDecreasePre:
                    Indents--;
                    break;

                case IndentIncreasePost:
                    postWriteIndentChange++;
                    break;

                case IndentDecreasePost:
                    postWriteIndentChange--;
                    break;

                case BlankLinePre:
                    pendingBlankLine = true;
                    break;

                case BlankLinePost:
                    triggeredBlankPost = true;
                    break;

                default:
                    if (processBrackets)
                    {
                        if (c == ForceIndentOn)
                        {
                            if (LineStartsOnlyWithWhitespace(text, x - 1))
                            {
                                postWriteIndentChange++;
                            }
                        }
                        else if (c == ForceIndentOff)
                        {
                            if (LineStartsOnlyWithWhitespace(text, x - 1))
                            {
                                Indents--;
                                triggeredOff = true;
                            }
                        }
                    }

                    return text[..(x + 1)];
            }
        }

        return [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DoPendingBlankLine()
    {
        if (pendingBlankLine)
        {
            pendingBlankLine = false;

            if (stringBuilder.Length > 0 && !lastLineWasBlank)
            {
                lastLineWasBlank = true;
                _ = stringBuilder.AppendLine();
            }
        }
    }
}
