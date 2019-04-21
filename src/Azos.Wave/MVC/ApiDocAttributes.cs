﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Azos.Conf;

namespace Azos.Wave.Mvc
{
  /// <summary>
  /// Base class for ApiDoc* attributes that provide additional metadata for API documentation
  /// </summary>
  public abstract class ApiDocAttribute : Attribute
  {
    /// <summary>
    /// Provides a short (typically under 128 chars) plain-text title stating the purpose of the decorated controller or endpoint
    /// </summary>
    public string Title {  get; set; }

    /// <summary>
    /// Specifies the list of additional DataDoc types schemas to include as a part of documentation. The system includes all DataDoc-derived
    /// parameters automatically so extra types may be included here such as the ones used in polymorphic results
    /// </summary>
    public Type[] DataSchemas { get; set; }

    /// <summary>
    /// Specifies the list of additional Permission-derived types to include as a part of documentation. The system includes all Permission-derived
    /// attributes automatically
    /// </summary>
    public Type[] Permissions { get; set; }


    public virtual void Describe(ApiDocGenerator generator, ConfigSectionNode data, Type controllerType)
    {
      data.AddAttributeNode("title", Title);
    }
  }

  /// <summary>
  /// Provides documentation-related metadata for API controllers
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
  public class ApiControllerDocAttribute : ApiDocAttribute
  {
    private string m_BaseUri;

    /// <summary>
    /// Base Uri for this controller, it must start with '/'
    /// </summary>
    public string BaseUri { get => m_BaseUri.IsNullOrWhiteSpace() ? "/" : m_BaseUri; set => m_BaseUri = value; }

    /// <summary>
    /// Specifies the name of the markdown resource file containing documentation for this controller, denotes as (#Title/H1),
    /// if left blank then the system tries to find markdown resource embedded at the same level with controller class.
    /// The endpoint/method level doc is searched if DocAnchor property is set
    /// </summary>
    public string DocFile { get; set; }

    /// <summary>
    /// Provides a short line (expected to be under 128) describing auth required
    /// </summary>
    public string Authentication { get; set; }

    public override void Describe(ApiDocGenerator generator, ConfigSectionNode data, Type controllerType)
    {
      base.Describe(generator, data, controllerType);
      data.AddAttributeNode("uri", BaseUri);
      data.AddAttributeNode("doc-file", DocFile);
    }

  }

  /// <summary>
  /// Provides documentation-related metadata for API endpoints such as action methods
  /// </summary>
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
  public class ApiEndpointDocAttribute : ApiDocAttribute
  {
    /// <summary>
    /// Uri for this endpoint, if the URI starts with '/' then it is an absolute URI and not appended to controller base URI,
    /// otherwise method URIs get appended to controller URIs. If this is not set, on the method level, URI is inferred from Action attribute
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// Specifies the anchor/id used as a topic in the doc markdown file. Endpoint anchors start with "##" (html H2 level) like "## list".
    /// The system reads content starting from that anchor up to the beginning of the next adjacent anchor of the same level.
    /// If this property is not set, the system takes the name from action attribute and prepends "## " at the front
    /// </summary>
    public string DocAnchor { get; set; }
  }


}
