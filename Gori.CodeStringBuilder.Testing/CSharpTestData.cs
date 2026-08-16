using System.Collections;

namespace Gori.CodeStringBuilder.Testing;

public class CSharpTestData : IEnumerable<TheoryDataRow<string, string?[]>>
{
    public IEnumerator<TheoryDataRow<string, string?[]>> GetEnumerator()
    {
        yield return new TheoryDataRow<string, string?[]>("""
        Line 1
        {
            Line 2
        }

        Line 3

        """,
        [
            """
            Line 1
            {
                Line 2
            }

            Line 3
            """
        ])
        {
            Label = "Single multi-lined write",
        };

        yield return new TheoryDataRow<string, string?[]>("""
        Line 1
        {
            Line 2
        }

        Line 3

        """,
        [
            "Line 1",
            "{",
            "Line 2",
            "}",
            null,
            "Line 3",
        ])
        {
            Label = "Multiple single-lined writes",
        };

        yield return new TheoryDataRow<string, string?[]>(
        """
        Pre-Line 1
        {
            Line 1
            {
                Line 2
            }

            Line 3
            Post-Line 1
        }

        Post-Line 2

        """,
        [
            "Pre-Line 1",
            "{",
            """
            Line 1
            {
                Line 2
            }

            Line 3
            """,
            "Post-Line 1",
            "}",
            "Post-Line 2",
        ])
        {
            Label = "Multiple mixed-lined writes",
        };

        yield return new TheoryDataRow<string, string?[]>(
        """
        Pre-Line 1
        {
            Line 1
            {
                Line 2
            }

            Line 3
            Post-Line 1
        }

        Post-Line 2

        """,
        [
            """
            Pre-Line 1
            {
            """,
            """
            Line 1
            {
                Line 2
            }

            Line 3
            """,
            """
                Post-Line 1
            }
            """,
            "Post-Line 2",
        ])
        {
            Label = "Multiple mixed-lined writes, with indent controls at end of multi-lined writes",
        };

        yield return new TheoryDataRow<string, string?[]>(
        """
        Pre-Line 1
        {
            Line 1
            {
                Line 2
            }

            Line 3
            Post-Line 1
        }

            Post-Line 2

        """,
        [
            """
            Pre-Line 1
            {
            """,
            """
            Line 1
            {
                Line 2
            }

            Line 3
            """,
            """
                Post-Line 1
            }
            """,
            $"Post-Line 2{CodeStringBuilder.IndentIncreasePre}",
        ])
        {
            Label = "Multiple mixed-lined writes, with indent controls at end of multi-lined writes and control char",
        };
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
