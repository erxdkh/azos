﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Azos.Serialization.JSON;

namespace Azos.Data.AST
{
  /// <summary>
  /// Provides general ancestor for abstract syntax tree nodes
  /// </summary>
  [ExpressionJsonHandlerAttribute]
  public abstract class Expression : AmorphousTypedDoc
  {
    public abstract void Accept(XlatContext ctx);

  }

  /// <summary>
  /// Represents a value, such as a constant literal
  /// </summary>
  public class ValueExpression : Expression
  {
    [Field]
    public object Value { get; set; }//may contain json array or json data map as-is

    public override void Accept(XlatContext ctx)
     => ctx.Visit(this);
  }

  /// <summary>
  /// Represents an identifier such as column/field name
  /// </summary>
  public class IdentifierExpression : Expression
  {
    [Field]
    public object Identifier { get; set; }//may contain json array or json data map as-is

    public override void Accept(XlatContext ctx)
     => ctx.Visit(this);
  }

  /// <summary>
  /// Provides abstraction for operators
  /// </summary>
  public abstract class OperatorExpression : Expression
  {
    [Field]
    public string Operator { get; set; }
  }

  /// <summary>
  /// Represents an operator that has a single operand, e.g. a negation or "not" operator
  /// </summary>
  public class UnaryExpression : OperatorExpression
  {
    [Field]
    public Expression Operand { get; set; }

    public override void Accept(XlatContext ctx) => ctx.Visit(this);
  }

  /// <summary>
  /// Represents an operator that has two operands: left and right
  /// </summary>
  public class BinaryExpression : OperatorExpression
  {
    [Field] public Expression LeftOperand {  get; set; }
    [Field] public Expression RightOperand { get; set; }

    public override void Accept(XlatContext ctx) => ctx.Visit(this);
  }


}
