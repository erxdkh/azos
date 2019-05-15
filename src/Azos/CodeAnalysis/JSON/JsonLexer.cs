/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Azos.CodeAnalysis.Source;

namespace Azos.CodeAnalysis.JSON
{
    /// <summary>
    /// Performs lexical analysis on source supplied in JSON syntax.
    /// This class supports lazy analysis that happens gradually as result tokens are consumed through IEnumerable interface.
    /// NOTE: Although called JSON, this is really a JSON superset implementation that includes extra features:
    ///  comments, directives, verbatim strings(start with $), ' or " string escapes, unquoted object key names
    /// </summary>
    public sealed class JsonLexer : Lexer<JsonToken>
    {
        public JsonLexer(ISourceText source, MessageList messages = null, bool throwErrors = false) :
            base( source, messages, throwErrors)
        {
             m_FSM = new FSM(this);
        }

        public JsonLexer(IAnalysisContext context, SourceCodeRef srcRef, ISourceText source, MessageList messages = null, bool throwErrors = false) :
            base(context, srcRef, source, messages, throwErrors)
        {
             m_FSM = new FSM(this);
        }

        public override Language Language
        {
            get { return JsonLanguage.Instance;}
        }

        public override string MessageCodeToString(int code)
        {
            return ((JsonMsgCode)code).ToString();
        }



        private FSM m_FSM;
        private IEnumerator<bool> m_Work;

        protected override bool DoLexingChunk()
        {
            if (m_AllAnalyzed) return true;

            if (m_Work==null)
            {
              m_Work = m_FSM.Run().GetEnumerator();
            }

            return !m_Work.MoveNext();
        }






    private class FSM
    {
      public FSM(JsonLexer lex)
      {
        lexer = lex;
        source = lexer.Source;
        tokens = lexer.m_Tokens;
        srcRef = lexer.SourceCodeReference;
      }

      private readonly JsonLexer lexer;
      private readonly ISourceText source;
      private readonly TokenList<JsonToken> tokens;
      private readonly SourceCodeRef srcRef;

      char chr, nchr;
      bool isCommentBlock;
      char commentBlockEnding;
      bool isCommentLine;
      bool isDirective;
      bool freshLine = true;

      bool isString;
      char stringEnding;
      bool isVerbatim;

      int posLine = 1;
      int posCol = 0;
      int posChar = 0;

      SourcePosition tagStartPos, tagEndPos;

      StringBuilder buffer = new StringBuilder();



      private void moveNext()
      {
        posChar++;
        posCol++;
        chr = source.ReadChar();
        nchr = source.PeekChar();
      }

      private SourcePosition srcPos()
      {
        return new SourcePosition(posLine, posCol, posChar);
      }


      //this is done on purpose do NOT use Char.isSymbol in .NET
      //we can control what WE consider symbols
      private bool isSymbol(char c)
      {
        return (
               ((c >= '!') && (c <= '/')) ||
               ((c >= ':') && (c <= '@')) ||
               ((c >= '[') && (c <= '`')) ||
               ((c >= '{') && (c <= '~'))
               ) && (c != '_') && (c != '.') && (c != '$') && (c != '#');
      }

      private void bufferAdd(char c)
      {
        if (buffer.Length == 0) tagStartPos = srcPos();
        buffer.Append(c);
        tagEndPos = srcPos();
      }



      public IEnumerable<bool> Run()
      {
        const int YIELD_BATCH = 5;
        var prevTokenCount = 0;


        tokens.Add(new JsonToken(
                                lexer,
                                JsonTokenType.tBOF,
                                srcPos(),
                                srcPos(),
                                String.Empty));

        #region Main walk
        //=======================================================================================================================
        while (!source.EOF)
        {
          moveNext();

          #region CRLF
          if ((chr == '\n') || (chr == '\r'))
          {
            if ((isString) && (!isVerbatim))
            {
              lexer.EmitMessage(MessageType.Error, (int)JsonMsgCode.eUnterminatedString, srcPos());
              yield break;//no further parsing
            }


            if ((isString && isVerbatim) || (isCommentBlock))
              bufferAdd(chr);


            if (chr == '\n')
            {
              if ((!isString) && (!isCommentBlock))
              {
                flush();
                if (isCommentLine)
                {
                  isCommentLine = false;
                  isDirective = false;
                }
                freshLine = true;
              }
              posLine++;
            }
            posCol = 0;


            continue;
          }

          #endregion


          if (isString)
          {
            #region Inside String

            if (isVerbatim || (chr != '\\') || (nchr != '\\'))//take care of 'c:\\dir\\';
            {
                  //turn off strings
                  if (
                       ((isVerbatim) && (chr == stringEnding) && (nchr == stringEnding)) ||
                       ((!isVerbatim) && (chr == '\\') && (nchr == stringEnding))
                      )
                  {
                    //Verbatim: eat one extra:   $"string ""test"" syntax" == string "test" syntax
                    //Regular: eat "\" escape:    "string \"test\" syntax" == string "test" syntax
                    moveNext();

                    if (source.EOF)
                    {
                      lexer.EmitMessage(MessageType.Error, (int)JsonMsgCode.eUnterminatedString, srcPos());
                      yield break;//stop further processing, as string did not terminate but EOF reached
                    }
                  }
                  else if (chr == stringEnding)
                  {
                    flush();
                    isString = false;
                    continue; // eat terminating string char
                  }

              }
              else//take care of 'c:\\dir\\'
              {
                bufferAdd(chr); //preserve  \
                moveNext();
              }
            #endregion
          }//in string
          else
          {
            #region Not Inside String

            if (!isCommentLine)
            {
              if (!isCommentBlock)
              {
                #region Not inside comments

                #region Turn On Comments
                //turn on comment block
                if (((chr == '/') || (chr == '|')) && (nchr == '*'))
                {
                  flush();
                  isCommentBlock = true;
                  commentBlockEnding = chr;
                  moveNext();
                  continue;
                }

                //turn on comment line
                if ((chr == '/') && (nchr == '/'))
                {
                  flush();
                  isCommentLine = true;
                  moveNext();
                  continue;
                }

                //turn on comment line mode for directive
                //directives MUST be the first non-white char on the line
                if (freshLine && chr == '#')
                {
                  flush();
                  isCommentLine = true;
                  isDirective = true;
                  continue;
                }


                #endregion

                #region Turn On Strings
                if ((chr == '$') && ((nchr == '"') || (nchr == '\'')))
                {
                  flush();
                  isString = true;
                  isVerbatim = true;
                  stringEnding = nchr;
                  moveNext();
                  continue;
                }
                if ((chr == '"') || (chr == '\''))
                {
                  flush();
                  isString = true;
                  isVerbatim = false;
                  stringEnding = chr;
                  continue;
                }

                #endregion

                #region Syntactic Separators - Space, colons and Symbols

                if ((chr == ' ') || (chr == '\t')) //space or TAB
                {
                  flush();
                  continue; //eat it
                }

                if (
                    (chr == ';') ||
                    (chr == '{') ||
                    (chr == '}') ||
                    (chr == '(') ||
                    (chr == ')') ||
                    (chr == '[') ||
                    (chr == ']') ||
                    (chr == ',') ||
                    (chr == ':') ||
                    ((chr == '.') && (!JsonIdentifiers.ValidateDigit(nchr)))
                   )
                 {
                   flush();
                   bufferAdd(chr);
                   flush();
                   continue;
                 }

                //Scientific numbers like:   2e+30, 45E-10
                if (buffer.Length > 0 &&
                     JsonIdentifiers.ValidateDigit(buffer[0]) &&
                     (chr == 'e' || chr == 'E') &&
                     (nchr == '+' || nchr == '-')
                    )
                {
                  bufferAdd(chr); //e
                  moveNext();
                  bufferAdd(chr); //+ or -
                  moveNext();
                  bufferAdd(chr); // add digit after + or -
                  continue;
                }



                //for operators like -- /= += etc...
                if ( (buffer.Length > 0) && (isSymbol(chr) != isSymbol(buffer[0])) )
                {
                  flush();
                }

                #endregion

                #endregion
              }
              else
              {
                #region Turn Off Comment Block
                if ((chr == '*') && (nchr == commentBlockEnding))
                {
                  flush();
                  isCommentBlock = false;
                  moveNext();
                  continue;
                }
                #endregion
              }//block comments off

            }//NOT CommentLine

            #endregion
          }//not in string

          bufferAdd(chr);
          freshLine = false;

          //yield the batch of new tokens
          if (tokens.Count>prevTokenCount+YIELD_BATCH)
          {
                prevTokenCount = tokens.Count;
                yield return true;
          }

        }//while
        //=======================================================================================================================
        #endregion


        flush(); //flush any remains

        #region Post-walk check
            if (tokens.Count < 2)
               lexer.EmitMessage(MessageType.Error, (int)JsonMsgCode.ePrematureEOF, srcPos());


            if (isCommentBlock)
               lexer.EmitMessage(MessageType.Error, (int)JsonMsgCode.eUnterminatedComment, srcPos());


            if (isString)
               lexer.EmitMessage(MessageType.Error, (int)JsonMsgCode.eUnterminatedString, srcPos());


        #endregion

            tokens.Add(new JsonToken(lexer,
                                   JsonTokenType.tEOF,
                                   new SourcePosition(posLine, posCol, posChar),
                                   new SourcePosition(posLine, posCol, posChar),
                                   String.Empty));
        yield return true;
        yield break;
      }//Run



      private void flush()
      {
        if (
            (!isString) &&
            (!isCommentBlock) &&
            (!isCommentLine) &&
            (buffer.Length == 0)
           ) return;

        string text = buffer.ToString();
        object value = null;

        buffer.Length = 0;

        var type = JsonTokenType.tUnknown;

        if (isString)
        {
          type = JsonTokenType.tStringLiteral;


          if (!isVerbatim)
          {
            try //expand escapes
            {
              text = JsonStrings.UnescapeString(text);
            }
            catch (StringEscapeErrorException err)
            {
              lexer.EmitMessage(MessageType.Error, (int)JsonMsgCode.eInvalidStringEscape, tagStartPos, null, err.ErroredEscape);
              return;
            }
          }
        }
        else if (isCommentLine && isDirective)//directives treated similar to line comments
        {
          type = JsonTokenType.tDirective;
        }
        else if (isCommentBlock || isCommentLine)
        {
          type = JsonTokenType.tComment;
        }
        else
        {
          try
          {
            value = JsonNumbers.Convert(text, out type);
          }
          catch (ArgumentException err)
          {
            lexer.EmitMessage(MessageType.Error, (int)JsonMsgCode.eValueTooBig, tagStartPos, null, err.Message);
            return;
          }

          if (value == null)  //not number
          {

            type = JsonKeywords.Resolve(text);

            if (type == JsonTokenType.tIdentifier)
            {
              if (text.StartsWith("$"))
              {
                 text = text.Remove(0, 1); //take care of verbatim names like: $class, $method, $var etc..
                 tagStartPos = new SourcePosition(tagStartPos.LineNumber, tagStartPos.ColNumber+1, tagStartPos.CharNumber+1);
              }

              if (!JsonIdentifiers.Validate(text))
              {
                lexer.EmitMessage(MessageType.Error, (int)JsonMsgCode.eInvalidIdentifier, tagStartPos, null, text);
                return;
              }
            }
          }//not number
        }//not comment


        if (type==JsonTokenType.tStringLiteral) value = text;

        tokens.Add(new JsonToken(lexer, type, tagStartPos, tagEndPos, text, value));
      }


    }//FSM










    }//JSON Lexer
}
