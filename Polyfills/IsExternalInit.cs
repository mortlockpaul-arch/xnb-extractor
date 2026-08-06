// Polyfill for C# init-only setters on frameworks that don't define IsExternalInit
namespace System.Runtime.CompilerServices
{
    // This empty type enables use of 'init' accessors when targeting older frameworks
    // (e.g., .NET Framework) that don't provide System.Runtime.CompilerServices.IsExternalInit.
    internal static class IsExternalInit { }
}
