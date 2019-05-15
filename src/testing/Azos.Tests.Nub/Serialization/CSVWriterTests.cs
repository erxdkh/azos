/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.IO;
using System.Text;

using Azos.Apps;
using Azos.Data;
using Azos.Scripting;
using Azos.Serialization.CSV;

namespace Azos.Tests.Nub.Serialization
{
  [Runnable]
  public class CSVWriterTests : IRunnableHook
  {
   private readonly string m_Header =
"SimpleStr,IntValue,FloatValue,DateValue,Multiline,Nullable,Quotes,Apostr,Comma\r\n";

   private readonly string m_FilteredHeader =
"SimpleStr,IntValue\r\n";

   private readonly string m_Data =
"Doctor Aibolit,66,19.66,12/31/1966 19:08:59,\"Avva\r\nChichi\",,\"\"\"Barm\"\"alei\"\"\",Mc'Farlen,\"1,2,3\"\r\n";

   private readonly string m_FilteredData =
"Doctor Aibolit,66\r\n";

    private TeztRow m_Row;

    void IRunnableHook.Prologue(Runner runner, FID id)
    {
      m_Row = new TeztRow
              {
                SimpleStr = "Doctor Aibolit",
                IntValue = 66,
                FloatValue = 19.66f,
                DateValue = new DateTime(1966, 12, 31, 19, 8, 59),
                NonUIValue = "nothing",
                Multiline = "Avva\r\nChichi",
                Nullable = null,
                Quotes = "\"Barm\"alei\"",
                Apostr = "Mc'Farlen",
                Comma = "1,2,3"
              };
    }

    bool IRunnableHook.Epilogue(Runner runner, FID id, Exception error) => false;


    [Run]
    public void Row_Default()
    {
      var res = CSVWriter.Write(m_Row);
      var str = m_Header + m_Data;
      Aver.AreEqual(str, res);
    }

    [Run]
    public void Row_TabDelimeter()
    {
      var res = CSVWriter.Write(m_Row, new CSVWritingOptions {FieldDelimiter='\t'});
      var str =
"SimpleStr\tIntValue\tFloatValue\tDateValue\tMultiline\tNullable\tQuotes\tApostr\tComma\r\n" +
"Doctor Aibolit\t66\t19.66\t12/31/1966 19:08:59\t\"Avva\r\nChichi\"\t\t\"\"\"Barm\"\"alei\"\"\"\tMc'Farlen\t1,2,3\r\n";
      Aver.AreEqual(str, res);
    }

    [Run]
    public void Row_CustomNull()
    {
      var res = CSVWriter.Write(m_Row, new CSVWritingOptions {NullValue="\0"});
      var d =
"Doctor Aibolit,66,19.66,12/31/1966 19:08:59,\"Avva\r\nChichi\",\0,\"\"\"Barm\"\"alei\"\"\",Mc'Farlen,\"1,2,3\"\r\n";
      Aver.AreEqual(m_Header + d, res);
    }

    [Run]
    public void Row_AllFields()
    {
      var res = CSVWriter.Write(m_Row,  CSVWritingOptions.AllFields);
      var h =
"SimpleStr,IntValue,FloatValue,DateValue,NonUIValue,Multiline,Nullable,Quotes,Apostr,Comma\r\n";
      var d =
"Doctor Aibolit,66,19.66,12/31/1966 19:08:59,nothing,\"Avva\r\nChichi\",,\"\"\"Barm\"\"alei\"\"\",Mc'Farlen,\"1,2,3\"\r\n";
      Aver.AreEqual(h + d, res);
    }

    [Run]
    public void Row_NoHeader()
    {
      var res = CSVWriter.Write(m_Row, CSVWritingOptions.NoHeader);
      Aver.AreEqual(m_Data, res);
    }

    [Run]
    public void Rowset_Default()
    {
      var rowset = new Rowset(m_Row.Schema);

      rowset.Add(m_Row);
      rowset.Add(m_Row);

      var res = CSVWriter.Write(rowset);
      var str = m_Header + m_Data + m_Data;
      Aver.AreEqual(str, res);
    }

    [Run]
    public void Rowset_Filter()
    {
      var rowset = new Rowset(m_Row.Schema);

      rowset.Add(m_Row);
      rowset.Add(m_Row);

      FieldFilterFunc filter = (r, k, fd) => fd.Name.EqualsIgnoreCase("SimpleStr") ||
                                             fd.Name.EqualsIgnoreCase("IntValue");

      var res = CSVWriter.Write(rowset, filter: filter);
      var str = m_FilteredHeader + m_FilteredData + m_FilteredData;
      Aver.AreEqual(str, res);
    }

    [Run]
    public void Rowset_NoHeader()
    {
      var rowset = new Rowset(m_Row.Schema);

      rowset.Add(m_Row);
      rowset.Add(m_Row);

      var res = CSVWriter.Write(rowset, CSVWritingOptions.NoHeader);
      var str = m_Data + m_Data;
      Aver.AreEqual(str, res);
    }

    [Run]
    public void Row_ToBuffer()
    {
      var encoding = new UTF8Encoding(false);
      var buffer = CSVWriter.WriteToBuffer(m_Row, encoding: encoding);

      var test = encoding.GetBytes(m_Header + m_Data);

      Aver.IsTrue(IOUtils.MemBufferEquals(test, buffer));
    }

    [Run]
    public void Row_ToFile()
    {
      var name = "data.csv";

      CSVWriter.WriteToFile(m_Row, name);
      Aver.IsTrue(File.Exists(name));

      var str = m_Header + m_Data;
      string res = System.IO.File.ReadAllText(name);
      Aver.AreEqual(str, res);

      File.Delete(name);
    }

    [Run]
    public void NullRowOrRowset()
    {
      var res = CSVWriter.Write((Doc)null);
      Aver.IsTrue(res == string.Empty);

      res = CSVWriter.Write((Rowset)null);
      Aver.IsTrue(res == string.Empty);
    }

    [Run]
    public void Row_AllNonWritable()
    {
      var row = new NonWritable
                {
                  FieldA = "A",
                  FieldB = "B",
                  FieldC = "C"
                };

      var res = CSVWriter.Write(row);
      Aver.IsTrue(res == string.Empty);

      res = CSVWriter.Write(row, CSVWritingOptions.AllFields);
      var test = "FieldA,FieldB,FieldC\r\nA,B,C\r\n";
      Aver.AreEqual(test, res);
    }

    private class TeztRow : TypedDoc
    {
      [Field] public string   SimpleStr  { get; set; }
      [Field] public int      IntValue   { get; set; }
      [Field] public float    FloatValue { get; set; }
      [Field] public DateTime DateValue  { get; set; }
      [Field(nonUI:true)]
              public string   NonUIValue { get; set; }
      [Field] public string   Multiline  { get; set; }
      [Field] public string   Nullable   { get; set; }
      [Field] public string   Quotes     { get; set; }
      [Field] public string   Apostr     { get; set; }
      [Field] public string   Comma      { get; set; }
    }

    private class NonWritable : TypedDoc
    {
      [Field(nonUI:true)] public string FieldA { get; set; }
      [Field(storeFlag: StoreFlag.None)] public string FieldB { get; set; }
      [Field(storeFlag: StoreFlag.OnlyLoad)] public string FieldC { get; set; }
    }
  }
}
