using Mono.Cecil;
using ModTool.Shared.Verification;
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
        /// Unity's enum mask fields set all bits to 1. This sets all unused bits to 0, so it can be converted to a string and serialized properly.
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
        /// Is this path a sub directory or the same directory of another path?
        /// </summary>
        /// <param name="self">A string.</param>
        /// <param name="other">A path.</param>
        /// <returns>True if this string is a sub directory or the same directory as the other.</returns>
        public static bool IsDirectoryOrSubdirectory(this string self, string other)
        {
            var isChild = false;
            try
            {
                var candidateInfo = new DirectoryInfo(self);
                var otherInfo = new DirectoryInfo(other);

                if (candidateInfo.FullName == otherInfo.FullName)
                    return true;

                while (candidateInfo.Parent != null)
                {
                    if (candidateInfo.Parent.FullName == otherInfo.FullName)
                    {
                        isChild = true;
                        break;
                    }
                    else candidateInfo = candidateInfo.Parent;
                }
            }
            catch (Exception e)
            {
                var message = String.Format("Unable to check directories {0} and {1}: {2}", self, other, e);
                LogUtility.LogWarning(message);
            }

            return isChild;
        }

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
        /// <param name="other">A TypeDefinition.</param>
        /// <returns>True if this TypeDefinition is a subclass of the other TypeDefinition.</returns>
        public static bool IsSubClassOf(this TypeDefinition self, TypeName other)
        {
            return self.IsSubClassOf(other.nameSpace, other.name);
        }

        /// <summary>
        /// Is this Type a subclass of the other Type?
        /// </summary>
        /// <param name="self">A TypeDefinition.</param>
        /// <param name="namespace">A Type's namespace.</param>
        /// <param name="name">A Type's name.</param>
        /// <returns>True if this TypeDefinition is a subclass of the Type.</returns>
        public static bool IsSubClassOf(this TypeDefinition self, string @namespace, string name)
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

                if (type != null)
                    if (type.Namespace == @namespace && type.Name == name)
                        return true;
            }

            return false;
        }

        /// <summary>
        /// Get the first method that matches with methodName.
        /// </summary>
        /// <param name="self">A TypeDefinition.</param>
        /// <param name="methodName">A method's name</param>
        /// <returns>The MethodDefinition for the method, if found. Null otherwise.</returns>
        public static MethodDefinition GetMethod(this TypeDefinition self, string methodName)
        {
            foreach (MethodDefinition method in self.Methods)
            {
                if (method.Name == methodName)
                    return method;
            }

            return null;
        }

        /// <summary>
        /// Get the first field that matches with fieldName.
        /// </summary>
        /// <param name="self">A TypeDefinition.</param>
        /// <param name="fieldName">The FieldDefinition for the field, if found. Null otherwise.</param>
        /// <returns>The FieldDefinition, or null of none was found.</returns>
        public static FieldDefinition GetField(this TypeDefinition self, string fieldName)
        {
            foreach (FieldDefinition field in self.Fields)
            {
                if (field.Name == fieldName)
                    return field;
            }

            return null;
        }

        /// <summary>
        /// Get the first property that matches with propertyName.
        /// </summary>
        /// <param name="self">A TypeDefinition.</param>
        /// <param name="propertyName">The PropertyDefinition for the field, if found. Null otherwise.</param>
        /// <returns>The PropertyDefinition, or null of none was found.</returns>
        public static PropertyDefinition GetProperty(this TypeDefinition self, string propertyName)
        {
            foreach(PropertyDefinition property in self.Properties)
            {
                if (property.Name == propertyName)
                    return property;
            }

            return null;
        }  
    }
}
