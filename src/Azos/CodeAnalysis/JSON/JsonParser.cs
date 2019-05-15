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
    /// Parses JSON lexer output into object graphs.
    /// NOTE: Although called JSON, this is really a JSON superset implementation that includes extra features:
    ///  comments, directives, verbatim strings(start with $), ' or " string escapes, unquoted object key names
    /// </summary>
    public sealed partial class JsonParser : Parser<JsonLexer>
    {

        public JsonParser(JsonLexer input,  MessageList messages = null, bool throwErrors = false, bool caseSensitiveMaps = true) :
            base(new JsonData(), new JsonLexer[]{ input }, messages, throwErrors)
        {
            m_Lexer = Input.First();
            m_CaseSensitiveMaps = caseSensitiveMaps;
        }


        public JsonParser(JsonData context, JsonLexer input,  MessageList messages = null, bool throwErrors = false, bool caseSensitiveMaps = true) :
            base(context, new JsonLexer[]{ input }, messages, throwErrors)
        {
            m_Lexer = Input.First();
            m_CaseSensitiveMaps = caseSensitiveMaps;
        }

        private JsonLexer m_Lexer;

        private bool m_CaseSensitiveMaps;

        public JsonLexer Lexer { get { return m_Lexer;} }

        public JsonData ResultContext { get{ return Context as JsonData;} }

        public override Language Language
        {
            get { return JsonLanguage.Instance; }
        }

        public override string MessageCodeToString(int code)
        {
            return ((JsonMsgCode)code).ToString();;
        }




        protected override void DoParse()
        {
            try
            {
                tokens = Lexer.GetEnumerator();
                fetchPrimary();
                var root = doAny();
                ResultContext.setData( root );
            }
            catch(abortException)
            {

            }
        }
    }
}
