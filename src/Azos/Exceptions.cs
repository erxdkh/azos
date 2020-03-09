/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Runtime.Serialization;

using Azos.Data;
using Azos.Apps;
using Azos.Serialization.BSON;
using Azos.Serialization.Arow;

namespace Azos
{
  /// <summary>
  /// Base exception thrown by the framework
  /// </summary>
  [Serializable]
  public class AzosException : Exception
  {
    public const string CODE_FLD_NAME = "AE-C";

    public AzosException() {}
    public AzosException(string message) : base(message) {}
    public AzosException(string message, Exception inner) : base(message, inner) {}
    protected AzosException(SerializationInfo info, StreamingContext context) : base(info, context) { Code = info.GetInt32(CODE_FLD_NAME); }

    /// <summary>
    /// Provides general-purpose error code
    /// </summary>
    public int Code { get; set; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException("info", GetType().Name + ".GetObjectData(info=null)");
      info.AddValue(CODE_FLD_NAME, Code);
      base.GetObjectData(info, context);
    }
  }


  /// <summary>
  /// Thrown by Debug class to indicate assertion failures
  /// </summary>
  [Serializable]
  public sealed class DebugAssertionException : AzosException
  {
    public const string FROM_FLD_NAME = "DAE-F";

    public DebugAssertionException(string from = null) { m_From = from; }
    public DebugAssertionException(string message, string from = null) : base(message) { m_From = from; }
    private DebugAssertionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      m_From = info.GetString(FROM_FLD_NAME);
    }

    private string m_From;

    public string From { get { return m_From ?? string.Empty; } }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new AzosException(StringConsts.ARGUMENT_ERROR + GetType().Name + ".GetObjectData(info=null)");
      info.AddValue(FROM_FLD_NAME, m_From);
      base.GetObjectData(info, context);
    }
  }


  /// <summary>
  /// Thrown by Aver class to indicate averment failures
  /// </summary>
  [Serializable]
  public sealed class AvermentException : AzosException
  {
    public const string FROM_FLD_NAME = "AE-F";

    public AvermentException(string message = null) : this(message, null, null) {}

    public AvermentException(string message, string from = null) : this(message, from, null) {}

    public AvermentException(string message, string from, Exception inner) : base((from.IsNullOrWhiteSpace() ? "" : "from '{0}' ".Args(from)) + message, inner)
    {
      m_From = from;
    }

    private AvermentException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      m_From = info.GetString(FROM_FLD_NAME);
    }

    private string m_From;

    public string From { get { return m_From ?? string.Empty; } }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new AzosException(StringConsts.ARGUMENT_ERROR + GetType().Name + ".GetObjectData(info=null)");
      info.AddValue(FROM_FLD_NAME, m_From);
      base.GetObjectData(info, context);
    }
  }

  /// <summary>
  /// Provides textual portable data about this exception which will be used in wrapped exception.
  /// Wrapped exceptions are used to marshal non serializable exceptions
  /// </summary>
  public interface IWrappedExceptionDataSource
  {
    /// <summary>
    /// Gets portable textual representation of exception data for inclusion in wrapped exception
    /// </summary>
    string GetWrappedData();
  }

  /// <summary>
  /// Marshals exception details
  /// </summary>
  [Serializable]
  [Arow("CBE82107-8FDA-4A2C-84D2-5D953907C9A0")]
  [BSONSerializable("A339F46F-6637-4396-B148-094BAFFB4BE6")]
  public sealed class WrappedExceptionData : TypedDoc, IBSONSerializable, IBSONDeserializable
  {

    /// <summary>
    /// Creates an instance of WrappedExceptionData saturating it from base64-encoded BSON data.
    /// This method complements .ToBase64()
    /// </summary>
    public static WrappedExceptionData FromBase64(string base64)
    {
      var bin = Convert.FromBase64String(base64);
      var doc = BSONDocument.FromArray(bin);
      var ser = new BSONSerializer();
      var result = new WrappedExceptionData();
      object ctx = null;
      result.DeserializeFromBSON(ser, doc, ref ctx);
      return result;
    }

    internal WrappedExceptionData(){}

    /// <summary>
    /// Initializes instance form local exception
    /// </summary>
    public WrappedExceptionData(Exception error, bool captureStack = true)
    {
      if (error==null) throw new AzosException(StringConsts.ARGUMENT_ERROR+"WrappedExceptionData.ctor(error=null)");

      var tp = error.GetType();
      m_TypeName = tp.FullName;
      m_Message = error.Message;
      if (error is AzosException)
        m_Code = ((AzosException)error).Code;

      m_ApplicationName = ExecutionContext.Application.Name;

      m_Source = error.Source;
      if (captureStack)
        m_StackTrace = error.StackTrace;

      if (error.InnerException != null)
        m_InnerException = new WrappedExceptionData(error.InnerException);

      var source = error as IWrappedExceptionDataSource;
      if (source != null)
        m_WrappedData = source.GetWrappedData();
    }

    private string m_TypeName;
    private string m_Message;
    private int m_Code;
    private string m_ApplicationName;
    private string m_Source;
    private string m_StackTrace;
    private string m_WrappedData;
    private WrappedExceptionData m_InnerException;

    /// <summary>
    /// Returns the name of remote exception type
    /// </summary>
    [Field, Field(isArow: true, backendName: "tpname")]
    public string TypeName
    {
      get => m_TypeName ?? CoreConsts.UNKNOWN;
      set => m_TypeName = value;
    }

    /// <summary>
    /// Returns the message of remote exception
    /// </summary>
    [Field, Field(isArow: true, backendName: "msg")]
    public string Message
    {
      get => m_Message ?? string.Empty;
      set => m_Message = value;
    }

    /// <summary>
    /// Returns the code of remote Azos exception
    /// </summary>
    [Field, Field(isArow: true, backendName: "code")]
    public int Code
    {
      get => m_Code;
      set => m_Code = value;
    }

    /// <summary>
    /// Name of the object that caused the error
    /// </summary>
    [Field, Field(isArow: true, backendName: "src")]
    public string Source
    {
      get => m_Source ?? string.Empty;
      set => m_Source = value;
    }

    /// <summary>
    /// Returns stack trace
    /// </summary>
    [Field, Field(isArow: true, backendName: "strace")]
    public string StackTrace
    {
      get => m_StackTrace ?? string.Empty;
      set => m_StackTrace = value;
    }

    /// <summary>
    /// Returns the name of remote application
    /// </summary>
    [Field, Field(isArow: true, backendName: "appname")]
    public string ApplicationName
    {
      get => m_ApplicationName ?? CoreConsts.UNKNOWN;
      set => m_ApplicationName = value;
    }

    /// <summary>
    /// Returns wrapped date from IWrappedDataSource
    /// </summary>
    [Field, Field(isArow: true, backendName: "wdata")]
    public string WrappedData
    {
      get => m_WrappedData;
      set => m_WrappedData = value;
    }

    /// <summary>
    /// Returns the inner remote exception if any
    /// </summary>
    [Field, Field(isArow: true, backendName: "inner")]
    public WrappedExceptionData InnerException
    {
      get => m_InnerException;
      set => m_InnerException = value;
    }

    public override string ToString()
    {
      return string.Format("[{0}:{1}:{2}] {3}", TypeName, Code, ApplicationName, Message);
    }

    public void SerializeToBSON(BSONSerializer serializer, BSONDocument doc, IBSONSerializable parent, ref object context)
    {
      serializer.AddTypeIDField(doc, parent, this, context);

      doc.Set( new BSONStringElement("tname", TypeName))
         .Set( new BSONStringElement("msg",   Message))
         .Set( new BSONInt32Element ("code",  Code))
         .Set( new BSONStringElement("app",   ApplicationName))
         .Set( new BSONStringElement("src",   Source))
         .Set( new BSONStringElement("trace", StackTrace));

      if (WrappedData!=null)
        doc.Set( new BSONStringElement("wdata", WrappedData));

      if (m_InnerException==null) return;

      doc.Set( new BSONDocumentElement("inner", serializer.Serialize(m_InnerException, parent: this)) );
    }

    public bool IsKnownTypeForBSONDeserialization(Type type)
    {
      return type==typeof(WrappedExceptionData);
    }

    public void DeserializeFromBSON(BSONSerializer serializer, BSONDocument doc, ref object context)
    {
      m_TypeName        = doc.TryGetObjectValueOf("tname").AsString();
      m_Message         = doc.TryGetObjectValueOf("msg").AsString();
      m_Code            = doc.TryGetObjectValueOf("code").AsInt();
      m_ApplicationName = doc.TryGetObjectValueOf("app").AsString();
      m_Source          = doc.TryGetObjectValueOf("src").AsString();
      m_StackTrace      = doc.TryGetObjectValueOf("trace").AsString();
      m_WrappedData     = doc.TryGetObjectValueOf("wdata").AsString();

      var iv = doc["inner"] as BSONDocumentElement;
      if (iv==null) return;

      m_InnerException = new WrappedExceptionData();
      serializer.Deserialize(iv.Value, m_InnerException);
    }

    /// <summary>
    /// Serializes the instance as base64-encoded BSON data. This method complements .FromBase64(string)
    /// </summary>
    public string ToBase64()
    {
      var ser = new BSONSerializer();
      var doc = ser.Serialize(this);
      var bin = doc.WriteAsBSONToNewArray();
      return Convert.ToBase64String(bin, Base64FormattingOptions.None);
    }
  }

  /// <summary>
  /// Represents exception that contains data about causing exception with all of it's chain
  /// </summary>
  [Serializable]
  [BSONSerializable("A43ABD0D-22B2-4012-8A24-280A038FD943")]
  public sealed class WrappedException : AzosException, IBSONSerializable, IBSONDeserializable
  {
    public const string WRAPPED_FLD_NAME = "WE-W";

    /// <summary>
    /// Returns an exception wrapped into WrappedException. If the exception is already wrapped, it is returned as-is
    /// </summary>
    public static WrappedException ForException(Exception root, bool captureStack = true)
    {
      if (root==null) return null;

      var we = root as WrappedException;
      if (we==null)
       we = new WrappedException( new WrappedExceptionData(root, captureStack) );

      return we;
    }

    public static WrappedException MakeFromBSON(BSONSerializer serializer, BSONDocument doc)
    {
      var wrp = doc["wrp"] as BSONDocumentElement;
      var result = wrp==null ? new WrappedException() : new WrappedException(wrp.Value.TryGetObjectValueOf("msg").AsString());
      serializer.Deserialize(doc, result);
      return result;
    }

    internal WrappedException() {}
    internal WrappedException(string msg): base(msg){}


    public WrappedException(WrappedExceptionData data) : base(data.Message) { m_Wrapped = data; }
    public WrappedException(string message, WrappedExceptionData data) : base(message) { m_Wrapped = data; }

    private WrappedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      m_Wrapped = (WrappedExceptionData)info.GetValue(WRAPPED_FLD_NAME, typeof(WrappedExceptionData));
    }

    private WrappedExceptionData m_Wrapped;

    /// <summary>
    /// Returns wrapped exception data
    /// </summary>
    public WrappedExceptionData Wrapped { get { return m_Wrapped; } }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new AzosException(StringConsts.ARGUMENT_ERROR + GetType().Name + ".GetObjectData(info=null)");
      info.AddValue(WRAPPED_FLD_NAME, m_Wrapped);
      base.GetObjectData(info, context);
    }

    public void SerializeToBSON(BSONSerializer serializer, BSONDocument doc, IBSONSerializable parent, ref object context)
    {
      serializer.AddTypeIDField(doc, parent, this, context);
      doc.Set( new BSONDocumentElement("wrp", serializer.Serialize(m_Wrapped, parent: this)));
    }

    public bool IsKnownTypeForBSONDeserialization(Type type)
    {
      return type==typeof(WrappedExceptionData);
    }

    public void DeserializeFromBSON(BSONSerializer serializer, BSONDocument doc, ref object context)
    {
      var iv = doc["wrp"] as BSONDocumentElement;
      if (iv==null) return;

      m_Wrapped = new WrappedExceptionData();
      serializer.Deserialize(iv.Value, m_Wrapped);
    }
  }

}