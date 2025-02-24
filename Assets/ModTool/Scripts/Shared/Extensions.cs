using Mono.Cecil;
using System;
using System.IO;

namespace ModTool.Shared
{
    /// <summary>
    /// Extension methods for enums.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Unity's enum mask fields sets all bits to 1. This sets all unused bits to 0, so it can be converted to a string and serialized properly.
        /// </summary>
        /// <param name="self">An enum instance.</param>
        /// <returns>A fixed enum.</returns>
        public static int FixEnum(this Enum self)
        {
            int bits = 0;
            foreach (var enumValue in Enum.GetValues(self.GetType()))
            {
                int checkBit = Convert.ToInt32(self) & (int)enumValue;
                if (checkBit != 0)
                {
                    bits |= (int)enumValue;
                }
            }

            return bits;
        }       
    }       
    
    /// <summary>
    /// Extension methods for strings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Returns a normalized version of a path.
        /// </summary>
        /// <param name="self">A string.</param>
        /// <returns>A normalized version of a path.</returns>
        public static string NormalizedPath(this string self)
        {
            string normalizedPath = Path.GetFullPath(new Uri(self).LocalPath);
            normalizedPath = normalizedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            normalizedPath = normalizedPath.ToLowerInvariant();
            return normalizedPath;
        }
    }

    /// <summary>
    /// Extension methods for Mono.Cecil.
    /// </summary>
    public static class CecilExtensions
    {
        /// <summary>
        /// Is this Type a subclass of the other Type?
        /// </summary>
        /// <param name="self">A TypeDefinition.</param>
        /// <param name="typeName">A Type's full name.</param>
        /// <returns>True if this TypeDefinition is a subclass of the Type.</returns>
        public static bool IsSubClassOf(this TypeDefinition self, string typeName)
        {
            TypeDefinition type = self;

            while (type != null)
            {
                if (type.BaseType != null)
                {
                    try
                    {
                        type = type.BaseType.Resolve();
                    }
                    catch (AssemblyResolutionException e)
                    {
                        LogUtility.LogWarning("Could not resolve " + e.AssemblyReference.Name + " in IsSubClassOf().");
                        return false;
                    }
                }
                else
                    type = null;

                if (type?.GetFullName() == typeName)
                    return true;                
            }

            return false;
        }

        /// <summary>
        /// Get the Type's name including the Type's namespace.
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string GetFullName(this TypeReference self)
        {
            string fullName = self.Name;

            if (string.IsNullOrEmpty(self.Namespace))
                return fullName;

            return self.Namespace + "." + fullName;
        }

        /// <summary>
        /// Get the Member's name including the declaring Type's full name.
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string GetFullName(this MemberReference self)
        {
            string name = self.Name;

            if (self.DeclaringType == null)
                return name;

            return self.DeclaringType.GetFullName() + "." + name;
        }
    }
}
