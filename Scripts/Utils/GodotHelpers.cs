using Godot;

namespace UsurperRemake.Utils
{
    /// <summary>
    /// Provides a global static replacement for Godot's GetNode<T>() so that legacy code can compile
    /// without inheriting from Godot.Node. Internally this simply resolves or creates singleton
    /// service instances using SystemHelper.
    /// </summary>
    public static class GodotHelpers
    {
        public static T GetNode<T>(string path) where T : class, new() => SystemHelper.GetSystem<T>();
    }
} 