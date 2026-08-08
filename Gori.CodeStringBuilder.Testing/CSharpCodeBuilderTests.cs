namespace Gori.CodeStringBuilder.Testing;

public class CSharpCodeBuilderTests (ITestOutputHelper output)
{
    [Theory]
    [ClassData(typeof(CSharpTestData))]
    public void CSharpTests (string expected, params string?[] lines)
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        foreach (string? line in lines)
        {
            _ = sb.WriteLine(line);
        }

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
    }

    [Theory]
    [ClassData(typeof(AutoTestData))]
    public void AutoTests (string expected, params string?[] lines)
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentChar = ' ',
            IndentControl = IndentControl.Auto,
            ForceIndentOn = '{',
            ForceIndentOff = '}',
            ForceBlankLineAfterOff = true,
            NoIndentChar = '#',
        };

        foreach (string? line in lines)
        {
            _ = sb.WriteLine(line);
        }

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
    }

    [Theory]
    [ClassData(typeof(CSharpTestData))]
    [ClassData(typeof(CSharpAutoOnlyTestData))]
    public void CSharpAutoTests (string expected, params string?[] lines)
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder(true);
        foreach (string? line in lines)
        {
            _ = sb.WriteLine(line);
        }

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Auto_RawMultiLineStringWithSelfContainedBlock_IndentNotCorrupted ()
    {
        // A raw multi-line string whose first line starts at 1 indent level and whose
        // last line also returns to 1 indent level should leave Indents unchanged and
        // write every line at the correct depth relative to the current builder indent.
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        _ = sb.WriteLine("class Foo");
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("""
            public void Method()
            {
                // Do stuff
            }
            """);
        _ = sb.WriteLine("}");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal(
            """
            class Foo
            {
                public void Method()
                {
                    // Do stuff
                }
            }

            """,
            result,
            ignoreLineEndingDifferences: true);
    }

    private static CodeStringBuilder createAutoBuilder () => new()
    {
        IndentSize = 4,
        IndentChar = ' ',
        IndentControl = IndentControl.Auto,
        ForceIndentOn = '{',
        ForceIndentOff = '}',
        ForceBlankLineAfterOff = true,
        NoIndentChar = '#',
    };

    [Fact]
    public void Auto_SingleLine_LeadingSpacesAppendedToCurrentIndent ()
    {
        // A single content line with leading spaces: those spaces are added on top
        // of the current indent, not stripped or counted as indent levels.
        CodeStringBuilder sb = createAutoBuilder();
        _ = sb.WriteLine("{");

        _ = sb.WriteLine("    extra");

        output.WriteLine(sb.ToString());

        Assert.Equal(
            """
            {
                    extra

            """,
            sb.ToString(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Auto_SingleLine_NoLeadingSpaces_WrittenAtCurrentIndent ()
    {
        // A plain single line with no leading spaces is written at the current indent.
        CodeStringBuilder sb = createAutoBuilder();
        _ = sb.WriteLine("{");

        _ = sb.WriteLine("content");

        output.WriteLine(sb.ToString());

        Assert.Equal(
            """
            {
                content

            """,
            sb.ToString(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Auto_SingleLine_ForceIndentOn_NextWriteIsIndented ()
    {
        // A single line ending with ForceIndentOn ('{') increments indent for the next write.
        CodeStringBuilder sb = createAutoBuilder();

        _ = sb.WriteLine("void Method()");
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("// body");

        output.WriteLine(sb.ToString());

        Assert.Equal(
            """
            void Method()
            {
                // body

            """,
            sb.ToString(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Auto_SingleLine_ForceIndentOff_DecreasesIndentBeforeWrite ()
    {
        // A single line ending with ForceIndentOff ('}') decrements indent before writing it.
        CodeStringBuilder sb = createAutoBuilder();
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("content");
        _ = sb.WriteLine("}");

        output.WriteLine(sb.ToString());

        Assert.Equal(
            """
            {
                content
            }

            """,
            sb.ToString(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Auto_MultiLine_LastLineIsForceIndentOn_NextWriteIsIndented ()
    {
        // When the last non-blank line of a multi-line Auto write ends with
        // ForceIndentOn ('{'), the next write should be one level deeper.
        // Bug: processBrackets=false means the '{' is never seen, so the
        // post-write +1 is never queued and the next write is at the wrong level.
        CodeStringBuilder sb = createAutoBuilder();
        _ = sb.WriteLine("{");

        _ = sb.WriteLine("    void Method()\n    {");

        _ = sb.WriteLine("// body");

        output.WriteLine(sb.ToString());

        Assert.Equal(
            """
            {
                void Method()
                {
                    // body

            """,
            sb.ToString(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Auto_CountIndents_PartialIndentCharsDoNotCountAsFullLevel ()
    {
        // With IndentSize=4 a line starting with only 3 spaces ("   X") is NOT
        // a complete indent level and should count as 0.
        // Bug: countIndents checks s < end (0,1,2) instead of s <= end (0,1,2,3),
        // so it only verifies 3 of 4 chars and incorrectly counts it as 1 level.
        // That causes baseIndents=1 which decrements Indents before writing,
        // putting the output at the wrong indent depth.
        CodeStringBuilder sb = createAutoBuilder();
        _ = sb.WriteLine("{");

        // First line has 3 spaces — not a full indent level (IndentSize=4).
        // baseIndents should be 0, so Indents stays at 1.
        _ = sb.WriteLine("   Line 1\n   Line 2");

        output.WriteLine($"Indents after write: {sb.Indents}");
        output.WriteLine(sb.ToString());

        Assert.Equal(1, sb.Indents);
        Assert.Equal(
            """
            {
                   Line 1
                   Line 2

            """,
            sb.ToString(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Auto_AtIndent2_ClosingBracketMiddle_DeltaFromLastLine ()
    {
        // Multi-line Auto: first eligible line "    something" = 1 indent level,
        // last eligible line "something else" = 0 indent levels → delta = -1.
        // Indents goes from 2 → 1 after the write.
        CodeStringBuilder sb = createAutoBuilder();
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("{");

        _ = sb.WriteLine("""
                something
            }

            something else
            """);

        output.WriteLine($"Indents after write: {sb.Indents}");
        output.WriteLine(sb.ToString());

        Assert.Equal(1, sb.Indents);
        Assert.Equal(
            """
            {
                {
                    something
                }

                something else

            """,
            sb.ToString(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Auto_AtIndent2_TwoClosingBrackets_DeltaFromLastLine ()
    {
        // Multi-line Auto: first eligible line "        something" = 2 levels,
        // last eligible line "}" = 0 levels → delta = -2.
        // Indents goes from 2 → 0 after the write.
        CodeStringBuilder sb = createAutoBuilder();
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("{");

        _ = sb.WriteLine("        something\n    }\n}");

        output.WriteLine($"Indents after write: {sb.Indents}");
        output.WriteLine(sb.ToString());

        Assert.Equal(0, sb.Indents);
        Assert.Equal(
            """
            {
                {
                    something
                }
            }

            """,
            sb.ToString(),
            ignoreLineEndingDifferences: true);
    }
}
