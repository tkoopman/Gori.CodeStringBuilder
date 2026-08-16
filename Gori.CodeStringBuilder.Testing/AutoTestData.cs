using System.Collections;

namespace Gori.CodeStringBuilder.Testing;

public class AutoTestData : IEnumerable<TheoryDataRow<string, string?[]>>
{
    public IEnumerator<TheoryDataRow<string, string?[]>> GetEnumerator()
    {
        // Base indent of the written text is stripped before the builder's current
        // indent is applied, so pre-indented text is normalised correctly.
        yield return new TheoryDataRow<string, string?[]>(
        """
        {
            Line 1
                Line 2
            Line 3
        }

        """,
        [
            "{",
            """
                Line 1
                    Line 2
                Line 3
            """,
            "}",
        ])
        {
            Label = "Multi-line with base indent stripped when nested",
        };

        // Blank lines within the written block are preserved as-is.
        yield return new TheoryDataRow<string, string?[]>(
        """
        {
            Line 1

            Line 2
        }

        """,
        [
            "{",
            """
                Line 1

                Line 2
            """,
            "}",
        ])
        {
            Label = "Blank lines within a multi-line write are preserved",
        };

        // Explicit control chars at the end of the last line still fire, same as AddOnly.
        yield return new TheoryDataRow<string, string?[]>(
        """
        Line 1
            Line 2

        """,
        [
            $"Line 1{CodeStringBuilder.IndentIncreasePost}",
            "Line 2",
        ])
        {
            Label = "Explicit control char at end of last line still fires",
        };

        // A line starting with NoIndentChar is written without the builder indent,
        // after base indent stripping.
        yield return new TheoryDataRow<string, string?[]>(
        """
        {
            Line 1
        #NoIndent
            Line 2
        }

        """,
        [
            "{",
            """
            Line 1
            #NoIndent
            Line 2
            """,
            "}",
        ])
        {
            Label = "NoIndentChar line within multi-line write skips builder indent",
        };

        // When the first non-blank line starts with NoIndentChar it is skipped for
        // base-indent detection; the next eligible line is used instead.
        // Without this rule a NoIndentChar line would always report base indent = 0,
        // causing all other lines to be over-indented.
        yield return new TheoryDataRow<string, string?[]>(
        """
        {
        #NoIndent
            Line 1
            Line 2
        }

        """,
        [
            "{",
            """
            #NoIndent
            Line 1
            Line 2
            """,
            "}",
        ])
        {
            Label = "NoIndentChar first line is skipped when detecting base indent",
        };

        // Only complete indent levels (IndentSize chars each) are stripped.
        // Partial leading spaces beyond the last full level are preserved.
        // Both lines share the same number of complete levels → delta = 0 → Indents unchanged.
        // The following '}' write fires at Indents=1, so it is indented by one level.
        yield return new TheoryDataRow<string, string?[]>(
        """
        {
              Line 1
                Line 2
            }

        """,
        [
            "{",
            """
                  Line 1
                    Line 2
            """,
            "}",
        ])
        {
            Label = "Partial leading spaces beyond a complete level are preserved after stripping",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
