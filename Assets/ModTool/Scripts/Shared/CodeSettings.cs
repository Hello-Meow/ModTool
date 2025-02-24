using System.Collections.Generic;
using UnityEngine;
using ModTool.Shared.Verification;


namespace ModTool.Shared
{
    /// <summary>
    /// Stores settings related to code verification for Mods.
    /// </summary>
    public class CodeSettings : Singleton<CodeSettings>
    {
        /// <summary>
        /// Restrictions related to inheritance of Types inside Mod Assemblies.
        /// </summary>
        public static List<InheritanceRestriction> inheritanceRestrictions
        {
            get
            {
                return instance._inheritanceRestrictions;
            }
        }

        /// <summary>
        /// Restrictions related to the use of fields, properties and methods from other types.
        /// </summary>
        public static List<MemberRestriction> memberRestrictions
        {
            get
            {
                return instance._memberRestrictions;
            }
        }

        /// <summary>
        /// Restrictions related to the use of Types for fields and properties.
        /// </summary>
        public static List<TypeRestriction> typeRestrictions
        {
            get
            {
                return instance._typeRestrictions;
            }
        }

        /// <summary>
        /// Restrictions related to the use of entire namespaces.
        /// </summary>
        public static List<NamespaceRestriction> namespaceRestrictions
        {
            get
            {
                return instance._namespaceRestrictions;
            }
        }

        [SerializeField]
        private List<InheritanceRestriction> _inheritanceRestrictions = new List<InheritanceRestriction>();

        [SerializeField]
        private List<MemberRestriction> _memberRestrictions = new List<MemberRestriction>();

        [SerializeField]
        private List<TypeRestriction> _typeRestrictions = new List<TypeRestriction>();

        [SerializeField]
        private List<NamespaceRestriction> _namespaceRestrictions = new List<NamespaceRestriction>();
        
        protected CodeSettings()
        {
            namespaceRestrictions.Add(new NamespaceRestriction("System.Reflection", true, null, "Reflection is not allowed.", RestrictionMode.Prohibited));
            namespaceRestrictions.Add(new NamespaceRestriction("System.IO", true, null, "IO is not allowed.", RestrictionMode.Prohibited));
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            GetInstance();
        }
    }
}
