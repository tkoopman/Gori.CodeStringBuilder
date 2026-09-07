# Gori.CodeStringBuilder

A lightweight .NET helper for writing indented, formatted text — designed for use in source generators and similar code-emission scenarios.

It is not a full code formatter. It provides just enough structure to write clean, consistently-indented output without managing indent state manually in every call site.

---

## Getting started

```csharp
var sb = new CodeStringBuilder
{
	IndentSize = 4,
	IndentControl = IndentControl.Auto,
};

sb.WriteLine("public class Foo")
  .WriteLine("{")
  .WriteLine("    public int Value { get; set; }")
  .WriteLine("}");

Console.Write(sb.ToString());
```

For C# output there is a preset factory method:

```csharp
// Auto mode (default)
var sb = CodeStringBuilder.CreateCSharpBuilder();
```

`CreateCSharpBuilder` presets:

| Property | Value |
|---|---|
| `IndentSize` | `4` |
| `IndentChar` | `' '` (space) |
| `ForceIndentOn` | `'{'` |
| `ForceIndentOff` | `'}'` |
| `ForceBlankLineAfterOff` | `true` |
| `NoIndentChar` | `'#'` |

---

## Configuration properties

| Property | Type | Default | Description |
|---|---|---|---|
| `IndentChar` | `char` | `' '` | Character used to build each indent level. |
| `IndentSize` | `int` | `4` | Number of `IndentChar` characters per indent level. |
| `IndentControl` | `IndentControl` | `Auto` | Default indentation mode for `WriteLine` calls. |
| `DefaultVariables` | `IReadOnlyDictionary<string, string>?` | `null` | Default variable values used by write methods when a key is missing from per-call variables (or when per-call variables are omitted). |
| `VariablePattern` | `string?` | `@"\$\{(?<name>[a-zA-Z0-9_]+)\}"` | Regex pattern for variable tokens. Must include a named capture group `name`. Set to `null` to disable variable replacement. |
| `Indents` | `int` | `0` | Current indent depth. Clamped to zero if set negative. |
| `NoIndentChar` | `char` | `'\0'` (off) | Lines starting with this character are written without indentation. Control chars at the end of the line are still processed. |
| `ForceIndentOn` | `char` | `'\0'` (off) | A line whose only non-whitespace content is this character increases the indent level for the next line. |
| `ForceIndentOff` | `char` | `'\0'` (off) | A line whose only non-whitespace content is this character decreases the indent level before writing. |
| `ForceBlankLineAfterOff` | `bool` | `false` | When `ForceIndentOff` fires, a blank line is queued after the closing line. The blank is suppressed if the next line is already blank. |

---

## IndentControl modes

```csharp
public enum IndentControl
{
	FullAuto,      // Strip leading whitespace, add current indent, process control chars per line
	Auto,          // Add current indent, process control chars on last line; delta-tracks first/last line indent
	BlankLineOnly, // No indentation; honours any pending blank line request; no control chars
	None,          // Write text exactly as given, no indentation, no blank-line handling; no control chars
}
```

The mode can be set as the instance default via `IndentControl` or overridden per call:

```csharp
sb.WriteLine("#pragma warning disable", IndentControl.BlankLineOnly);
sb.WriteLine("raw text", IndentControl.None);
```

### FullAuto vs Auto

**`FullAuto`** processes every line in a multi-line write independently. 
Leading whitespace is stripped and the current indent is added back. 
`ForceIndentOn`/`ForceIndentOff` take effect immediately per line, 
so indent changes are visible within the same write call.

**`Auto`** preserves leading whitespace as given and treats each write as already correctly formatted relative to itself. 
Control chars are processed on the last line only. 
For multi-line writes the indent delta between the first and last eligible line is applied after writing,
so the builder tracks where the snippet leaves off.
For single-line writes any whitespace at the start of the line is assumed to be intentional, and is added on top of current indents. 

---

## Control characters

Append these special characters to the end of a line to influence indentation or blank-line insertion. 
They are consumed (removed from output) during processing.

| Constant | Hex | Effect |
|---|---|---|
| `IndentIncreasePre` | `\x01` | Increase indent before writing this line |
| `IndentIncreasePost` | `\x02` | Increase indent after writing this line (`Increase` is an alias) |
| `IndentDecreasePre` | `\x03` | Decrease indent before writing this line (`Decrease` is an alias) |
| `IndentDecreasePost` | `\x04` | Decrease indent after writing this line |
| `BlankLinePre` | `\x1D` | Insert a blank line before this line if one does not already exist |
| `BlankLinePost` | `\x1E` | Queue a blank line after this line |

Multiple control chars can appear at the end of a single line and are processed in reverse order.
 
```csharp
// Default code string builder which has no forced indents
var sb = new CodeStringBuilder();

// Increase indent after "namespace Foo {"
sb.WriteLine($"namespace Foo {{{CodeStringBuilder.IndentIncreasePost}}");

// Decrease indent before "}" and queue a blank line after it
sb.WriteLine($"}}{CodeStringBuilder.Decrease}{CodeStringBuilder.BlankLinePost}");
```

---

## Variables

Variable replacement is supported on all `WriteLine` and `WriteLineIf` overloads that accept a `variables` dictionary.

Default token format is `${name}` where `name` matches `[a-zA-Z0-9_]+`.

Resolution order for each token:

1. Per-call `variables` dictionary
2. `DefaultVariables` on the builder
3. Leave token unchanged if no value is found

```csharp
var sb = new CodeStringBuilder
{
    DefaultVariables = new Dictionary<string, string>
    {
        ["namespace"] = "MyApp",
        ["access"] = "public"
    }
};

sb.WriteLine("namespace ${namespace}")
  .WriteLine("${access} class ${name}", new Dictionary<string, string>
  {
      ["name"] = "Widget"
  });
```

You can override the token syntax using `VariablePattern` (must include a named group `name`):

```csharp
var sb = new CodeStringBuilder
{
    VariablePattern = "\\#\\((?<name>[a-zA-Z0-9_]+)\\)"
};

sb.WriteLine("Hello #(user)", new Dictionary<string, string>
{
    ["user"] = "Alice"
});
```

Set `VariablePattern = null` to disable replacements.

---

## Blank line helpers

### `ForceBlankLine()`

Queues a blank line between the previous write and the next content write. The blank is not emitted if:

- Nothing has been written yet.
- The last line written was already blank.
- Nothing more is written.

```csharp
sb.WriteLine("public int A { get; set; }")
  .ForceBlankLine()
  .WriteLine("public int B { get; set; }");
// Output:
// public int A { get; set; }
//
// public int B { get; set; }
```

### `ForceBlankLineAfterOff`

When `ForceIndentOff` fires (e.g. a closing `}`), a blank line is automatically queued after it. 
Useful for ensuring visual separation between top-level blocks without adding it manually after every closing brace.

If multiple consecutive `ForceIndentOff` lines are written (e.g. nested closing braces), 
the blank line is only emitted after the **last** one — intermediate closing lines cancel the pending blank queued by the previous one.

---

## Writing

All `WriteLine` overloads return `this` for fluent chaining.

```csharp
// Single line
sb.WriteLine("text");

// Single line with per-call indent override
sb.WriteLine("text", IndentControl.None);

// Blank line
sb.WriteLine();

// Multi-line — newlines within the string are split and each line is handled individually
sb.WriteLine("line one\nline two\nline three");

// Single line with variables
sb.WriteLine("Hello ${name}", new Dictionary<string, string> { ["name"] = "World" });

// Conditional writes
sb.WriteLineIf(() => condition, "optional line");
sb.WriteLineIf(() => condition, () => ExpensiveString());
sb.WriteLineIf(() => condition, "optional line", IndentControl.BlankLineOnly);
sb.WriteLineIf(() => condition, () => ExpensiveString(), IndentControl.BlankLineOnly);
sb.WriteLineIf(() => condition, "Hi ${who}", new Dictionary<string, string> { ["who"] = "Tim" });
```

### `Clear()`

Resets the buffer and indent state (`Indents` back to 0, pending blank flags cleared).

---

## Example — C# class with FullAuto

```csharp
var sb = CodeStringBuilder.CreateCSharpBuilder(fullAuto: true);

sb.WriteLine("namespace MyApp")
  .WriteLine("{")
  .WriteLine("public class MyClass")
  .WriteLine("{")
  .WriteLine("public void Method()")
  .WriteLine("{")
  .WriteLine("// body")
  .WriteLine("}")
  .WriteLine("}");

Console.Write(sb.ToString());
```

Output:
```
namespace MyApp
{
	public class MyClass
	{
		public void Method()
		{
			// body
		}
	}
}
```

The blank line after `}` (Method) would normally be produced by `ForceBlankLineAfterOff = true`, 
but because the next line is also a `ForceIndentOff` line (`}`), it cancels the pending blank and 
queues its own — and so on up the chain. The result is that consecutive closing braces are written 
with no blank lines between them, with only a single blank queued after the last one. Since nothing 
is written after the outermost `}` here, no trailing blank appears.

---

## Notes

- `NoIndentChar` lines (e.g. `#pragma`) are written without indentation but control chars at the end of the line are still processed and removed.
- `BlankLineOnly` is intended for lines that should bypass indentation (like `None`) but still respect a pending blank line queued by `ForceBlankLine()` or `BlankLinePost`. 
  If the text starts with a whitespace-only first line, the pending blank is absorbed rather than emitted as a separate line.
- Indent depth is clamped to zero — setting `Indents` below zero has no effect.
