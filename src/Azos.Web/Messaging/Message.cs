/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Linq;

using Azos.Data;
using Azos.Serialization.Arow;


namespace Azos.Web.Messaging
{
  /// <summary>
  /// Represents an email msg that needs to be sent
  /// </summary>
  [Serializable]
  [Arow("31B5D987-5DBF-4CE9-AFFA-6684005D2F8F")]
  public class Message : TypedDoc
  {
    [Serializable]
    [Arow("593907F9-0577-466F-8228-03C4EB24AE50")]
    public class Attachment : TypedDoc
    {
      public Attachment(string name, byte[] content, string contentType)
      {
        Name = name;
        Content = content;
        ContentType = contentType ?? Azos.Web.ContentType.BINARY;
      }
      [Field(backendName: "nm", isArow: true)]   public string Name { get; set; }
      [Field(backendName: "ct", isArow: true)]   public byte[] Content { get; set; }
      [Field(backendName: "curl", isArow: true)] public string ContentURL { get; set; }
      [Field(backendName: "tp", isArow: true)]   public string ContentType { get; set; }

      /// <summary>
      /// Returns true to indicate that the content has fetched either as byte[] or URL (that yet needs to be fetched).
      /// This is used in fetching messages back from the store where their attachments must be
      /// fetched using a separate call due to their sheer size
      /// </summary>
      public bool HasContent{ get{ return Content!= null || ContentURL.IsNotNullOrWhiteSpace();} }
    }

    public Message(){ }

    public Message(Guid? id, DateTime? utcCreateDate = null)
    {
      ID = id ?? Guid.NewGuid();
      Priority = MsgPriority.Normal;
      CreateDateUTC = utcCreateDate ?? Ambient.UTCNow;
    }


    /// <summary>
    /// Every message has an ID of type GUID generated upon the creation, it is used for unique identification
    /// in small systems and message co-relation into conversation threads
    /// </summary>
    [Field(backendName: "id", isArow: true)] public Guid  ID { get; private set;}

    /// <summary>
    /// When set, identifies the message in a thread which this one relates to
    /// </summary>
    [Field(backendName: "rel", isArow: true)] public Guid?  RelatedID { get; set;}

    [Field(backendName: "cdt", isArow: true)] public DateTime CreateDateUTC { get; set;}

    [Field(backendName: "pr", isArow: true)] public MsgPriority   Priority   { get; set;}
    [Field(backendName: "im", isArow: true)] public MsgImportance Importance { get; set;}

    [Field(backendName: "a_frm", isArow: true)]  public string AddressFrom    { get{ return m_AddressFrom;   }  set{ m_AddressFrom    = value; m_Builder_AddressFrom    = null;} }
    [Field(backendName: "a_rto", isArow: true)]  public string AddressReplyTo { get{ return m_AddressReplyTo;}  set{ m_AddressReplyTo = value; m_Builder_AddressReplyTo = null;} }
    [Field(backendName: "a_to",  isArow: true)]  public string AddressTo      { get{ return m_AddressTo;     }  set{ m_AddressTo      = value; m_Builder_AddressTo      = null;} }
    [Field(backendName: "a_cc",  isArow: true)]  public string AddressCC      { get{ return m_AddressCC;     }  set{ m_AddressCC      = value; m_Builder_AddressCC      = null;} }
    [Field(backendName: "a_bcc", isArow: true)]  public string AddressBCC     { get{ return m_AddressBCC;    }  set{ m_AddressBCC     = value; m_Builder_AddressBCC     = null;} }

    /// <summary>Subject short text </summary>
    [Field(backendName: "sb", isArow: true)] public string Subject{ get; set; }

    /// <summary>Short text body </summary>
    [Field(backendName: "short", isArow: true)] public string ShortBody{ get; set; }

    /// <summary>Plain/text body </summary>
    [Field(backendName: "plain", isArow: true)] public string Body{ get; set; }

    /// <summary>Rich-formatted body per content type </summary>
    [Field(backendName: "rich", isArow: true)] public string RichBody{ get; set; }

    /// <summary>Rich body content type </summary>
    [Field(backendName: "rctp", isArow: true)] public string RichBodyContentType{ get; set; }

    /// <summary>Collection of Attachments </summary>
    [Field(backendName: "ats", isArow: true)] public Attachment[] Attachments { get; set; }

    private string m_AddressFrom;
    private string m_AddressReplyTo;
    private string m_AddressTo;
    private string m_AddressCC;
    private string m_AddressBCC;

    [NonSerialized]private MessageAddressBuilder m_Builder_AddressFrom;
    [NonSerialized]private MessageAddressBuilder m_Builder_AddressReplyTo;
    [NonSerialized]private MessageAddressBuilder m_Builder_AddressTo;
    [NonSerialized]private MessageAddressBuilder m_Builder_AddressCC;
    [NonSerialized]private MessageAddressBuilder m_Builder_AddressBCC;

    public MessageAddressBuilder AddressFromBuilder    { get{ return m_Builder_AddressFrom    ?? (m_Builder_AddressFrom    = new MessageAddressBuilder(m_AddressFrom,   (b) => m_AddressFrom    = b.ToString())); } }
    public MessageAddressBuilder AddressReplyToBuilder { get{ return m_Builder_AddressReplyTo ?? (m_Builder_AddressReplyTo = new MessageAddressBuilder(m_AddressReplyTo,(b) => m_AddressReplyTo = b.ToString())); } }
    public MessageAddressBuilder AddressToBuilder      { get{ return m_Builder_AddressTo      ?? (m_Builder_AddressTo      = new MessageAddressBuilder(m_AddressTo,     (b) => m_AddressTo      = b.ToString())); } }
    public MessageAddressBuilder AddressCCBuilder      { get{ return m_Builder_AddressCC      ?? (m_Builder_AddressCC      = new MessageAddressBuilder(m_AddressCC,     (b) => m_AddressCC      = b.ToString())); } }
    public MessageAddressBuilder AddressBCCBuilder     { get{ return m_Builder_AddressBCC     ?? (m_Builder_AddressBCC     = new MessageAddressBuilder(m_AddressBCC,    (b) => m_AddressBCC     = b.ToString())); } }


    public override ValidState Validate(ValidState state)
    {
      state = base.Validate(state);
      if (state.ShouldStop) return state;

      try  { var b = AddressFromBuilder; }
      catch(Exception error) { return new ValidState(state, new FieldValidationException(this.Schema.DisplayName, "AddressFrom", error.ToMessageWithType())); }

      try { var b = AddressReplyToBuilder; }
      catch (Exception error) { return new ValidState(state, new FieldValidationException(this.Schema.DisplayName, "AddressReplyTo", error.ToMessageWithType())); }

      try { var b = AddressCCBuilder; }
      catch (Exception error) { return new ValidState(state, new FieldValidationException(this.Schema.DisplayName, "AddressCC", error.ToMessageWithType())); }

      try { var b = AddressBCCBuilder; }
      catch (Exception error) { return new ValidState(state, new FieldValidationException(this.Schema.DisplayName, "AddressBCC", error.ToMessageWithType())); }

      try
      {
        var b = AddressToBuilder;
        if (!b.All.Any()) return new ValidState(state, new FieldValidationException(this.Schema.DisplayName, "AddressTo", "No TO"));
      }
      catch(Exception error) { return new ValidState(state, new FieldValidationException(this.Schema.DisplayName, "AddressTo", error.ToMessageWithType())); }

      return state;
    }
  }
}
