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
        public string targetType;

        /// <summary>
        /// Does the Restriction require or prohibit the use of something?
        /// </summary>
        public RestrictionMode restrictionMode;

        /// <summary>
        /// Initialize a new Restriction.
        /// </summary>
        /// <param name="targetType">The base Type this Restriction will apply to.</param>
        /// <param name="message">The Message that will be shown upon failing the Restriction.</param>
        /// <param name="restrictionMode">Should it be required or prohibited?</param>
        protected Restriction(string targetType, string message, RestrictionMode restrictionMode)
        {
            this.targetType = targetType;
            this.message = message;
            this.restrictionMode = restrictionMode;
        }

        /// <summary>
        /// Verify a member with this Restriction.
        /// </summary>
        /// <param name="member">A member.</param>
        /// <param name="messages">A list of messages of failed Restrictions.</param>
        public void Verify(MemberReference member, List<string> messages)
        {
            if (!Applicable(member))
                return;

            bool present;

            if (member is MethodReference)
                present = PresentInMethodRecursive(member as MethodReference);
            else
                present = Present(member);

            if (present && restrictionMode == RestrictionMode.Prohibited)
                messages.Add(GetMessage(member));

            if (!present &&restrictionMode == RestrictionMode.Required)
                messages.Add(GetMessage(member));
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
                
        private bool PresentInMethodRecursive(MethodReference method)
        {
            HashSet<string> visited = new HashSet<string>();

            return PresentInMethodRecursive(method, visited);
        }

        private bool PresentInMethodRecursive(MethodReference method, HashSet<string> visited)
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
                                                
                        if (member.DeclaringType == null)
                            continue;

                        if (member.DeclaringType.Namespace.StartsWith("System"))
                            continue;

                        if (member.DeclaringType.Namespace.StartsWith("Unity"))
                            continue;

                        if (AssemblyUtility.IsShared(member.Module.Assembly.Name.Name))
                            continue;

                        if (member is MethodReference)
                        {
                            if (PresentInMethodRecursive(member as MethodReference, visited))
                                return true;
                        }
                    }
                }
            }

            return false;
        }        

        /// <summary>
        /// Get a Restriction message for a MemberReference.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        protected virtual string GetMessage(MemberReference member)
        {
            return string.Format("{0}: {1} - {2}", restrictionMode, member.FullName, message);
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
            if (targetType == null)
                return true;

            if (string.IsNullOrEmpty(targetType))
                return true;

            try
            {
                return type.Resolve().IsSubClassOf(targetType);
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
        /// <param name="targetType">The base Type this Restriction will apply to.</param>
        /// <param name="message">The Message that will be shown upon failing the Restriction.</param>
        /// <param name="restrictionMode">Should it be required or prohibited?</param>
        public NamespaceRestriction(string nameSpace, bool includeNested, string targetType, string message, RestrictionMode restrictionMode) : base(targetType, message, restrictionMode)
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
        public string type;

        /// <summary>
        /// Initialize a new TypeRestriction.
        /// </summary>
        /// <param name="type">The Type that will be checked for this Restriction.</param>
        /// <param name="targetType">The base Type this Restriction will apply to.</param>
        /// <param name="message">The Message that will be shown upon failing the Restriction.</param>
        /// <param name="restrictionMode">Should it be required or prohibited?</param>
        public TypeRestriction(string type, string targetType, string message, RestrictionMode restrictionMode)
            : base(targetType, message, restrictionMode)
        {
            this.type = type;
        }

        protected override bool Present(MemberReference member)
        {
            if (member is FieldReference)
            {
                FieldReference field = member as FieldReference;
                return field.FieldType.GetFullName() == type;
            }

            if (member is PropertyReference)
            {
                PropertyReference property = member as PropertyReference;
                return property.PropertyType.GetFullName() == type;
            }

            if (member.DeclaringType == null)
                return false;

            return member.DeclaringType.GetFullName() == type;
        }

        protected override bool PresentMethodVariable(VariableReference variable)
        {
            return (variable.VariableType.GetFullName() == type);
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
        public string type;

        /// <summary>
        /// Initialize a new InheritanceRestriction.
        /// </summary>
        /// <param name="type">The Type that will be checked for this Restriction</param>
        /// <param name="targetType">The base Type this Restriction will apply to.</param>
        /// <param name="message">The Message that will be shown upon failing the Restriction.</param>
        /// <param name="restrictionMode">Should it be required or prohibited?</param>
        public InheritanceRestriction(string type, string targetType, string message, RestrictionMode restrictionMode)
            : base(targetType, message, restrictionMode)
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
        public string member;

        /// <summary>
        /// Initialize a new MemberRestriction.
        /// </summary>
        /// <param name="member">The member that will be checked for this Restriction.</param>
        /// <param name="targetType">The base Type this Restriction will apply to.</param>
        /// <param name="message">The Message that will be shown upon failing the Restriction.</param>
        /// <param name="restrictionMode">Should it be required or prohibited?</param>
        public MemberRestriction(string member, string targetType, string message, RestrictionMode restrictionMode)
            : base(targetType, message, restrictionMode)
        {
            this.member = member;
        }

        protected override bool Present(MemberReference member)
        {
            return member.GetFullName() == this.member;
        }
    }
}
