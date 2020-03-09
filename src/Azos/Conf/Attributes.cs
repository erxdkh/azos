/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Azos.Conf
{
    /// <summary>
    /// Decorates classes or structs that act as a context object for macro evaluation - passed as context parameter to MacroRunner.Run(...context) method
    /// </summary>
    [AttributeUsage(AttributeTargets.Class |
                    AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public class ConfigMacroContextAttribute : Attribute
    {
       public ConfigMacroContextAttribute()
       {
       }
    }




    /// <summary>
    /// Specifies how to apply configuration values to classes/fields/props
    /// </summary>
    [AttributeUsage(AttributeTargets.Class |
                    AttributeTargets.Struct |
                    AttributeTargets.Field |
                    AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class ConfigAttribute : Attribute
    {

       /// <summary>
       /// Decorates members so that path is defaulted from member's name with prepended '$' attribute symbol
       /// </summary>
       public ConfigAttribute()
       {
       }

       /// <summary>
       /// Decorates members that will be configured from supplied path
       /// </summary>
       public ConfigAttribute(string path)
       {
         Path = path;
       }

       /// <summary>
       /// Decorates members that will be configured from supplied path and defaulted in case
       ///  the supplied path does not resolve to existing node
       /// </summary>
       public ConfigAttribute(string path, object defaultValue)
       {
         Path = path;
         Default = defaultValue;
       }

       /// <summary>
       /// String path of configuration source i.e. 'log/machine-name'.
       /// Path is relative to item root
       /// </summary>
       public string Path{ get; set;}

       /// <summary>
       /// Default value used when configuration does not specify any other value
       /// </summary>
       public object Default { get; set;}


       /// <summary>
       /// Takes verbatim value if true
       /// </summary>
       public bool Verbatim { get; set;}


       /// <summary>
       /// Applies config values to fields/properties as specified by config attributes
       /// </summary>
       public static T Apply<T>(T entity, IConfigSectionNode node)
       {
          return (T)Apply((object)entity, node);
       }

       /// <summary>
       /// Applies config values to fields/properties as specified by config attributes
       /// </summary>
       public static object Apply(object entity, IConfigSectionNode node)
       {
         if (entity==null || node==null) return entity;

         var etp = entity.GetType();

         //20130708 DKh - support for [ConfigMacroContext] injection
         var macroAttr = etp.GetCustomAttributes(typeof(ConfigMacroContextAttribute), true).FirstOrDefault() as ConfigMacroContextAttribute;
         if (macroAttr!=null)
             node.Configuration.MacroRunnerContext = entity;
         //---

         var cattr = etp.GetCustomAttributes(typeof(ConfigAttribute), true).FirstOrDefault() as ConfigAttribute;

         if (cattr!=null)//rebase root config node per supplied path
         {
           cattr.evalAttributeVars(etp);

           var path = cattr.Path ?? CoreConsts.NULL_STRING;
           node = node.Navigate(path) as ConfigSectionNode;
           if (node==null)
            throw new ConfigException(string.Format(StringConsts.CONFIGURATION_NAVIGATION_REQUIRED_ERROR, cattr.Path));
         }

         var members =  getAllFieldsOrProps( etp );
         foreach(var mem in members)
         {
           var mattr = mem.GetCustomAttributes(typeof(ConfigAttribute), true).FirstOrDefault() as ConfigAttribute;
           if (mattr==null) continue;

           //default attribute name taken from member name if path==null
           if (string.IsNullOrWhiteSpace(mattr.Path))
                mattr.Path =  GetConfigPathsForMember(mem);

           mattr.evalAttributeVars(etp);

           var mnode = node.Navigate(mattr.Path);

           if (mem.MemberType == MemberTypes.Field)
           {
                 var finf = (FieldInfo)mem;

                 if (typeof(IConfigSectionNode).IsAssignableFrom(finf.FieldType))
                 {
                       if (finf.IsInitOnly)
                         throw new ConfigException(string.Format(StringConsts.CONFIGURATION_ATTRIBUTE_MEMBER_READONLY_ERROR, etp.FullName, finf.Name));

                       var snode = mnode as IConfigSectionNode;
                       if (snode==null)
                          throw new ConfigException(string.Format(StringConsts.CONFIGURATION_PATH_ICONFIGSECTION_SECTION_ERROR, etp.FullName, finf.Name));
                       finf.SetValue(entity, mnode);
                 }
                 else
                 if (typeof(IConfigurable).IsAssignableFrom(finf.FieldType))
                 {
                       var snode = mnode as IConfigSectionNode;
                       if (snode==null)
                          throw new ConfigException(string.Format(StringConsts.CONFIGURATION_PATH_ICONFIGURABLE_SECTION_ERROR, etp.FullName, finf.Name));

                       if (finf.GetValue(entity)!=null)
                         ((IConfigurable)finf.GetValue(entity)).Configure(snode);
                 }
                 else
                 {
                       if (finf.IsInitOnly)
                         throw new ConfigException(string.Format(StringConsts.CONFIGURATION_ATTRIBUTE_MEMBER_READONLY_ERROR, etp.FullName, finf.Name));

                       if (mnode.Exists && mnode.VerbatimValue!=null)
                        finf.SetValue(entity, getVal(mnode, finf.FieldType, etp.FullName, finf.Name, mattr.Verbatim));
                       else
                        if (mattr.Default!=null) finf.SetValue(entity, mattr.Default);

                 }

           }
           else
           if (mem.MemberType == MemberTypes.Property)
           {
                 var pinf = (PropertyInfo)mem;

                 if (typeof(IConfigSectionNode).IsAssignableFrom(pinf.PropertyType))
                 {
                       if (!pinf.CanWrite)
                         throw new ConfigException(string.Format(StringConsts.CONFIGURATION_ATTRIBUTE_MEMBER_READONLY_ERROR, etp.FullName, pinf.Name));

                       var snode = mnode as IConfigSectionNode;
                       if (snode==null)
                          throw new ConfigException(string.Format(StringConsts.CONFIGURATION_PATH_ICONFIGSECTION_SECTION_ERROR, etp.FullName, pinf.Name));
                       pinf.SetValue(entity, mnode, null);
                 }
                 else
                 if (typeof(IConfigurable).IsAssignableFrom(pinf.PropertyType))
                 {
                       var snode = mnode as IConfigSectionNode;
                       if (snode==null)
                          throw new ConfigException(string.Format(StringConsts.CONFIGURATION_PATH_ICONFIGURABLE_SECTION_ERROR, etp.FullName, pinf.Name));

                       if (pinf.GetValue(entity, null)!=null)
                         ((IConfigurable)pinf.GetValue(entity, null)).Configure(snode);
                 }
                 else
                 {
                       if (!pinf.CanWrite)
                         throw new ConfigException(string.Format(StringConsts.CONFIGURATION_ATTRIBUTE_MEMBER_READONLY_ERROR, etp.FullName, pinf.Name));

                       if (mnode.Exists && mnode.VerbatimValue!=null)
                        pinf.SetValue(entity, getVal(mnode,  pinf.PropertyType, etp.FullName, pinf.Name, mattr.Verbatim), null);
                       else
                        if (mattr.Default!=null) pinf.SetValue(entity, mattr.Default, null);
                 }
           }

         }//for members

        return entity;
       }


       /// <summary>
       /// Generates 2 attribute paths for named member. This first path is just the member name converted to lower case.
       /// The seconfd path is "OR"ed with the first one and is taken from member name where all case transitions are prefixed with "-".
       /// For private fields 'm_' and 's_' prefixes are removed
       /// </summary>
       public static string GetConfigPathsForMember(MemberInfo member)
       {
           var mn = member.Name;

           if (member is FieldInfo fi)
           {
             //the field prefixes are swallowed
             if (mn.Length>2 && (mn.StartsWith("m_", StringComparison.InvariantCulture) ||
                                 mn.StartsWith("s_", StringComparison.InvariantCulture))
                ) mn = mn.Remove(0, 2);
           }

           var sb = new StringBuilder();
           var first = true;
           var plc = false;
           foreach(var c in mn)
           {
                var clc = char.IsLower(c);
                if (!first && !clc && plc && c!='-') sb.Append('-');
                sb.Append(c);
                first = false;
                plc = clc;
           }

           return "${0}|${1}".Args(mn.ToLowerInvariant(), sb.ToString().ToLowerInvariant());
       }

       private static IEnumerable<MemberInfo> getAllFieldsOrProps(Type t)
       {
         var result = new List<MemberInfo>(64);

            while(t != typeof(object))
            {
                var fields = t.GetFields(BindingFlags.NonPublic  |
                                           BindingFlags.Public   |
                                           BindingFlags.Instance |
                                           BindingFlags.DeclaredOnly);
                result.AddRange(fields);

                var props = t.GetProperties(BindingFlags.NonPublic  |
                                               BindingFlags.Public   |
                                               BindingFlags.Instance |
                                               BindingFlags.DeclaredOnly);
                result.AddRange(props);


                t = t.BaseType;
            }

         return result;
       }

       private static object getVal(IConfigNode node, Type type, string tname, string mname, bool verbatim)
       {
         try
         {
           return node.ValueAsType(type, verbatim);
         }
         catch(Exception error)
         {
           throw new ConfigException(string.Format(StringConsts.CONFIGURATION_ATTR_APPLY_VALUE_ERROR, mname, tname, error.Message));
         }
       }


       private void evalAttributeVars(Type type)
       {
         if (Path==null) return;

         Path = Path.Replace("@type@", type.FullName);
         Path = Path.Replace("@assembly@", type.Assembly.GetName().Name);

       }

    }

}
