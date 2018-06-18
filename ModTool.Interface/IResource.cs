
namespace ModTool.Interface
{
    /// <summary>
    /// A loadable resource
    /// </summary>
    public interface IResource
    {
        /// <summary>
        /// The resource's name.
        /// </summary>
        string name { get; }

        /// <summary>
        /// Load the Resource.
        /// </summary>
        void Load();

        /// <summary>
        /// Unload the resource.
        /// </summary>
        void Unload();

        /// <summary>
        /// Load the resource asynchronously.
        /// </summary>
        void LoadAsync();
    }
}
