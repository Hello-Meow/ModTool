
namespace ModTool.Interface
{
    /// <summary>
    /// Provides methods for handling loading and unloading of mods.
    /// </summary>
    public interface IModHandler
    {
        /// <summary>
        /// Called when the Mod is loaded.
        /// </summary>
        /// <param name="contentHandler">The Mod's ContentHandler.</param>
        void OnLoaded(ContentHandler contentHandler);

        /// <summary>
        /// Called when the Mod is unloaded.
        /// </summary>
        void OnUnloaded();
    }
}
