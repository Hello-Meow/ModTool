using System;
using System.Collections.Generic;
using UnityEngine;
using ModTool.Shared.Verification;


namespace ModTool.Shared
{
    /// <summary>
    /// Stores settings related to the game's API and code verification.
    /// </summary>
    public class CodeSettings : ScriptableSingleton<CodeSettings>
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

        /// <summary>
        /// List of the Game's api Assembly names.
        /// </summary>
        public static List<string> apiAssemblies
        {
            get
            {
                return instance._apiAssemblies;
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

        [SerializeField]
        private List<string> _apiAssemblies = new List<string>();
        
        protected CodeSettings()
        {
            memberRestrictions.Add(new MemberRestriction(new MemberName(new TypeName("UnityEngine", "Object"), "Instantiate"), null, "Please use ModBehaviour.Instantiate or ContentHandler.Instantiate instead to ensure proper object creation.", RestrictionMode.Prohibited));
            memberRestrictions.Add(new MemberRestriction(new MemberName(new TypeName("UnityEngine", "GameObject"), "AddComponent"), null, "Please use ModBehaviour.AddComponent or ContentHandler.AddComponent instead to ensure proper component handling.", RestrictionMode.Prohibited));
            memberRestrictions.Add(new MemberRestriction(new MemberName(new TypeName("UnityEngine", "GameObject"), ".ctor"), null, "Creating new GameObjects is not allowed", RestrictionMode.Prohibited));

            inheritanceRestrictions.Add(new InheritanceRestriction(new TypeName("ModTool.Interface", "ModBehaviour"), new TypeName("UnityEngine", "MonoBehaviour"), "Please use ModTool.Interface.ModBehaviour instead of MonoBehaviour.", RestrictionMode.Required));

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
