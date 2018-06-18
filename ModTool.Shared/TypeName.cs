using System;

namespace ModTool.Shared.Verification
{
    /// <summary>
    /// Represents a Type's name.
    /// </summary>
    [Serializable]
    public class TypeName
    {
        /// <summary>
        /// The Type's namespace.
        /// </summary>
        public string nameSpace = "";

        /// <summary>
        /// The Type's name.
        /// </summary>
        public string name = "";

        /// <summary>
        /// Initialize a new TypeName.
        /// </summary>
        /// <param name="nameSpace">The Type's namespace.</param>
        /// <param name="name">The Type's name.</param>
        public TypeName(string nameSpace, string name)
        {
            this.nameSpace = nameSpace;
            this.name = name;
        }        

        public TypeName()
        {

        }
    }
}
