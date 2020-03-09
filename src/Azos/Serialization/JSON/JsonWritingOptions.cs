/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

using Azos.Conf;

namespace Azos.Serialization.JSON
{

    /// <summary>
    /// Specifies the purpose of JSON serialization so the level of detail may be dynamically adjusted.
    /// Depending on this parameter IJSONWritable implementors may include additional details
    /// that are otherwise not needed
    /// </summary>
    public enum JsonSerializationPurpose
    {
       Unspecified = 0,

       /// <summary>
       /// UI Interface feeding - include only the data needed to show to the user
       /// </summary>
       UIFeed,

       /// <summary>
       /// Include as much data as possible for remote object reconstruction
       /// </summary>
       Marshalling
    }



    /// <summary>
    /// Specifies how JSON should be written as text. Use JSONWritingOptions.Compact or JSONWritingOptions.PrettyPrint
    ///  static properties for typical options
    /// </summary>
    public class JsonWritingOptions : IConfigurable
    {
        private static JsonWritingOptions s_Compact =   new JsonWritingOptions();

        private static JsonWritingOptions s_CompacRowsAsMap =  new JsonWritingOptions(){RowsAsMap = true};

        private static JsonWritingOptions s_CompactASCII = new JsonWritingOptions{ ASCIITarget = true };

        private static JsonWritingOptions s_PrettyPrint =  new JsonWritingOptions{
                                                                                   IndentWidth = 2,
                                                                                   ObjectLineBreak = true,
                                                                                   MemberLineBreak = true,
                                                                                   SpaceSymbols = true,
                                                                                   ASCIITarget = false
                                                                                 };

        private static JsonWritingOptions s_PrettyPrintASCII =  new JsonWritingOptions{
                                                                                   IndentWidth = 2,
                                                                                   ObjectLineBreak = true,
                                                                                   MemberLineBreak = true,
                                                                                   SpaceSymbols = true,
                                                                                   ASCIITarget = true
                                                                                 };

        private static JsonWritingOptions s_PrettyPrintRowsAsMap =  new JsonWritingOptions{
                                                                                   IndentWidth = 2,
                                                                                   ObjectLineBreak = true,
                                                                                   MemberLineBreak = true,
                                                                                   SpaceSymbols = true,
                                                                                   ASCIITarget = false,
                                                                                   RowsAsMap = true
                                                                                 };


        /// <summary>
        /// Writes JSON without line breaks between members and no indenting. Suitable for data transmission
        /// </summary>
        public static JsonWritingOptions Compact { get { return s_Compact;} }

        /// <summary>
        /// Writes JSON without line breaks between members and no indenting writing rows as maps(key:values) instead of arrays. Suitable for data transmission
        /// </summary>
        public static JsonWritingOptions CompactRowsAsMap { get { return s_CompacRowsAsMap;} }

        /// <summary>
        /// Writes JSON without line breaks between members and no indenting escaping any characters
        ///  with codes above 127 suitable for ASCII transmission
        /// </summary>
        public static JsonWritingOptions CompactASCII { get { return s_CompactASCII;} }

        /// <summary>
        /// Writes JSON suitable for printing/screen display
        /// </summary>
        public static JsonWritingOptions PrettyPrint { get { return s_PrettyPrint;} }

        /// <summary>
        /// Writes JSON suitable for printing/screen display
        ///  with codes above 127 suitable for ASCII transmission
        /// </summary>
        public static JsonWritingOptions PrettyPrintASCII { get { return s_PrettyPrintASCII;} }

        /// <summary>
        /// Writes JSON suitable for printing/screen display writing rows as maps(key:values) instead of arrays
        /// </summary>
        public static JsonWritingOptions PrettyPrintRowsAsMap { get { return s_PrettyPrintRowsAsMap;} }

        public JsonWritingOptions()
        {
        }

        public JsonWritingOptions(JsonWritingOptions other)
        {
          if (other==null) return;

          this.NLSMapLanguageISO        = other.NLSMapLanguageISO;
          this.NLSMapLanguageISODefault = other.NLSMapLanguageISODefault;
          this.IndentWidth              = other.IndentWidth;
          this.SpaceSymbols             = other.SpaceSymbols;
          this.ObjectLineBreak          = other.ObjectLineBreak;
          this.MemberLineBreak          = other.MemberLineBreak;
          this.ASCIITarget              = other.ASCIITarget;
          this.ISODates                 = other.ISODates;
          this.MaxNestingLevel          = other.MaxNestingLevel;
          this.RowsAsMap                = other.RowsAsMap;
          this.RowsetMetadata           = other.RowsetMetadata;
          this.Purpose                  = other.Purpose;
          this.MapSkipNulls             = other.MapSkipNulls;
          this.RowMapTargetName         = other.RowMapTargetName;
        }


        /// <summary>
        /// Specifies language ISO code (3 chars) that is used (when set) by the NLSMap class,
        /// so only entries for that particular language are included. When NLSMap contains entries for more than 1 language,
        /// but user needs only one entry received for his/her selected language, this option can be set, then NLSMap will only inline
        /// Name:Descr pair for that language. If a map does not contain an entry for the requested lang then NLSMapLanguageISODefault
        /// will be tried
        /// </summary>
        [Config]
        public string NLSMapLanguageISO;

        /// <summary>
        /// Specified language ISO default for NLSMap lookup, "eng" is used for default
        /// </summary>
        [Config]
        public string NLSMapLanguageISODefault = CoreConsts.ISO_LANG_ENGLISH;


        /// <summary>
        /// Specifies character width of single indent level
        /// </summary>
        [Config]
        public int IndentWidth;

        /// <summary>
        /// Indicates whether a space must be placed right after the symbol, such as coma in array declaration or colon in member declaration for
        ///  better readability
        /// </summary>
        [Config]
        public bool SpaceSymbols;

        /// <summary>
        /// Specifies whether objects need to be separated by line brakes for better readability
        /// </summary>
        [Config]
        public bool ObjectLineBreak;

        /// <summary>
        /// Specifies whether every object member must be placed on a separate line for better readability
        /// </summary>
        [Config]
        public bool MemberLineBreak;

        /// <summary>
        /// Specifies whether the target of serialization only deals with ASCII characters,
        /// so any non-ASCII character with code above 127 must be escaped with unicode escape sequence
        /// </summary>
        [Config]
        public bool ASCIITarget;

        /// <summary>
        /// Specifies whether DateTime must be encoded using ISO8601 format that look like "2011-03-18T14:25:00Z",
        /// otherwise dates are encoded using "new Date(milliseconds_since_unix_epoch)" which is technically not a valid JSON, however
        ///  most JSON parsers understand it very well
        /// </summary>
        [Config]
        public bool ISODates = true;

        /// <summary>
        /// Sets a limit of object nesting, i.e. for recursive graph depth. Default is 0xff
        /// </summary>
        [Config]
        public int MaxNestingLevel = 0xff;


        /// <summary>
        /// When true, writes every row as a map {FieldName: FieldValue,...} instead of array of values
        /// </summary>
        [Config]
        public bool RowsAsMap;

        /// <summary>
        /// When true, writes rowset metadata (i.e. schema, instance id etc.)
        /// </summary>
        [Config]
        public bool RowsetMetadata;

        /// <summary>
        /// Specifies the purpose of JSON serialization so the level of detail may be dynamically adjusted.
        /// Depending on this parameter IJSONWritable implementors may include additional details
        /// that are otherwise not needed
        /// </summary>
        [Config]
        public JsonSerializationPurpose Purpose;


        /// <summary>
        /// If true, then does not write map keys which are null
        /// </summary>
        [Config]
        public bool MapSkipNulls;


        /// <summary>
        /// When set, specifies the target name for Row's fields when they are written as map
        /// </summary>
        [Config]
        public string RowMapTargetName;


        public void Configure(IConfigSectionNode node)
        {
            ConfigAttribute.Apply(this, node);
        }
    }
}
