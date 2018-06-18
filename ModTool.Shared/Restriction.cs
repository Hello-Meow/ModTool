using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ModTool.Shared.Verification
{
    /// <summary>
    /// Base class for restrictions. A restriction either requires or prohibits something.
    /// </summary>
    [Serializable]
    public abstract class Restriction
    {
        /// <summary>
        /// The message to use when this restriction causes the verification to fail.
        /// </summary>
        public string message;

        /// <summary>
        /// The base type to which the Restriction will be applied.
        /// </summary>
        public TypeName applicableBaseType;

        /// <summary>
        /// Does the Restriction require or prohibit the use of something?
        /// </summary>
        public RestrictionMode restrictionMode;

        /// <summary>
        /// Initialize a new Restriction.
        /// </summary>
        /// <param name="applicableBaseType">The base Type this Restriction will apply to.</param>
        /// <param name="message">The Message that will be shown upon failing the Restriction.</param>
        /// <param name="restrictionMode">Should it be required or prohibited?</param>
        protected Restriction(TypeName applicableBaseType, string message, RestrictionMode restrictionMode)
        {
            this.applicableBaseType = applicableBaseType;
            this.message = message;
            this.restrictionMode = restrictionMode;
        }

        /// <summary>
        /// Verify a member with this Restriction.
        /// </summary>
        /// <param name="member">A member.</param>
        /// <param name="excludedAssemblies">A List of Assembly names that should be ignored.</param>
        /// <returns>False if the Member fails the verification.</returns>
        public bool Verify(MemberReference member, List<string> excludedAssemblies)
        {
            if (Applicable(member))
            {
                bool present;

                if (member is MethodReference)
                    present = PresentInMethodRecursive(member as MethodReference, excludedAssemblies);
                else
                    present = Present(member);

                if (present)
                {
                    if (restrictionMode == RestrictionMode.Prohibited)
                    {
                        LogMessage(member);
                        return false;
                    }
                }
                else if (restrictionMode == RestrictionMode.Required)
                {
                    LogMessage(member);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Is the Restriction present in the member?
        /// </summary>
        /// <param name="member">A member.</param>
        /// <returns>True if the Restriction is present in the member.</returns>
        protected virtual bool Present(MemberReference member)
        {
            return false;
        }                

        /// <summary>
        /// Is the restriction present in a local variable?
        /// </summary>
        /// <param name="variable">A local variable</param>
        /// <returns>True if the restriction is present in the local variable.</returns>
        protected virtual bool PresentMethodVariable(VariableReference variable)
        {
            return false;
        }     
                
        private bool PresentInMethodRecursive(MethodReference method, List<string> excludedAssemblies)
        {
            HashSet<string> visited = new HashSet<string>();
            return PresentInMethodRecursive(method, excludedAssemblies, visited);
        }

        private bool PresentInMethodRecursive(MethodReference method, List<string> apiAssemblies, HashSet<string> visited)
        {
            MethodDefinition resolvedMethod = null;
            
            try
            {
                resolvedMethod = method.Resolve();
            }
            catch (AssemblyResolutionException e)
            {
                LogUtility.LogWarning(e.Message);
            }

            if (resolvedMethod != null)
            {
                if (!resolvedMethod.HasBody)
                    return false;

                if (visited.Contains(resolvedMethod.FullName))
                    return false;

                visited.Add(resolvedMethod.FullName);

                foreach (VariableDefinition variable in resolvedMethod.Body.Variables)
                {
                    if (PresentMethodVariable(variable))
                        return true;
                }

                foreach (Instruction instruction in resolvedMethod.Body.Instructions)
                {
                    if (instruction.Operand == null)
                        continue;
                                        
                    if (instruction.Operand is MemberReference)
                    {
                        MemberReference member = instruction.Operand as MemberReference;
                        
                        if (member.Module.Assembly.Name.Name == "ModTool.Interface")
                            continue;

                        if (Present(member))
                            return true;

                        if (apiAssemblies.Contains(member.Module.Assembly.Name.Name))
                            continue;

                        if (member.DeclaringType == null)
                            continue;

                        if (member.DeclaringType.Namespace.StartsWith("System"))
                            continue;

                        if (member.DeclaringType.Namespace.StartsWith("Unity"))
                            continue;
                        
                        if (member is MethodReference)
                        {
                            if (PresentInMethodRecursive(member as MethodReference, apiAssemblies, visited))
                                return true;
                        }
                    }
                }
            }

            return false;
        }
                
        /// <summary>
        /// Log this Restriction's message.
        /// </summary>
        /// <param name="member">The Member to include in the message.</param>
        protected virtual void LogMessage(MemberReference member)
        {
            LogUtility.LogWarning(restrictionMode + ": " + member.FullName + " - " + message);
        }
                
        /// <summary>
        /// Is this Restriction applicable to the member?
        /// </summary>
        /// <param name="member">A member.</param>
        /// <returns>True if the Restriction is applicable.</returns>
        protected bool Applicable(MemberReference member)
        {            
            if(member is TypeReference)
            {
                return Applicable(member as TypeReference);
            }

            if (member.DeclaringType == null)
                return false;

            return Applicable(member.DeclaringType);
        }

        /// <summary>
        /// Is this Restriction applicable to the Type?
        /// </summary>
        /// <param name="type">A Type.</param>
        /// <returns>True if the Restriction is applicable.</returns>
        protected bool Applicable(TypeReference type)
        {
            if (applicableBaseType == null)
                return true;

            if (string.IsNullOrEmpty(applicableBaseType.name))
                return true;

            try
            {
                return type.Resolve().IsSubClassOf(applicableBaseType);
            }
            catch (AssemblyResolutionException e)
            {
                LogUtility.LogWarning(e.Message);
            }

            return false;
        }        
    }

    /// <summary>
    /// A restriction that either requires or prohibits the use of a namespace.
    /// </summary>
    [Serializable]
    public class NamespaceRestriction : Restriction
    {
        /// <summary>
        /// The namespace that will be checked for this restriction.
        /// </summary>
        public string nameSpace;

        /// <summary>
        /// Should nested namespaces be restricted as well?
        /// </summary>
        public bool includeNested;

        /// <summary>
        /// Initialize a new NamespaceRestriction
        /// </summary>
        /// <param name="nameSpace">The namespace that will be checked for this restriction.</param>
        /// <param name="includeNested">Should nested namespaces be restricted as well?</param>
        /// <param name="applicableBaseType">The base Type this Restriction will apply to.</param>
        /// <param name="message">The Message that will be shown upon failing the Restriction.</param>
        /// <param name="restrictionMode">Should it be required or prohibited?</param>
        public NamespaceRestriction(string nameSpace, bool includeNested, TypeName applicableBaseType, string message, RestrictionMode restrictionMode) : base(applicableBaseType, message, restrictionMode)
        {
            this.nameSpace = nameSpace;
            this.includeNested = includeNested;
        }

        protected override bool Present(MemberReference member)
        {
            if (member is TypeReference)
                return CheckNamespace((member as TypeReference).Namespace);
            if (member is FieldReference)
                return CheckNamespace((member as FieldReference).FieldType.Namespace);
            if (member is PropertyReference)
                return CheckNamespace((member as PropertyReference).PropertyType.Namespace);

            if (member.DeclaringType == null)
                return false;

            return CheckNamespace(member.DeclaringType.Namespace);
        }

        protected override bool PresentMethodVariable(VariableReference variable)
        {
            return CheckNamespace(variable.VariableType.Namespace);
        }

        private bool CheckNamespace(string nameSpace)
        {
            if (includeNested)
                return nameSpace.StartsWith(this.nameSpace);
            else
                return nameSpace == this.nameSpace;
        }
    }

    /// <summary>
    /// A restriction that either requires or prohibits the use of a Type
    /// </summary>
    [Serializable]
    public class TypeRestriction : Restriction
    {
        /// <summary>
        /// The Type that will be checked for this Restriction.
        /// </summary>
        public TypeName type;

        /// <summary>
        /// Initialize a new TypeRestriction.
        /// </summary>
        /// <param name="type">The Type that will be checked for this Restriction.</param>
        /// <param name="applicableBaseType">The base Type this Restriction will apply to.</param>
        /// <param name="message">The Message that will be shown upon failing the Restriction.</param>
        /// <param name="restrictionMode">Should it be required or prohibited?</param>
        public TypeRestriction(TypeName type, TypeName applicableBaseType, string message, RestrictionMode restrictionMode)
            : base(applicableBaseType, message, restrictionMode)
        {
            this.type = type;
        }

        protected override bool Present(MemberReference member)
        {
            if (member is FieldReference)
            {
                FieldReference field = member as FieldReference;
                return field.FieldType.Name == type.name && field.FieldType.Namespace == type.nameSpace;
            }

            if (member is PropertyReference)
            {
                PropertyReference property = member as PropertyReference;
                return property.PropertyType.Name == type.name && property.PropertyType.Namespace == type.nameSpace;
            }

            if (member.DeclaringType == null)
                return false;

            return member.DeclaringType.Name == type.name && member.DeclaringType.Namespace == type.nameSpace;
        }

        protected override bool PresentMethodVariable(VariableReference variable)
        {
            return (variable.VariableType.Name == type.name && variable.VariableType.Namespace == type.nameSpace);
        }
    }

    /// <summary>
    /// A restriction that either requires or prohibits inheritance from a class
    /// </summary>
    [Serializable]
    public class InheritanceRestriction : Restriction
    {
        /// <summary>
        /// The Type that will be checked for this Restriction.
        /// </summary>
        public TypeName type;

        /// <summary>
        /// Initialize a new InheritanceRestriction.
        /// </summary>
        /// <param name="type">The Type that will be checked for this Restriction</param>
        /// <param name="applicableBaseType">The base Type this Restriction will apply to.</param>
        /// <param name="message">The Message that will be shown upon failing the Restriction.</param>
        /// <param name="restrictionMode">Should it be required or prohibited?</param>
        public InheritanceRestriction(TypeName type, TypeName applicableBaseType, string message, RestrictionMode restrictionMode)
            : base(applicableBaseType, message, restrictionMode)
        {
            this.type = type;
        }

        protected override bool Present(MemberReference member)
        {
            if (member is TypeReference)
            {
                TypeDefinition typeDefinition = (member as TypeReference).Resolve();
                return typeDefinition.IsSubClassOf(type);
            }

            //TODO: What if a Type derives from Type inside Unity that derives from MonoBehaviour?

            return false;
        }
    }

    /// <summary>
    /// Type for applying Restrictions.
    /// </summary>
    public enum RestrictionMode { Prohibited, Required }

    /// <summary>
    /// A restriction that either requires or prohibits the use of a given Type's member
    /// </summary>
    [Serializable]
    public class MemberRestriction : Restriction
    {
        /// <summary>
        /// The member that will be checked for this Restriction.
        /// </summary>
        public MemberName memberName;

        /// <summary>
        /// Initialize a new MemberRestriction.
        /// </summary>
        /// <param name="memberName">The member that will be checked for this Restriction.</param>
        /// <param name="applicableBaseType">The base Type this Restriction will apply to.</param>
        /// <param name="message">The Message that will be shown upon failing the Restriction.</param>
        /// <param name="restrictionMode">Should it be required or prohibited?</param>
        public MemberRestriction(MemberName memberName, TypeName applicableBaseType, string message, RestrictionMode restrictionMode)
            : base(applicableBaseType, message, restrictionMode)
        {
            this.memberName = memberName;
        }

        protected override bool Present(MemberReference member)
        {
            if (member.DeclaringType == null)
                return member.Name == memberName.name;

            return (member.Name == memberName.name && member.DeclaringType.Name == memberName.type.name);
        }
    }
}
