namespace Gori.CodeStringBuilder;

/// <summary>
/// Options for controlling how to process writes.
/// </summary>
public enum IndentControl
{
    /// <summary>
    /// Force strip any white space from lines and add indents.
    /// Will search for control chars and increase / decrease indents accordingly as written.
    /// </summary>
    FullAuto,

    /// <summary>
    /// Treats any single write command as already being properly formatted itself.
    /// Any indents on first line will be removed, as starting at current indent level.
    /// Same number of indents will be removed from all other lines, including moving back in indents if needed.
    /// NOTE: Indents must use same char and count for indents.
    /// </summary>
    Auto,

    /// <summary>
    /// Same as None but will still enforce any current pending blank line requests.
    /// Will not search for any control chars just enforce blank line prior if previously set.
    /// </summary>
    BlankLineOnly,

    /// <summary>
    /// Disables any indenting and just writes the text out as is.
    /// Will not search for any control chars.
    /// </summary>
    None,
}
