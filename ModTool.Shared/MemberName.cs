using System;

namespace ModTool.Shared.Verification
{
    /// <summary>
    /// Represents a member's name.
    /// </summary>
    [Serializable]
    public class MemberName
    {
        /// <summary>
        /// The Type to which the member belongs.
        /// </summary>
        public TypeName type = new TypeName();

        /// <summary>
        /// The member's name.
        /// </summary>
        public string name = "";

        /// <summary>
        /// Initialize a new MemberName.
        /// </summary>
        /// <param name="type">The Type to which the member belongs.</param>
        /// <param name="name">The member's name.</param>
        public MemberName(TypeName type, string name)
        {
            this.type = type;
            this.name = name;
        }

        public MemberName()
        {

        }
    }        
}
