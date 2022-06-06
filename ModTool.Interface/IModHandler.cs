
namespace ModTool.Interface
{
    /// <summary>
    /// Interface to be implemented by mods that wish to receive OnLoaded and OnUnloaded calls. 
    /// Non-UnityEngine.Object types that implement this interface will be instantiated when the Mod loads.
    /// </summary>
    public interface IModHandler
    {
        /// <summary>
        /// Called when the Mod is loaded.
        /// </summary>
        void OnLoaded();

        /// <summary>
        /// Called when the Mod is unloaded.
        /// </summary>
        void OnUnloaded();
    }
}
