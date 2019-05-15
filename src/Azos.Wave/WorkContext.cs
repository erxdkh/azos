/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;

using Azos.Log;
using Azos.Web;

using Azos.Serialization.JSON;
using Azos.Web.GeoLookup;
using Azos.Platform;

namespace Azos.Wave
{
  /// <summary>
  /// Represents a context for request/response server processing in WAVE framework
  /// </summary>
  [Serialization.Slim.SlimSerializationProhibited]
  public class WorkContext : DisposableObject
  {
    #region .ctor/.dctor
      private static AsyncFlowMutableLocal<WorkContext> ats_Current = new AsyncFlowMutableLocal<WorkContext>();

      /// <summary>
      /// Returns the current call context/thread/async flow instance
      /// </summary>
      public static WorkContext Current => ats_Current.Value;

      internal WorkContext(WaveServer server, HttpListenerContext listenerContext)
      {
        m_ID = Guid.NewGuid();
        m_Server = server;
        m_ListenerContext = listenerContext;
        m_Response = new Response(this, listenerContext.Response);

        ats_Current.Value = this;
        Interlocked.Increment(ref m_Server.m_stat_WorkContextCtor);
      }

      /// <summary>
      /// Warning: if overridden, must call base otherwise semaphore will not get released
      /// </summary>
      protected override void Destructor()
      {
        if (m_Server.m_InstrumentationEnabled)
        {
          Interlocked.Increment(ref m_Server.m_stat_WorkContextDctor);
          if (m_Aborted) Interlocked.Increment(ref m_Server.m_stat_WorkContextAborted);
          if (m_Handled) Interlocked.Increment(ref m_Server.m_stat_WorkContextHandled);
          if (m_NoDefaultAutoClose) Interlocked.Increment(ref m_Server.m_stat_WorkContextNoDefaultClose);
        }

        ats_Current.Value = null;
        ReleaseWorkSemaphore();
        m_Response.Dispose();
      }
    #endregion

    #region Fields
      private Guid m_ID;
      private WaveServer m_Server;
      private bool m_WorkSemaphoreReleased;

      private HttpListenerContext m_ListenerContext;
      internal IPEndPoint m_EffectiveCallerIPEndPoint;//set by filters
      private Response m_Response;

      internal Filters.SessionFilter m_SessionFilter;
      internal WaveSession m_Session;

      internal Filters.PortalFilter m_PortalFilter;
      internal Portal m_Portal;
      internal Theme m_PortalTheme;
      internal WorkMatch m_PortalMatch;
      internal JsonDataMap m_PortalMatchedVars;


      private object m_ItemsLock = new object();
      private volatile ConcurrentDictionary<object, object> m_Items;

      internal WorkHandler m_Handler;

      private WorkMatch m_Match;
      private JsonDataMap m_MatchedVars;
                     /// <summary>
                     /// Internal method. Developers do not call
                     /// </summary>
                     internal void ___SetWorkMatch(WorkMatch match, JsonDataMap vars){m_Match = match; m_MatchedVars = vars;}

      private bool m_HasParsedRequestBody;
      private JsonDataMap m_RequestBodyAsJSONDataMap;
      private JsonDataMap m_WholeRequestAsJSONDataMap;

      internal bool m_Handled;
      private bool m_Aborted;

      private bool m_NoDefaultAutoClose;

      private GeoEntity m_GeoEntity;

      private bool m_IsAuthenticated;
    #endregion

    #region Properties


      /// <summary>
      /// Uniquely identifies the request
      /// </summary>
      public Guid ID{ get{ return m_ID;} }

      /// <summary>
      /// Returns the application that this context is under
      /// </summary>
      public IApplication App => m_Server.App;

      /// <summary>
      /// Returns the server that this context is under
      /// </summary>
      public WaveServer Server { get { return m_Server;} }

      /// <summary>
      /// Returns true to indicate that work semaphore has been already released.
      /// It is not necessary to use this property or ReleaseWorkSemaphore() method as the framework does it
      ///  automatically in 99% cases. ReleaseWorkSemaphore() may need to be called from special places like HTTP streaming
      ///   servers that need to keep WorkContext instances open for a long time
      /// </summary>
      public bool WorkSemaphoreReleased { get{ return m_WorkSemaphoreReleased;}}


      /// <summary>
      /// Returns HttpListenerRequest object for this context
      /// </summary>
     //todo Wrap in Wave.Request object (just like Response)
      public HttpListenerRequest Request { get { return m_ListenerContext.Request;} }


      /// <summary>
      /// Returns the effective caller endpoint- that is, if the real caller filter is set it will inject the real IP
      /// as seen before any proxy devices. By default this property returns the Request.RemoteEndPoint
      /// </summary>
      public IPEndPoint EffectiveCallerIPEndPoint
      {
        get{ return m_EffectiveCallerIPEndPoint ?? Request.RemoteEndPoint;}
      }


      /// <summary>
      /// Returns Response object for this context
      /// </summary>
      public Response Response { get { return m_Response;} }

      /// <summary>
      /// Returns session that this context is linked with or null
      /// </summary>
      public WaveSession Session { get {return m_Session;} }

      /// <summary>
      /// Returns the first session filter which was injected in the processing line.
      /// It is the filter that manages the session state for this context
      /// </summary>
      public Filters.SessionFilter SessionFilter {get{ return m_SessionFilter;}}

      /// <summary>
      /// Returns true when the context was configured to support SessionFilter so Session can be injected
      /// </summary>
      public bool SupportsSession { get{ return m_SessionFilter!=null;}}

      /// <summary>
      /// Returns portal object for this request or null if no portal was injected
      /// </summary>
      public Portal Portal { get { return m_Portal;} }

               /// <summary>
               /// DEVELOPERS do not use!
               /// A hack method needed in some VERY RARE cases, like serving an error page form the filter which is out of portal scope.
               /// </summary>
               public void ___InternalInjectPortal(Portal portal = null,
                                                   Theme theme = null,
                                                   WorkMatch match = null,
                                                   JsonDataMap matchedVars = null)
                                                   {
                                                     m_Portal = portal;
                                                     m_PortalTheme = theme;
                                                     m_PortalMatch = match;
                                                     m_PortalMatchedVars = matchedVars;
                                                   }

      /// <summary>
      /// Returns the first portal filter which was injected in the processing line.
      /// It is the filter that manages the portals for this context
      /// </summary>
      public Filters.PortalFilter PortalFilter {get{ return m_PortalFilter;}}

      /// <summary>
      /// Returns matched that was made by portal filter or null
      /// </summary>
      public WorkMatch PortalMatch {get{ return m_PortalMatch;}}

      /// <summary>
      /// Gets/sets portal theme. This may be null as this is just a holder variable
      /// </summary>
      public Theme PortalTheme
      {
        get{ return m_PortalTheme ?? (m_Portal!=null ? m_Portal.DefaultTheme :  null);}
        set{ m_PortalTheme = value;}
      }


      /// <summary>
      /// Returns variables that have been extracted by WorkMatch when PortalFilter assigned portal.
      /// Returns null if no portal was matched
      /// </summary>
      public JsonDataMap PortalMatchedVars{  get { return m_PortalMatchedVars;}}


      /// <summary>
      /// Returns the work match instances that was made for this requested work or null if nothing was matched yet
      /// </summary>
      public WorkMatch Match {get{ return m_Match;}}

      /// <summary>
      /// Returns variables that have been extracted by WorkMatch when dispatcher assigned request to WorkHandler.
      /// If variables have not been assigned yet returns empty object
      /// </summary>
      public JsonDataMap MatchedVars
      {
        get
        {
          if (m_MatchedVars==null)
            m_MatchedVars = new JsonDataMap(false);

          return m_MatchedVars;
        }
      }

      /// <summary>
      /// Returns dynamic object that contains variables that have been extracted by WorkMatch when dispatcher assigned request to WorkHandler.
      /// If variables have not been assigned yet returns empty object
      /// </summary>
      public dynamic Matched{ get { return new JsonDynamicObject(MatchedVars);} }



      /// <summary>
      /// Fetches request body: multi-part content, URL encoded content, or JSON body into one JSONDataMap bag,
      /// or null if there is no body.
      /// The property does caching
      /// </summary>
      public JsonDataMap RequestBodyAsJSONDataMap
      {
        get
        {
          if (!m_HasParsedRequestBody)
          {
            m_RequestBodyAsJSONDataMap = ParseRequestBodyAsJSONDataMap();
            m_HasParsedRequestBody = true;
          }
          return m_RequestBodyAsJSONDataMap;
        }
      }

      /// <summary>
      /// Fetches matched vars, multi-part content, URL encoded content, or JSON body into one JSONDataMap bag.
      /// The property does caching
      /// </summary>
      public JsonDataMap WholeRequestAsJSONDataMap
      {
        get
        {
          if (m_WholeRequestAsJSONDataMap==null)
            m_WholeRequestAsJSONDataMap = GetWholeRequestAsJSONDataMap();
          return m_WholeRequestAsJSONDataMap;
        }
      }


      /// <summary>
      /// Provides a thread-safe dictionary of items. The underlying collection is lazily allocated
      /// </summary>
      public ConcurrentDictionary<object, object> Items
      {
          get
          {
            if (m_Items==null)
                lock(m_ItemsLock)
                {
                  if (m_Items==null)
                    m_Items = new ConcurrentDictionary<object,object>(4, 16);
                }
            return m_Items;
          }
      }

      /// <summary>
      /// Returns the work handler instance that was matched to perform work on this context or null if the match has not been made yet
      /// </summary>
      public WorkHandler Handler {get{ return m_Handler;}}


      /// <summary>
      /// Returns true when the work has been executed by the WorkHandler instance
      /// </summary>
      public bool Handled {get{return m_Handled;}}

      /// <summary>
      /// Indicates whether the work context is logically finished and its nested processing (i.e. through Filters/Handlers) should stop.
      /// For example, when some filter detects a special condition (judging by the request) and generates the response
      ///  and needs to abort the work request so it does no get filtered/processed anymore, it can set this property to true.
      /// This mechanism performs much better than throwing exceptions
      /// </summary>
      public bool Aborted
      {
        get {return m_Aborted;}
        set {m_Aborted = value;}
      }


      /// <summary>
      /// Generates short context description
      /// </summary>
      public string About
      {
        get
        {
          return "Work('{0}'@'{1}' -> {2} '{3}')".Args(Request.UserAgent, EffectiveCallerIPEndPoint, Request.HttpMethod, Request.Url);
        }
      }

      /// <summary>
      /// Indicates whether the default dispatcher should close the WorkContext upon completion of async processing.
      /// This property may ONLY be set to TRUE IF Response.Buffered = false (chunked transfer) and Response has already been written to.
      /// When this property is set to true the WorkDispatcher will not auto dispose this WorkContext instance.
      /// This may be needed for a server that streams chat messages and some other thread manages the lifetime of this WorkContext.
      /// Keep in mind that alternative implementations of WorkDispatcher (derived classes that implement alternative threading/lifecycle)
      ///  may disregard this flag altogether
      /// </summary>
      public bool NoDefaultAutoClose
      {
        get { return m_NoDefaultAutoClose;}
        set
        {
          if ( value && (Response.Buffered==true || !Response.WasWrittenTo))
            throw new WaveException(StringConsts.WORK_NO_DEFAULT_AUTO_CLOSE_ERROR);

          m_NoDefaultAutoClose = value;
        }
      }

      /// <summary>
      /// Captures last error
      /// </summary>
      public Exception LastError{get; set;}

      /// <summary>
      /// Gets sets geo location information as detected by GeoLookupHandler.
      /// If Session context is injected then get/set passes through into session object
      /// </summary>
      public GeoEntity GeoEntity
      {
        get { return m_Session==null? m_GeoEntity : m_Session.GeoEntity;}
        set { if (m_Session==null)  m_GeoEntity = value; else  m_Session.GeoEntity = value;}
      }


         private bool? m_RequestedJSON;
      /// <summary>
      /// Returns true if client indicated in response that "application/json" is accepted
      /// </summary>
      public bool RequestedJSON
      {
        get
        {
          if (!m_RequestedJSON.HasValue)
            m_RequestedJSON = Request.AcceptTypes != null && Request.AcceptTypes.Any(at => at != null && at.IndexOf(ContentType.JSON, StringComparison.OrdinalIgnoreCase) != -1);

          return m_RequestedJSON.Value;
        }
      }

      /// <summary>
      /// Indicates that request method id POST
      /// </summary>
      public bool IsPOST { get{ return Request.HttpMethod.EqualsOrdIgnoreCase("POST");}}

      /// <summary>
      /// Indicates that request method id GET
      /// </summary>
      public bool IsGET { get{ return Request.HttpMethod.EqualsOrdIgnoreCase("GET");}}

      /// <summary>
      /// Indicates that request method id PUT
      /// </summary>
      public bool IsPUT { get{ return Request.HttpMethod.EqualsOrdIgnoreCase("PUT");}}

      /// <summary>
      /// Indicates that request method id DELETE
      /// </summary>
      public bool IsDELETE { get{ return Request.HttpMethod.EqualsOrdIgnoreCase("DELETE");}}

      /// <summary>
      /// Indicates that request method id PATCH
      /// </summary>
      public bool IsPATCH { get{ return Request.HttpMethod.EqualsOrdIgnoreCase("PATCH");}}

      /// <summary>
      /// Returns true to indicate that this context is/was authenticated.
      /// Used to not redirect users to login page on authorization exception
      /// </summary>
      public bool IsAuthenticated { get { return m_IsAuthenticated; } }
    #endregion

    #region Public

      /// <summary>
      /// Releases work semaphore that throttles the processing of WorkContext instances.
      /// The WorkContext is released automatically in destructor, however there are cases when the semaphore release
      /// may be needed sooner, i.e. in a HTTP streaming application where work context instances are kept open indefinitely
      /// it may not be desirable to consider long-living work context instances as a throttling factor.
      /// Returns true if semaphore was released, false if it was not released during this call as it was already released before
      /// </summary>
      public bool ReleaseWorkSemaphore()
      {
        if (m_Server!=null && m_Server.Running && !m_WorkSemaphoreReleased)
        {
          var workCount = m_Server.m_WorkSemaphore.Release();
          m_WorkSemaphoreReleased = true;
          if (m_Server.m_InstrumentationEnabled)
          {
            Interlocked.Increment(ref m_Server.m_stat_WorkContextWorkSemaphoreRelease);
            Thread.VolatileWrite(ref m_Server.m_stat_ServerWorkSemaphoreCount, workCount);
          }
          return true;
        }
        return false;
      }


      /// <summary>
      /// Ensures that session is injected if session filter is present in processing chain.
      /// If session is already available (Session!=null) then does nothing, otherwise
      /// fills Session property with either NEW session (if onlyExisting=false(default)) if user supplied no session token,
      /// OR gets session from session store as defined by the first SessionFilter in the chain
      /// </summary>
      public WaveSession NeedsSession(bool onlyExisting = false)
      {
        if (m_Session!=null) return m_Session;

        Interlocked.Increment(ref m_Server.m_stat_WorkContextNeedsSession);

        if (m_SessionFilter!=null)
          m_SessionFilter.FetchExistingOrMakeNewSession(this, onlyExisting);
        else
          throw new WaveException(StringConsts.SESSION_NOT_AVAILABLE_ERROR.Args(About));

        return m_Session;
      }


      /// <summary>
      /// Facilitates context-aware logging
      /// </summary>
      public void Log(MessageType type, string text, string from = null, Exception error = null, string pars = null, Guid? related = null)
      {
        var msg = new Message
        {
          Type = type,
          Topic = SysConsts.WAVE_LOG_TOPIC,
          From = from.IsNotNullOrWhiteSpace() ? from : About,
          Text = text,
          Exception = error ?? LastError,
          Parameters = pars
        };

        if (related.HasValue)
          msg.RelatedTo = related.Value;
        else
          msg.RelatedTo = this.m_ID;

        App.Log.Write(msg);
      }

      /// <summary>
      /// Returns true if the whole request (body or matched vars) contains any names matching any field names of the specified document
      /// </summary>
      public bool HasAnyVarsMatchingFieldNames(Data.Doc doc)
      {
        if (doc == null) return false;

        foreach(var fdef in doc.Schema)
         if (WholeRequestAsJSONDataMap.ContainsKey(fdef.Name)) return true;

        return false;
      }

      public override string ToString()
      {
        return About;
      }

      /// <summary>
      /// Invoked by applications to signify the presence of authentication
      /// </summary>
      public void SetAuthenticated(bool value)
      {
        m_IsAuthenticated = value;
      }
    #endregion


    #region Protected

      /// <summary>
      /// Converts request body and MatchedVars into a single JSONDataMap. Users should call WholeRequestAsJSONDataMap.get() as it caches the result
      /// </summary>
      protected virtual JsonDataMap GetWholeRequestAsJSONDataMap()
      {
        var body = this.RequestBodyAsJSONDataMap;

        if (body==null) return MatchedVars;

        var result = new JsonDataMap(false);
        result.Append(MatchedVars)
              .Append(body);
        return result;
      }

      /// <summary>
      /// This method is called only once as it touches the input streams
      /// </summary>
      protected virtual JsonDataMap ParseRequestBodyAsJSONDataMap()
      {
        if (!Request.HasEntityBody) return null;

        JsonDataMap result = null;

        var ctp = Request.ContentType;

        //Has body by no content type
        if (ctp==null)
        {
          throw HTTPStatusException.NotAcceptable_406("Missing content-type");
        }
        //Multi-part
        if (ctp.IndexOf(ContentType.FORM_MULTIPART_ENCODED)>=0)
        {
          var boundary = Multipart.ParseContentType(ctp);
          var mp = Multipart.ReadFromStream(Request.InputStream, ref boundary, Request.ContentEncoding);
          result =  mp.ToJSONDataMap();
        }
        else //Form URL encoded
        if (ctp.IndexOf(ContentType.FORM_URL_ENCODED)>=0)
          result = JsonDataMap.FromURLEncodedStream(new Azos.IO.NonClosingStreamWrap(Request.InputStream),
                                                  Request.ContentEncoding);
        else//JSON
        if (ctp.IndexOf(ContentType.JSON)>=0)
          result = JsonReader.DeserializeDataObject(new Azos.IO.NonClosingStreamWrap(Request.InputStream),
                                                  Request.ContentEncoding) as JsonDataMap;

        return result;
      }

    #endregion

  }





}
