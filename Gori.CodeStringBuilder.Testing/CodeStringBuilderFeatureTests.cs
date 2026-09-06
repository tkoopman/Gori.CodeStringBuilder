namespace Gori.CodeStringBuilder.Testing;

/// <summary>
/// Feature-focused tests for CodeStringBuilder that complement the data-driven CSharp builder tests.
/// </summary>
public class CodeStringBuilderFeatureTests(ITestOutputHelper output)
{
    // -------------------------------------------------------------------------
    // WriteLine() – parameterless overload writes a blank line
    // -------------------------------------------------------------------------

    [Fact]
    public void WriteLine_Parameterless_WritesBlankLine()
    {
        var sb = new CodeStringBuilder();
        _ = sb.WriteLine("A").WriteLine().WriteLine("B");

        string result = sb.ToString();
        output.WriteLine(result);

        // WriteLine() with no args appends a blank line between A and B
        Assert.Equal("A\r\n\r\nB\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // Method chaining – WriteLine returns the same instance
    // -------------------------------------------------------------------------

    [Fact]
    public void WriteLine_ReturnsThis_AllowsChaining()
    {
        var sb = new CodeStringBuilder();
        CodeStringBuilder returned = sb.WriteLine("line");

        Assert.Same(sb, returned);
    }

    // -------------------------------------------------------------------------
    // Clear() resets both content and indent depth
    // -------------------------------------------------------------------------

    [Fact]
    public void Clear_ResetsContentAndIndents()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.Auto,
            Indents = 3
        };
        _ = sb.WriteLine("something");

        sb.Clear();

        Assert.Equal(string.Empty, sb.ToString());
        Assert.Equal(0, sb.Indents);
    }

    // -------------------------------------------------------------------------
    // Indents – negative assignment clamps to 0
    // -------------------------------------------------------------------------

    [Fact]
    public void Indents_NegativeValue_ClampsToZero()
    {
        var sb = new CodeStringBuilder
        {
            Indents = -5
        };

        Assert.Equal(0, sb.Indents);
    }

    // -------------------------------------------------------------------------
    // IndentControl.None – text is written without any indentation
    // -------------------------------------------------------------------------

    [Fact]
    public void WriteLine_IndentControlNone_NoIndentAdded()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.None,
            Indents = 2
        };
        _ = sb.WriteLine("hello");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("hello\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteLine_OverrideIndentControlNone_NoIndentAdded()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.Auto,
            Indents = 2
        };
        _ = sb.WriteLine("hello", IndentControl.None);

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("hello\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // Custom IndentChar and IndentSize
    // -------------------------------------------------------------------------

    [Fact]
    public void CustomIndentChar_IsUsed()
    {
        var sb = new CodeStringBuilder
        {
            IndentChar = '\t',
            IndentSize = 1,
            IndentControl = IndentControl.Auto,
            Indents = 2
        };
        _ = sb.WriteLine("code");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("\t\tcode\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void CustomIndentSize_IsUsed()
    {
        var sb = new CodeStringBuilder
        {
            IndentChar = ' ',
            IndentSize = 2,
            IndentControl = IndentControl.Auto,
            Indents = 3
        };
        _ = sb.WriteLine("code");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("      code\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // NoIndentChar – line starting with this char gets no indentation
    // -------------------------------------------------------------------------

    [Fact]
    public void NoIndentChar_LineStartingWithIt_HasNoIndent()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.Auto,
            NoIndentChar = '#',
            Indents = 2
        };
        _ = sb.WriteLine("normal");
        _ = sb.WriteLine("#pragma warning disable");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("        normal\r\n#pragma warning disable\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void NoIndentChar_FullAuto_LineStartingWithIt_HasNoIndent()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.FullAuto,
            NoIndentChar = '#',
            Indents = 1
        };
        _ = sb.WriteLine("normal\n#pragma warning disable");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("    normal\r\n#pragma warning disable\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // ControlCharDecreasePost – decreases indent AFTER the line is written
    // -------------------------------------------------------------------------

    [Fact]
    public void ControlCharDecreasePost_Auto_DecreasesIndentAfterWrite()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.Auto,
            Indents = 2
        };
        // Line is written at current indent (2), then indent drops to 1
        _ = sb.WriteLine($"close{CodeStringBuilder.IndentDecreasePost}");
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("        close\r\n    next\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ControlCharDecreasePost_FullAuto_DecreasesIndentAfterWrite()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.FullAuto,
            Indents = 2
        };
        _ = sb.WriteLine($"close{CodeStringBuilder.IndentDecreasePost}\nnext");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("        close\r\n    next\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // ControlCharIncreasePre – increases indent BEFORE writing the line (Auto)
    // -------------------------------------------------------------------------

    [Fact]
    public void ControlCharIncreasePre_Auto_IncreasesIndentBeforeWrite()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.Auto,
            Indents = 0
        };
        // Indent jumps to 1 before writing, so line gets 4 spaces
        _ = sb.WriteLine($"indented{CodeStringBuilder.IndentIncreasePre}");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("    indented\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // ControlCharIncreasePre – per-line in FullAuto mode
    // -------------------------------------------------------------------------

    [Fact]
    public void ControlCharIncreasePre_FullAuto_AppliedPerLine()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.FullAuto,
            Indents = 0
        };
        _ = sb.WriteLine($"first{CodeStringBuilder.IndentIncreasePre}\nsecond");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("    first\r\n    second\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // ControlCharDecreasePre – decreases indent BEFORE writing (Auto single-line)
    // -------------------------------------------------------------------------

    [Fact]
    public void ControlCharDecreasePre_Auto_DecreasesIndentBeforeWrite()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.Auto,
            Indents = 2
        };
        _ = sb.WriteLine($"dedented{CodeStringBuilder.IndentDecreasePre}");
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("    dedented\r\n    next\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // ControlCharIncreasePost (Increase alias) – single-line Auto
    // -------------------------------------------------------------------------

    [Fact]
    public void Increase_Auto_IncreasesIndentAfterWrite()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.Auto,
            Indents = 0
        };
        _ = sb.WriteLine($"open{CodeStringBuilder.Increase}");
        _ = sb.WriteLine("body");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("open\r\n    body\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // ForceIndentOn / ForceIndentOff – only fires when char is alone on last line
    // -------------------------------------------------------------------------

    [Fact]
    public void ForceIndentOn_AloneOnLastLine_IncreasesIndent()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("body");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    body\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceIndentOff_AloneOnLastLine_DecreasesIndent()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("body");
        _ = sb.WriteLine("}");
        _ = sb.WriteLine("after");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    body\r\n}\r\n\r\nafter\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceIndentOn_NotAloneOnLastLine_DoesNotIncreaseIndent()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        // '{' appears at end but other text is on the same last line
        _ = sb.WriteLine("foo {");
        _ = sb.WriteLine("body");

        string result = sb.ToString();
        output.WriteLine(result);

        // indent should still be 0 – no change
        Assert.Equal("foo {\r\nbody\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceIndentOff_NotAloneOnLastLine_DoesNotDecreaseIndent()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        _ = sb.WriteLine("{");   // indent → 1
        _ = sb.WriteLine("foo }");  // '}' not alone on line → indent stays 1
        _ = sb.WriteLine("after");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    foo }\r\n    after\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // ForceIndentOn / ForceIndentOff in multi-line text (Auto – only last line checked)
    // -------------------------------------------------------------------------

    [Fact]
    public void ForceIndentOn_MultiLine_Auto_OnlyLastLineChecked()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        // The '{' on the LAST line should trigger indent increase
        _ = sb.WriteLine("line1\n{");
        _ = sb.WriteLine("body");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("line1\r\n{\r\n    body\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // CreateCSharpBuilder – verifies preset properties
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateCSharpBuilder_HasExpectedDefaults()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();

        Assert.Equal(4, sb.IndentSize);
        Assert.Equal(' ', sb.IndentChar);
        Assert.Equal(IndentControl.Auto, sb.IndentControl);
        Assert.Equal('{', sb.ForceIndentOn);
        Assert.Equal('}', sb.ForceIndentOff);
        Assert.True(sb.ForceBlankLineAfterOff);
        Assert.Equal('#', sb.NoIndentChar);
    }

    [Fact]
    public void CreateCSharpBuilder_FullAuto_HasFullAutoControl()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder(fullAuto: true);

        Assert.Equal(IndentControl.FullAuto, sb.IndentControl);
    }

    // -------------------------------------------------------------------------
    // ForceBlankLineAfterOff – Auto
    // -------------------------------------------------------------------------

    [Fact]
    public void ForceBlankLineAfterOff_Auto_InsertsBlankBeforeNextLine()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("body");
        _ = sb.WriteLine("}");
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    body\r\n}\r\n\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceBlankLineAfterOff_Auto_WhenDisabled_NoBlankAdded()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize  = 4,
            IndentChar  = ' ',
            IndentControl = IndentControl.Auto,
            ForceIndentOn  = '{',
            ForceIndentOff = '}',
            ForceBlankLineAfterOff = false,
        };
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("body");
        _ = sb.WriteLine("}");
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    body\r\n}\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceBlankLineAfterOff_Auto_NextLineAlreadyBlank_NoDuplicateBlank()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("body");
        _ = sb.WriteLine("}");
        _ = sb.WriteLine();       // explicit blank line
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    body\r\n}\r\n\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceBlankLineAfterOff_Auto_NextLineWhitespaceOnly_NoDuplicateBlank()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("body");
        _ = sb.WriteLine("}");
        _ = sb.WriteLine("   ");  // whitespace-only line clears the pending flag
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    body\r\n}\r\n\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceBlankLineAfterOff_Auto_ConsecutiveForceOff_BlankOnlyAfterLast()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("body");
        _ = sb.WriteLine("}");
        _ = sb.WriteLine("}");
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    {\r\n        body\r\n    }\r\n}\r\n\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // ForceBlankLineAfterOff – FullAuto
    // -------------------------------------------------------------------------

    [Fact]
    public void ForceBlankLineAfterOff_FullAuto_InsertsBlankBeforeNextLine()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder(fullAuto: true);
        _ = sb.WriteLine("{\n    body\n}");
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    body\r\n}\r\n\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceBlankLineAfterOff_FullAuto_WhenDisabled_NoBlankAdded()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize  = 4,
            IndentChar  = ' ',
            IndentControl = IndentControl.FullAuto,
            ForceIndentOn  = '{',
            ForceIndentOff = '}',
            ForceBlankLineAfterOff = false,
        };
        _ = sb.WriteLine("{\n    body\n}");
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    body\r\n}\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceBlankLineAfterOff_FullAuto_NextLineAlreadyBlank_NoDuplicateBlank()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder(fullAuto: true);
        _ = sb.WriteLine("{\n    body\n}");
        _ = sb.WriteLine();       // explicit blank line
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    body\r\n}\r\n\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceBlankLineAfterOff_FullAuto_ConsecutiveForceOff_SameCall_BlankOnlyAfterLast()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder(fullAuto: true);
        _ = sb.WriteLine("{\n    {\n        body\n    }\n}");
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    {\r\n        body\r\n    }\r\n}\r\n\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceBlankLineAfterOff_FullAuto_ConsecutiveForceOff_AcrossCalls_BlankOnlyAfterLast()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder(fullAuto: true);
        _ = sb.WriteLine("{\n    {\n        body\n    }");
        _ = sb.WriteLine("}");
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("{\r\n    {\r\n        body\r\n    }\r\n}\r\n\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // ForceBlankLineAfterOff – Clear resets pending state
    // -------------------------------------------------------------------------

    [Fact]
    public void ForceBlankLineAfterOff_Clear_ResetsPendingBlank()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();
        _ = sb.WriteLine("{");
        _ = sb.WriteLine("body");
        _ = sb.WriteLine("}");   // sets pending blank

        sb.Clear();              // should reset pending flag

        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("next\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // WriteLineIf – predicate overloads
    // -------------------------------------------------------------------------

    [Fact]
    public void WriteLineIf_String_PredicateTrue_WritesLine()
    {
        var sb = new CodeStringBuilder();
        _ = sb.WriteLineIf(() => true, "hello");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("hello\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteLineIf_String_PredicateFalse_WritesNothing()
    {
        var sb = new CodeStringBuilder();
        _ = sb.WriteLineIf(() => false, "hello");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void WriteLineIf_String_PredicateFalse_ReturnsThis()
    {
        var sb = new CodeStringBuilder();
        CodeStringBuilder returned = sb.WriteLineIf(() => false, "hello");

        Assert.Same(sb, returned);
    }

    [Fact]
    public void WriteLineIf_FuncString_PredicateTrue_WritesLine()
    {
        var sb = new CodeStringBuilder();
        _ = sb.WriteLineIf(() => true, () => "hello");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("hello\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteLineIf_FuncString_PredicateFalse_WritesNothing()
    {
        var sb = new CodeStringBuilder();
        _ = sb.WriteLineIf(() => false, () => "hello");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void WriteLineIf_FuncString_PredicateFalse_FactoryNotCalled()
    {
        var sb = new CodeStringBuilder();
        bool factoryCalled = false;
        _ = sb.WriteLineIf(() => false, () =>
        {
            factoryCalled = true;
            return "hello";
        });

        Assert.False(factoryCalled);
    }

    [Fact]
    public void WriteLineIf_StringWithIndentControl_PredicateTrue_WritesLine()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.Auto,
            Indents = 2
        };
        _ = sb.WriteLineIf(() => true, "hello", IndentControl.None);

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("hello\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteLineIf_StringWithIndentControl_PredicateFalse_WritesNothing()
    {
        var sb = new CodeStringBuilder();
        _ = sb.WriteLineIf(() => false, "hello", IndentControl.None);

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void WriteLineIf_FuncStringWithIndentControl_PredicateTrue_WritesLine()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.Auto,
            Indents = 2
        };
        _ = sb.WriteLineIf(() => true, () => "hello", IndentControl.None);

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("hello\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteLineIf_FuncStringWithIndentControl_PredicateFalse_FactoryNotCalled()
    {
        var sb = new CodeStringBuilder();
        bool factoryCalled = false;
        _ = sb.WriteLineIf(() => false,
                           () =>
                           {
                               factoryCalled = true;
                               return "hello";
                           }, IndentControl.None);

        Assert.False(factoryCalled);
    }

    // -------------------------------------------------------------------------
    // BlankLinePre – inserts a blank line before the line it appears on
    // -------------------------------------------------------------------------

    [Fact]
    public void BlankLinePre_Auto_InsertsBlankBeforeLine()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.Auto };
        _ = sb.WriteLine("first");
        _ = sb.WriteLine($"second{CodeStringBuilder.BlankLinePre}");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nsecond\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLinePre_Auto_NoDuplicateBlank_WhenAlreadyBlank()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.Auto };
        _ = sb.WriteLine("first");
        _ = sb.WriteLine();
        _ = sb.WriteLine($"second{CodeStringBuilder.BlankLinePre}");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nsecond\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLinePre_Auto_NoBlankIfNothingWrittenYet()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.Auto };
        _ = sb.WriteLine($"first{CodeStringBuilder.BlankLinePre}");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLinePre_FullAuto_InsertsBlankBeforeLine()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.FullAuto };
        _ = sb.WriteLine($"first\nsecond{CodeStringBuilder.BlankLinePre}");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nsecond\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // BlankLinePost – inserts a blank line after the line it appears on
    // -------------------------------------------------------------------------

    [Fact]
    public void BlankLinePost_Auto_InsertsBlankAfterLine()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.Auto };
        _ = sb.WriteLine($"first{CodeStringBuilder.BlankLinePost}");
        _ = sb.WriteLine("second");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nsecond\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLinePost_Auto_NoDuplicateBlank_WhenNextLineAlreadyBlank()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.Auto };
        _ = sb.WriteLine($"first{CodeStringBuilder.BlankLinePost}");
        _ = sb.WriteLine();
        _ = sb.WriteLine("second");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nsecond\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLinePost_Auto_NoBlankIfNothingFollows()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.Auto };
        _ = sb.WriteLine($"first{CodeStringBuilder.BlankLinePost}");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLinePost_FullAuto_InsertsBlankAfterLine()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.FullAuto };
        _ = sb.WriteLine($"first{CodeStringBuilder.BlankLinePost}\nsecond");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nsecond\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLinePost_FullAuto_AcrossCalls_InsertsBlankAfterLine()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.FullAuto };
        _ = sb.WriteLine($"first{CodeStringBuilder.BlankLinePost}");
        _ = sb.WriteLine("second");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nsecond\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // IndentIncreasePre – FullAuto applies per-line before writing
    // -------------------------------------------------------------------------

    [Fact]
    public void IndentIncreasePre_FullAuto_IncreasesIndentBeforeWrite()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.FullAuto,
            Indents = 0
        };
        _ = sb.WriteLine($"first{CodeStringBuilder.IndentIncreasePre}\nsecond");

        string result = sb.ToString();
        output.WriteLine(result);

        // IndentIncreasePre fires before writing first, so first gets 4 spaces; second also at 1
        Assert.Equal("    first\r\n    second\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // IndentDecreasePre – FullAuto applies per-line before writing
    // -------------------------------------------------------------------------

    [Fact]
    public void IndentDecreasePre_FullAuto_DecreasesIndentBeforeWrite()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.FullAuto,
            Indents = 2
        };
        _ = sb.WriteLine($"dedented{CodeStringBuilder.IndentDecreasePre}\nnext");

        string result = sb.ToString();
        output.WriteLine(result);

        // IndentDecreasePre fires before writing dedented, so it gets 4 spaces (indent 1); next also at 1
        Assert.Equal("    dedented\r\n    next\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // IndentIncreasePost – FullAuto applies per-line after writing
    // -------------------------------------------------------------------------

    [Fact]
    public void IndentIncreasePost_FullAuto_IncreasesIndentAfterWrite()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.FullAuto,
            Indents = 0
        };
        _ = sb.WriteLine($"open{CodeStringBuilder.IndentIncreasePost}\nbody");

        string result = sb.ToString();
        output.WriteLine(result);

        // open written at indent 0, then indent increases; body written at indent 1
        Assert.Equal("open\r\n    body\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // NoIndentChar + control char combined in FullAuto
    // Control chars are processed before NoIndentChar check, so they still fire
    // -------------------------------------------------------------------------

    [Fact]
    public void NoIndentChar_FullAuto_WithBlankLinePost_BlankStillFires()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.FullAuto,
            NoIndentChar = '#',
            Indents = 1
        };
        _ = sb.WriteLine($"#pragma{CodeStringBuilder.BlankLinePost}");
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        // 'next' does not start with NoIndentChar so it still receives indentation
        Assert.Equal("#pragma\r\n\r\n    next\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void NoIndentChar_FullAuto_WithIndentIncreasePost_IndentStillChanges()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.FullAuto,
            NoIndentChar = '#',
            Indents = 0
        };
        _ = sb.WriteLine($"#pragma{CodeStringBuilder.IndentIncreasePost}\nbody");

        string result = sb.ToString();
        output.WriteLine(result);

        // #pragma written without indent, then indent increases; body at indent 1
        Assert.Equal("#pragma\r\n    body\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // BlankLinePre – FullAuto across separate calls
    // -------------------------------------------------------------------------

    [Fact]
    public void BlankLinePre_FullAuto_AcrossCalls_InsertsBlankBeforeLine()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.FullAuto };
        _ = sb.WriteLine("first");
        _ = sb.WriteLine($"second{CodeStringBuilder.BlankLinePre}");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nsecond\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // BlankLinePost wins over ForceBlankLineAfterOff = false
    // -------------------------------------------------------------------------

    [Fact]
    public void BlankLinePost_Auto_WinsOverForceBlankLineAfterOffFalse()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.Auto,
            ForceIndentOn = '{',
            ForceIndentOff = '}',
            ForceBlankLineAfterOff = false,
        };
        _ = sb.WriteLine($"}}{CodeStringBuilder.BlankLinePost}");
        _ = sb.WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        // ForceBlankLineAfterOff is false but BlankLinePost overrides it
        Assert.Equal("}\r\n\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // IndentControl.None – pending blank is consumed by next non-None write
    // -------------------------------------------------------------------------

    [Fact]
    public void PendingBlank_AfterNoneWrite_ConsumedByNextWrite()
    {
        var sb = new CodeStringBuilder
        {
            IndentSize = 4,
            IndentControl = IndentControl.Auto,
            Indents = 0
        };
        _ = sb.WriteLine("first");
        _ = sb.ForceBlankLine();
        _ = sb.WriteLine("middle", IndentControl.None);
        _ = sb.WriteLine("last");

        string result = sb.ToString();
        output.WriteLine(result);

        // IndentControl.None skips pending-blank processing; the blank fires before the next Auto write
        Assert.Equal("first\r\nmiddle\r\n\r\nlast\r\n", result, ignoreLineEndingDifferences: true);
    }

    // -------------------------------------------------------------------------
    // ForceBlankLine() – direct test of the public method
    // -------------------------------------------------------------------------

    [Fact]
    public void ForceBlankLine_InsertsBlankBeforeNextLine()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.Auto };
        _ = sb.WriteLine("first");
        _ = sb.ForceBlankLine();
        _ = sb.WriteLine("second");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nsecond\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceBlankLine_NoBlankIfNothingWrittenYet()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.Auto };
        _ = sb.ForceBlankLine();
        _ = sb.WriteLine("first");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceBlankLine_NoDuplicateBlank_WhenAlreadyBlank()
    {
        var sb = new CodeStringBuilder { IndentControl = IndentControl.Auto };
        _ = sb.WriteLine("first");
        _ = sb.WriteLine();
        _ = sb.ForceBlankLine();
        _ = sb.WriteLine("second");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nsecond\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ForceBlankLine_ReturnsThis_AllowsChaining()
    {
        var sb = new CodeStringBuilder();
        CodeStringBuilder returned = sb.ForceBlankLine();

        Assert.Same(sb, returned);
    }

    // -------------------------------------------------------------------------
    // BlankLineOnly – behaves like None but honours pending blank lines
    // -------------------------------------------------------------------------

    [Fact]
    public void BlankLineOnly_NoPendingBlank_WritesLineAsIs()
    {
        var sb = new CodeStringBuilder { Indents = 1 };
        _ = sb.WriteLine("text", IndentControl.BlankLineOnly);

        string result = sb.ToString();
        output.WriteLine(result);

        // No pending blank; written without indent, exactly as None would
        Assert.Equal("text\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLineOnly_WithPendingBlank_EmitsBlankFirst()
    {
        // Write something first so ForceBlankLine() is not a no-op, then
        // use BlankLineOnly to check the pending blank fires before the line.
        var sb = new CodeStringBuilder();
        _ = sb.WriteLine("first")
              .ForceBlankLine()
              .WriteLine("body", IndentControl.BlankLineOnly);

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nbody\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLineOnly_PendingBlankFromForceBlankLineAfterOff_EmitsBlankFirst()
    {
        // ForceBlankLineAfterOff leaves a pending blank; BlankLineOnly should fire it
        var sb = new CodeStringBuilder
        {
            IndentControl = IndentControl.Auto,
            ForceIndentOff = '}',
            ForceBlankLineAfterOff = true
        };
        _ = sb.WriteLine("}")
              .WriteLine("#pragma warning restore", IndentControl.BlankLineOnly);

        string result = sb.ToString();
        output.WriteLine(result);

        // Blank fired before the BlankLineOnly line; the line itself has no indent
        Assert.Equal("}\r\n\r\n#pragma warning restore\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLineOnly_NoPendingBlank_DoesNotAddExtraBlank()
    {
        var sb = new CodeStringBuilder { Indents = 2 };
        _ = sb.WriteLine("a")
              .WriteLine("b", IndentControl.BlankLineOnly)
              .WriteLine("c", IndentControl.BlankLineOnly);

        string result = sb.ToString();
        output.WriteLine(result);

        // No pending blanks set so consecutive BlankLineOnly writes are adjacent
        Assert.Equal("        a\r\nb\r\nc\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLineOnly_WhitespaceOnlyText_WritesBlankLine_NoPendingBlankFired()
    {
        var sb = new CodeStringBuilder();
        _ = sb.WriteLine("first")
              .ForceBlankLine()
              .WriteLine("   ", IndentControl.BlankLineOnly)
              .WriteLine("last");

        string result = sb.ToString();
        output.WriteLine(result);

        // Whitespace-only text is written as-is; pending blank is absorbed (not fired first)
        // so the whitespace line itself acts as the separator — no double blank before "last"
        Assert.Equal("first\r\n   \r\nlast\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLineOnly_MultiLine_FirstLineWhitespace_TreatedAsBlank()
    {
        // First line is whitespace-only; subsequent lines have content.
        // The whole write should be treated as blank (first-line check only),
        // so the pending blank is absorbed rather than fired.
        var sb = new CodeStringBuilder();
        _ = sb.WriteLine("first")
              .ForceBlankLine()
              .WriteLine("   \nsecond", IndentControl.BlankLineOnly)
              .WriteLine("last");

        string result = sb.ToString();
        output.WriteLine(result);

        // First line whitespace absorbs pending blank; the full text is written as-is
        Assert.Equal("first\r\n   \r\nsecond\r\nlast\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLineOnly_MultiLine_FirstLineHasContent_PendingBlankFiredFirst()
    {
        // First line has content so the pending blank must fire before the write.
        var sb = new CodeStringBuilder();
        _ = sb.WriteLine("first")
              .ForceBlankLine()
              .WriteLine("second\nthird", IndentControl.BlankLineOnly)
              .WriteLine("last");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("first\r\n\r\nsecond\r\nthird\r\nlast\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BlankLineOnly_MultiLine_LastLineBlank_ForceBlankLine_NoDuplicateBlank()
    {
        // Write multi-line text ending with a blank line via BlankLineOnly,
        // then ForceBlankLine, then a content line.
        // _lastLineWasBlank should be true after the BlankLineOnly write,
        // so ForceBlankLine is a no-op and no duplicate blank appears.
        var sb = new CodeStringBuilder();
        _ = sb.WriteLine("content\n", IndentControl.BlankLineOnly)
              .ForceBlankLine()
              .WriteLine("next");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("content\r\n\r\nnext\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Variables_ValidReplacements_SingularAndMultiple()
    {
        var sb = new CodeStringBuilder();
        _ = sb.WriteLine("Hello ${name}", new Dictionary<string, string> { ["name"] = "World" });
        _ = sb.WriteLine("Coords:${x}${y}", new Dictionary<string, string> { ["x"] = "1", ["y"] = "2" });

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("Hello World\r\nCoords:12\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Variables_PatternFound_NoMatchingValue_LeftUnchanged()
    {
        var sb = new CodeStringBuilder();
        // Provide an empty variables dictionary so the pattern is evaluated but no replacement is found
        _ = sb.WriteLine("Missing: ${notfound}", new Dictionary<string, string>());

        string result = sb.ToString();
        output.WriteLine(result);

        // The variable should be left unchanged because there is no matching key in the provided dictionary
        Assert.Equal("Missing: ${notfound}\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Variables_CustomPattern_Used()
    {
        var sb = new CodeStringBuilder
        {
            // Use a custom pattern of the form #(name)
            VariablePattern = "\\#\\((?<name>[a-zA-Z0-9_]+)\\)"
        };

        _ = sb.WriteLine("Custom: #(name)", new Dictionary<string, string> { ["name"] = "Alice" });

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("Custom: Alice\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Variables_UsesDefaultVariables_WhenMethodParamNull()
    {
        var sb = new CodeStringBuilder
        {
            DefaultVariables = new Dictionary<string, string> { ["who"] = "Default" }
        };

        // Call overload that does not pass variables (null), DefaultVariables should be used
        _ = sb.WriteLine("Hi ${who}");

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("Hi Default\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Variables_DefaultAndMethodParam_MissingKeysFromParam_AreTakenFromDefault()
    {
        var sb = new CodeStringBuilder
        {
            DefaultVariables = new Dictionary<string, string> { ["b"] = "B" }
        };

        // Provide only 'a' in method param; 'b' should be resolved from DefaultVariables
        _ = sb.WriteLine("AB:${a}${b}", new Dictionary<string, string> { ["a"] = "A" });

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("AB:AB\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Variables_MethodParamOverridesDefault_WhenBothPresent()
    {
        var sb = new CodeStringBuilder
        {
            DefaultVariables = new Dictionary<string, string> { ["x"] = "DefaultX", ["y"] = "DefaultY" }
        };

        // Provide 'x' in method param which should override DefaultVariables; 'y' comes from default
        _ = sb.WriteLine("XY:${x}-${y}", new Dictionary<string, string> { ["x"] = "ParamX" });

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("XY:ParamX-DefaultY\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Variables_ReplacedWithEmptyString_Removed()
    {
        var sb = new CodeStringBuilder();

        _ = sb.WriteLine("A${v}B", new Dictionary<string, string> { ["v"] = string.Empty });

        string result = sb.ToString();
        output.WriteLine(result);

        Assert.Equal("AB\r\n", result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Variables_MultiLineCSharp_IsFormattedAccordingToIndentControl()
    {
        var sb = CodeStringBuilder.CreateCSharpBuilder();

        _ = sb.WriteLine("class Foo");
        _ = sb.WriteLine("{");

        _ = sb.WriteLine("${method}", new Dictionary<string, string>
        {
            ["method"] = "public void Method()\n{\n// Do stuff\n}"
        }, IndentControl.FullAuto);

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
}
