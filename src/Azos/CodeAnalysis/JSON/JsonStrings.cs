/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Text;

using Azos.Text;

namespace Azos.CodeAnalysis.JSON
{

  /// <summary>
  /// Provides JSON string escape parsing
  /// </summary>
  public static class JsonStrings
  {

    public static string UnescapeString(string str)
    {
      //quick return strings that are not escaped
      if (str.IndexOf('\\') == -1) return str;

      StringBuilder sb = new StringBuilder(str.Length);

      for (int i = 0; i < str.Length; i++)
      {
        char c = str[i];
        if ((i < str.Length - 1) && (c == '\\'))
        {
          i++;
          char n = str[i];

          switch (n)
          {
            case '\\': sb.Append('\\'); break;
            case '/': sb.Append('/'); break;
            case '0': sb.Append((char)CharCodes.Char0); break;
            case 'a': sb.Append((char)CharCodes.AlertBell); break;
            case 'b': sb.Append((char)CharCodes.Backspace); break;
            case 'f': sb.Append((char)CharCodes.Formfeed); break;
            case 'n': sb.Append((char)CharCodes.LF); break;
            case 'r': sb.Append((char)CharCodes.CR); break;
            case 't': sb.Append((char)CharCodes.Tab); break;
            case 'v': sb.Append((char)CharCodes.VerticalQuote); break;
            case 'u': //  \uFFFF
              string hex = string.Empty;
              int cnt = 0;
              //loop through UNICODE hex number chars
              while ((i < str.Length - 1) && (cnt < 4))
              {
                i++;
                hex += str[i];
                cnt++;
              }

              try
              {
                sb.Append(Char.ConvertFromUtf32(Convert.ToInt32(hex, 16)));
              }
              catch
              {
                throw new StringEscapeErrorException(hex);
              }

              break;

            default:
              throw new StringEscapeErrorException(String.Format("{0}{1}", c, n));
          }

        }
        else
          sb.Append(c);

      }//for

      return sb.ToString();
    }

  }



}