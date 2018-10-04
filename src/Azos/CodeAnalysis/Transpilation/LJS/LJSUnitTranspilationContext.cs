﻿using System;
using System.Collections.Generic;
using System.Text;

using Azos.Conf;
using Azos.CodeAnalysis.Laconfig;

namespace Azos.CodeAnalysis.Transpilation.LJS
{
  /// <summary>
  /// Represents a scope of LJS unit transpilation (such as a file). The transpilation is done
  /// fragment-by-fragment, where every fragment is excised from the JS file
  /// </summary>
  public class LJSUnitTranspilationContext : CommonCodeProcessor, IAnalysisContext
  {
    public const string DEFAULT_ID_PREFIX = "æ";
    public const int DEFAULT_INDENT = 2;

    public const string WV_DOM_PREFIX = "$d";
    public const string WV_TYPE_PREFIX = "$t";


    public LJSUnitTranspilationContext(string unitName) : this(unitName, null, null, true)//be default throws errors
    {
    }

    public LJSUnitTranspilationContext(string unitName, IAnalysisContext context = null, MessageList messages = null, bool throwErrors = false)
     : base( context, messages, throwErrors)
    {
      m_UnitName = unitName.IsNullOrWhiteSpace() ? CoreConsts.UNKNOWN : unitName;
    }

    private string m_IdPrefix = DEFAULT_ID_PREFIX;
    private string m_DomPrefix = WV_DOM_PREFIX;
    private string m_TypePrefix = WV_TYPE_PREFIX;

    private int m_IndentWidth = DEFAULT_INDENT;
    private int m_IDSeed;
    private string m_UnitName;

    [Config("trans|transpiler")]
    public IConfigSectionNode TranspilerConfig{ get; set;}

    /// <summary>
    /// Defines prefix for ids generated by transpiler
    /// </summary>
    [Config]
    public string IdPrefix
    {
      get => m_IdPrefix;
      set => m_IdPrefix =  value.IsNullOrWhiteSpace() ? DEFAULT_ID_PREFIX : value;
    }

    /// <summary>
    /// Defines prefix for Waev.DOM module which provides shortcuts for createElement(), getElement byid etc...
    /// </summary>
    [Config]
    public string DomPrefix
    {
      get => m_DomPrefix;
      set => m_DomPrefix =  value.IsNullOrWhiteSpace() ? WV_DOM_PREFIX : value;
    }

    /// <summary>
    /// Defines prefix for Waev.TYPE module
    /// </summary>
    [Config]
    public string TypePrefix
    {
      get => m_TypePrefix;
      set => m_TypePrefix = value.IsNullOrWhiteSpace() ? WV_TYPE_PREFIX : value;
    }

    [Config(Default=DEFAULT_INDENT)]
    public int IndentWidth
    {
      get => m_IndentWidth;
      set => m_IndentWidth = value < 0 ? 0: value;
    }


    public override Language Language => LJSLanguage.Instance;

    public override string MessageCodeToString(int code) => ((LaconfigMsgCode)code).ToString();

    /// <summary>
    /// Returns the name of the current unit which is being transpiled
    /// </summary>
    public string UnitName => m_UnitName;


    /// <summary>
    /// Generates unique ID  per this instance
    /// </summary>
    public string GenerateID() => "{0}{1}".Args(m_IdPrefix, m_IDSeed++);

    /// <summary>
    /// Factory method that makes new configured instance of transpiler per supplied configuration
    /// </summary>
    public virtual LJSFragmentTranspiler MakeAndConfigureTranspiler(LJSParser parser)
    {
      var node = TranspilerConfig;

      if (node==null || !node.Exists)
        return new LJSFragmentTranspiler(this, parser, this.Messages, false);

      var result = FactoryUtils.MakeAndConfigure<LJSFragmentTranspiler>(node,
                                                                        typeof(LJSFragmentTranspiler),
                                                                        new object[]{this, parser, this.Messages, false});
      return result;
    }

    /// <summary>
    /// Helper facade that assembles processing pipeline and transpiles an LJS fragment into a string
    /// within this unit context
    /// </summary>
    public string TranspileFragmentToString(Source.ISourceText source)
    {
      //1 Assemble pipeline
      var lexer = new LaconfigLexer(this, source);
      var ctxFragment = new LJSData(this);
      var parser = new LJSParser(ctxFragment, lexer);
      //make and configure parser-fragment transpiler in the unit scope
      var transpiler = this.MakeAndConfigureTranspiler(parser);

      //2 parse into fragment context
      parser.Parse();

      //3 transpile into fragment context under whole unit scope
      transpiler.Transpile();

      var result = ctxFragment.ResultObject.TranspiledContent;
      return result;
    }
  }
}