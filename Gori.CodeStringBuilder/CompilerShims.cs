// Polyfill types that enable the 'init' keyword on netstandard2.0.
// The C# compiler only requires these types to exist; it never calls them at runtime.
#if NETSTANDARD2_0
#pragma warning disable IDE0130, IDE0161, SA1502, SA1600, SA1649
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
    [DebuggerNonUserCode]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    internal static class IsExternalInit { }
}
#endif
