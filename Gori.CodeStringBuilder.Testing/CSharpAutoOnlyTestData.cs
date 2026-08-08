using System.Collections;

namespace Gori.CodeStringBuilder.Testing;

public class CSharpAutoOnlyTestData : IEnumerable<TheoryDataRow<string, string?[]>>
{
    public IEnumerator<TheoryDataRow<string, string?[]>> GetEnumerator ()
    {
        yield return new TheoryDataRow<string, string?[]>("""
        Line 1
            {
                Line 2
            }
        
        Line 3

        """,
        [
            $$"""
            Line 1{{CodeStringBuilder.Increase}}
            {
                Line 2
            }

            Line 3{{CodeStringBuilder.Decrease}}
            """
        ])
        {
            Label = "Single multi-lined write with control chars",
        };

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
            Label = "Single multi-lined write no formatting",
        };

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
            Label = "Single multi-lined write random formatting",
        };
    }
    IEnumerator IEnumerable.GetEnumerator () => GetEnumerator();
}
