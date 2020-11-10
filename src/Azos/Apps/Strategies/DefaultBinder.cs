﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Azos.Conf;

namespace Azos.Apps.Strategies
{
  /// <summary>
  /// Provides most basic IStrategyBinder implementation, resolving
  /// strategies from all assemblies by IStrategy
  /// </summary>
  public class DefaultBinder : ModuleBase, IStrategyBinder
  {
    public DefaultBinder(IApplication app) : base(app) {}
    public DefaultBinder(IModule parent) : base(parent) {}

    public override bool IsHardcodedModule => false;

    public override string ComponentLogTopic => CoreConsts.APPLICATION_TOPIC;

    [Config] private string m_Assemblies;

    private Dictionary<Type, BindingHandler> m_Cache;

    private IConfigSectionNode m_BindingHandlerConfig;

    protected virtual BindingHandler MakeBindingHandler(Type tTarget)
     => FactoryUtils.Make<BindingHandler>(m_BindingHandlerConfig, typeof(BindingHandler), new object[]{ tTarget, m_BindingHandlerConfig});

    protected override void DoConfigure(IConfigSectionNode node)
    {
      base.DoConfigure(node);

      if (node!=null)
      {
        m_BindingHandlerConfig = node["binding-handler","binding","handler"];
        if (m_BindingHandlerConfig.Exists)
        {
          var cfg = new MemoryConfiguration();
          cfg.CreateFromNode(m_BindingHandlerConfig);
          m_BindingHandlerConfig = cfg.Root;
          return;
        }
      }

      m_BindingHandlerConfig = Configuration.NewEmptyRoot();
    }

    protected override bool DoApplicationAfterInit()
    {
      m_Assemblies.NonBlank("Assemblies not configured");
      var anames = m_Assemblies.Split(';');

      m_Cache = new Dictionary<Type, BindingHandler>();

      var asms = anames.Where(n => n.IsNotNullOrWhiteSpace())
                       .Select(n => Assembly.LoadFrom(n.Trim()));

      foreach (var asm in asms)
      {
        var types = asm.GetTypes()
                       .Where(t => t.IsClass && !t.IsAbstract && typeof(IStrategy).IsAssignableFrom(t));

        foreach (var type in types)
        {
          var intfs = type.GetInterfaces().Where(ti => typeof(IStrategy).IsAssignableFrom(ti));
          foreach (var intf in intfs)
          {
            if (m_Cache.TryGetValue(intf, out var handler))
            {
              handler.Register(type);
            }
            else
            {
              handler = MakeBindingHandler(intf);
              handler.Register(type);
              m_Cache[intf] = handler;//keyed on IStrategy-derived interface type
            }
          }
        }
      }

      if (m_Cache.Count == 0)
        throw new StrategyException(StringConsts.STRAT_BINDING_NOTHING_REGISTERED_ERROR.Args(nameof(DefaultBinder)));

      foreach(var kvp in m_Cache) kvp.Value.FinalizeRegistration();

      return base.DoApplicationAfterInit();
    }



    #region IStrategyBinderLogic Implementation

    public TStrategy Bind<TStrategy, TContext>(TContext context) where TStrategy : class, IStrategy<TContext>
                                                                 where TContext : IStrategyContext
    {
      if (!m_Cache.TryGetValue(typeof(TStrategy), out var handler))
        throw new StrategyException(StringConsts.STRAT_BINDING_UNRESOLVED_ERROR.Args(typeof(TStrategy).DisplayNameWithExpandedGenericArgs()));

      var result = handler.Bind<TStrategy, TContext>(context);

      if (result==null)
        throw new StrategyException(StringConsts.STRAT_BINDING_MATCH_ERROR.Args(typeof(TStrategy).DisplayNameWithExpandedGenericArgs(), typeof(TContext).DisplayNameWithExpandedGenericArgs()));

      App.InjectInto(result);

      return result;
    }

    #endregion
  }
}
