using System.Runtime.CompilerServices;

namespace UnityExposed
{
    // This type exists because I can't figure out how I emit the proper IL to get typeof(T)
    // without ending up with unwanted references to different dotnet runtimes than the
    // one that Unity is using. Probably easy to fix, yet I have no idea how!
    public static class TypeHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Type GetType<T>() => typeof(T);
    }
}