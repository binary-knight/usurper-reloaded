using System;

namespace UsurperRemake
{
    /// <summary>
    /// Simple DI-less singleton wrappers for core systems where a proper dependency
    /// injection container is not yet available.
    /// </summary>
    public static class GodSystemSingleton
    {
        private static GodSystem? _instance;
        public static GodSystem Instance => _instance ??= new GodSystem();
    }
} 