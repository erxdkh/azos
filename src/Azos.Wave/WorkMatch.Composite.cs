/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Linq;

using Azos.Conf;
using Azos.Collections;
using Azos.Serialization.JSON;

namespace Azos.Wave
{
  /// <summary>
  /// Matches work context using AND and OR branches of composite matches
  /// </summary>
  public class CompositeWorkMatch : WorkMatch
  {
    public const string CONFIG_AND_SECTION = "and";
    public const string CONFIG_OR_SECTION = "or";

    public CompositeWorkMatch(string name, int order) : base(name, order) { }
    public CompositeWorkMatch(IConfigSectionNode confNode) : base(confNode)
    {
      if (confNode==null)
        throw new WaveException(StringConsts.ARGUMENT_ERROR + GetType().FullName+".ctor(confNode==null)");

      foreach(var cn in confNode[CONFIG_AND_SECTION].Children.Where(cn=>cn.IsSameName(WorkMatch.CONFIG_MATCH_SECTION)))
        if(!m_ANDMatches.Register( FactoryUtils.Make<WorkMatch>(cn, typeof(WorkMatch), args: new object[]{ cn })) )
          throw new WaveException(StringConsts.CONFIG_COMPOSITE_MATCH_DUPLICATE_MATCH_NAME_ERROR.Args("and: '{0}'".Args(cn.AttrByName(Configuration.CONFIG_NAME_ATTR).Value)));

      foreach(var cn in confNode[CONFIG_OR_SECTION].Children.Where(cn=>cn.IsSameName(WorkMatch.CONFIG_MATCH_SECTION)))
        if(!m_ORMatches.Register( FactoryUtils.Make<WorkMatch>(cn, typeof(WorkMatch), args: new object[]{ cn })) )
          throw new WaveException(StringConsts.CONFIG_COMPOSITE_MATCH_DUPLICATE_MATCH_NAME_ERROR.Args("or: '{0}'".Args(cn.AttrByName(Configuration.CONFIG_NAME_ATTR).Value)));
    }

    private OrderedRegistry<WorkMatch> m_ANDMatches = new OrderedRegistry<WorkMatch>();
    private OrderedRegistry<WorkMatch> m_ORMatches  = new OrderedRegistry<WorkMatch>();

    /// <summary>
    /// If true, captures result of individuals matches into return context of this match
    /// </summary>
    [Config]
    public bool CaptureMatches{ get; set;}


    /// <summary>
    /// AND matches - all must match
    /// </summary>
    public OrderedRegistry<WorkMatch> ANDMatches { get{ return m_ANDMatches;} }

    /// <summary>
    /// OR matches - any one can match
    /// </summary>
    public OrderedRegistry<WorkMatch> ORMatches { get{ return m_ORMatches;} }


    public override JsonDataMap Make(WorkContext work, object context = null)
    {
      var result = base.Make(work, context);
      if (result==null) return null;

      foreach(var match in m_ANDMatches.OrderedValues)
      {
        var capture = match.Make(work, context);
        if (capture == null) return null;

        if (CaptureMatches && match.CompositeCapture)
          result.Append(capture, true);
      }


      var any = false;
      JsonDataMap orCapture = null;
      var compositeCapture = false;
      foreach(var match in m_ORMatches.OrderedValues)
      {
        any = true;
        orCapture = match.Make(work, context);
        if (orCapture!=null)
        {
          compositeCapture = match.CompositeCapture;
          break;
        }
      }

      if (any && orCapture==null) return null;

      if (CaptureMatches && compositeCapture && orCapture!=null)
        result.Append(orCapture);

      return result;
    }
  }


}
