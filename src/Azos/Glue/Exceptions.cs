/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Runtime.Serialization;

namespace Azos.Glue
{
  /// <summary>
  /// Base exception thrown by the Glue framework
  /// </summary>
  [Serializable]
  public class GlueException : AzosException
  {
    public GlueException() { }
    public GlueException(string message) : base(message) { }
    public GlueException(string message, Exception inner) : base(message, inner) { }
    protected GlueException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Base exception thrown by the Glue framework when some operations are invoked that do not apply
  /// </summary>
  [Serializable]
  public class InvalidGlueOperationException : GlueException
  {
    public InvalidGlueOperationException() { }
    public InvalidGlueOperationException(string message) : base(message) { }
    public InvalidGlueOperationException(string message, Exception inner) : base(message, inner) { }
    protected InvalidGlueOperationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Thrown by the Glue framework when clients try to perfom a call but that action fails
  /// </summary>
  [Serializable]
  public class ClientCallException : GlueException
  {
    public const string STATUS_FLD_NAME = "CCE-S";

    public ClientCallException(CallStatus status) : base(StringConsts.GLUE_CLIENT_CALL_ERROR + status.ToString()) { Status = status; }
    public ClientCallException(CallStatus status, string message) : base(message) { Status = status; }
    public ClientCallException(CallStatus status, string message, Exception inner) : base(message, inner) { Status = status; }
    protected ClientCallException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      Status = (CallStatus)info.GetInt32(STATUS_FLD_NAME);
    }

    /// <summary>
    /// Returns call status enum
    /// </summary>
    public readonly CallStatus Status;

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new AzosException(StringConsts.ARGUMENT_ERROR + GetType().Name + ".GetObjectData(info=null)");
      info.AddValue(STATUS_FLD_NAME, Status);
      base.GetObjectData(info, context);
    }
  }

  /// <summary>
  /// Thrown by the Glue framework when client message inspector fails with exception
  /// </summary>
  [Serializable]
  public class ClientMsgInspectionException : GlueException
  {
    public ClientMsgInspectionException(IClientMsgInspector inspector, Exception error)
      : base(StringConsts.GLUE_CLIENT_INSPECTORS_THREW_ERROR + error.Message, error) { m_Inspector = inspector; }
    protected ClientMsgInspectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    [NonSerialized]
    private IClientMsgInspector m_Inspector;

    /// <summary>
    /// Returns inspector that threw exception
    /// </summary>
    public IClientMsgInspector Inspector { get { return m_Inspector; } }
  }


  /// <summary>
  /// Base exception thrown by the Glue framework when remote errors are reported back to caller
  /// </summary>
  [Serializable]
  public class RemoteException : GlueException
  {
    public const string REMOTE_FLD_NAME = "RE-R";

    public RemoteException(WrappedExceptionData data) : base(data.ToString()) { m_Remote = data; }
    public RemoteException(string message, WrappedExceptionData data) : base(message) { m_Remote = data; }
    public RemoteException(string message, WrappedExceptionData data, Exception inner) : base(message, inner) { m_Remote = data; }
    protected RemoteException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      m_Remote = (WrappedExceptionData)info.GetValue(REMOTE_FLD_NAME, typeof(WrappedExceptionData));
    }

    private WrappedExceptionData m_Remote;

    /// <summary>
    /// Returns remote exception data
    /// </summary>
    public WrappedExceptionData Remote { get { return m_Remote; } }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new AzosException(StringConsts.ARGUMENT_ERROR + GetType().Name + ".GetObjectData(info=null)");
      info.AddValue(REMOTE_FLD_NAME, m_Remote);
      base.GetObjectData(info, context);
    }
  }


  /// <summary>
  /// Exception thrown when there are protocol-related errors like deserialization,
  /// request message received when response is expected
  /// </summary>
  [Serializable]
  public class ProtocolException : GlueException
  {
    public const string CLOSE_CHANNEL_FLD_NAME = "PE-CC";

    public ProtocolException(bool closeChannel = false) { m_CloseChannel = closeChannel; }
    public ProtocolException(string message, bool closeChannel = false) : base(message) { m_CloseChannel = closeChannel; }
    public ProtocolException(string message, Exception inner, bool closeChannel = false) : base(message, inner) { m_CloseChannel = closeChannel; }
    protected ProtocolException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      m_CloseChannel = info.GetBoolean(CLOSE_CHANNEL_FLD_NAME);
    }

    private bool m_CloseChannel;

    /// <summary>
    /// Returns true when error is not recoverable on the existing channel and it needs to be closed
    /// </summary>
    public bool CloseChannel { get { return m_CloseChannel; } }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new AzosException(StringConsts.ARGUMENT_ERROR + GetType().Name + ".GetObjectData(info=null)");
      info.AddValue(CLOSE_CHANNEL_FLD_NAME, m_CloseChannel);
      base.GetObjectData(info, context);
    }
  }

  /// <summary>
  /// Exception thrown in client/server when there's an attempt to send a too large message
  /// </summary>
  [Serializable]
  public class MessageSizeException : ProtocolException
  {
    public MessageSizeException(int size, int limit, string operation, bool closeChannel = false)
      : base(StringConsts.GLUE_MAX_MSG_SIZE_EXCEEDED_ERROR.Args(size, limit, operation), closeChannel) { }

    protected MessageSizeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Exception thrown in Glue server
  /// </summary>
  [Serializable]
  public abstract class ServerException : GlueException
  {
    public ServerException() { }
    public ServerException(string message) : base(message) { }
    public ServerException(string message, Exception inner) : base(message, inner) { }
    protected ServerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Exception thrown in Glue server when it shuts down/not running
  /// </summary>
  [Serializable]
  public class ServerNotRunningException : ServerException
  {
    public ServerNotRunningException() { }
    public ServerNotRunningException(string message) : base(message) { }
    public ServerNotRunningException(string message, Exception inner) : base(message, inner) { }
    protected ServerNotRunningException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Exception thrown when server could not get contract
  /// </summary>
  [Serializable]
  public class ServerContractException : ServerException
  {
    public ServerContractException() { }
    public ServerContractException(string message) : base(message) { }
    public ServerContractException(string message, Exception inner) : base(message, inner) { }
    protected ServerContractException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Exception thrown when server instance can not be created
  /// </summary>
  [Serializable]
  public class ServerInstanceActivationException : ServerException
  {
    public ServerInstanceActivationException() { }
    public ServerInstanceActivationException(string message) : base(message) { }
    public ServerInstanceActivationException(string message, Exception inner) : base(message, inner) { }
    protected ServerInstanceActivationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Exception thrown when statful server identified is not found/has expired/timed-out
  /// </summary>
  [Serializable]
  public class StatefulServerInstanceDoesNotExistException : ServerInstanceActivationException
  {
    public StatefulServerInstanceDoesNotExistException() { }
    public StatefulServerInstanceDoesNotExistException(string message) : base(message) { }
    public StatefulServerInstanceDoesNotExistException(string message, Exception inner) : base(message, inner) { }
    protected StatefulServerInstanceDoesNotExistException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Exception thrown when statful server instance is not thread safe and could not be locked before set timeout expired
  /// </summary>
  [Serializable]
  public class StatefulServerInstanceLockTimeoutException : ServerInstanceActivationException
  {
    public StatefulServerInstanceLockTimeoutException() { }
    public StatefulServerInstanceLockTimeoutException(string message) : base(message) { }
    public StatefulServerInstanceLockTimeoutException(string message, Exception inner) : base(message, inner) { }
    protected StatefulServerInstanceLockTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }


  /// <summary>
  /// Exception thrown when server can not invoke method
  /// </summary>
  [Serializable]
  public class ServerMethodInvocationException : ServerException
  {
    public ServerMethodInvocationException() { }
    public ServerMethodInvocationException(string message) : base(message) { }
    public ServerMethodInvocationException(string message, Exception inner) : base(message, inner) { }
    protected ServerMethodInvocationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }
}
