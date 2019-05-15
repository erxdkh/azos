﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Azos.Conf;

namespace Azos.Data
{
  /// <summary>
  /// Provides custom metadata for DataDocuments
  /// </summary>
  public sealed class DocCustomMetadataProvider : CustomMetadataProvider
  {
    public override ConfigSectionNode ProvideMetadata(MemberInfo member, object instance, IMetadataGenerator context, ConfigSectionNode dataRoot, NodeOverrideRules overrideRules = null)
    {
      var tdoc = member.NonNull(nameof(member)) as Type;
      if (tdoc == null || !typeof(Doc).IsAssignableFrom(tdoc)) return null;

      var typed = tdoc.IsSubclassOf(typeof(TypedDoc));

      var ndoc = dataRoot.AddChildNode("data-doc");
      Schema schema;
      if (instance is Doc doc) schema = doc.Schema;
      else if (typed) schema = Schema.GetForTypedDoc(tdoc);
      else schema = null;

      MetadataUtils.AddMetadataTokenIdAttribute(ndoc, tdoc);
      ndoc.AddAttributeNode("kind", typed ? "typed" : "dynamic");

      CustomMetadataAttribute.Apply(typeof(Schema), schema, context, ndoc, overrideRules);

      return ndoc;
    }
  }
}
