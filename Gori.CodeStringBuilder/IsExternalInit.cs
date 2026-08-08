#if NETSTANDARD2_0
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0161 // Convert to file-scoped namespace
namespace System.Runtime.CompilerServices
#pragma warning restore IDE0161 // Convert to file-scoped namespace
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage, DebuggerNonUserCode]
    internal static class IsExternalInit { }
}
#endif