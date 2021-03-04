/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Threading;

using Azos.Apps;
using Azos.Data;
using Azos.Conf;

namespace Azos.Wave.Filters
{
  /// <summary>
  /// Manages session state in work context
  /// </summary>
  public class SessionFilter : WorkFilter
  {
    #region CONSTS
      public const string CONF_COOKIE_NAME_ATTR = "session-cookie-name";
      public const string CONF_GATE_NEW_SESSION_VAR_ATTR = "gate-new-session-var";
      public const string DEFAULT_COOKIE_NAME = "SID";

      public const string CONF_SESSION_TIMEOUT_MS_ATTR = "session-timeout-ms";

      public const int DEFAULT_SESSION_TIMEOUT_MS = 5 *  //min
                                                    60 * //sec
                                                    1000;//msec

    #endregion

    #region .ctor
      public SessionFilter(WorkDispatcher dispatcher, string name, int order) : base(dispatcher, name, order) {}
      public SessionFilter(WorkDispatcher dispatcher, IConfigSectionNode confNode): base(dispatcher, confNode) {ctor(confNode);}
      public SessionFilter(WorkHandler handler, string name, int order) : base(handler, name, order) {}
      public SessionFilter(WorkHandler handler, IConfigSectionNode confNode): base(handler, confNode) {ctor(confNode);}

      private void ctor(IConfigSectionNode confNode)
      {
        m_CookieName = confNode.AttrByName(CONF_COOKIE_NAME_ATTR).ValueAsString(DEFAULT_COOKIE_NAME);
        m_SessionTimeoutMs = confNode.AttrByName(CONF_SESSION_TIMEOUT_MS_ATTR).ValueAsInt(DEFAULT_SESSION_TIMEOUT_MS);
        m_GateNewSessionVar = confNode.AttrByName(CONF_GATE_NEW_SESSION_VAR_ATTR).Value;
    }

    #endregion

    #region Fields

     private string m_GateNewSessionVar;
     private string m_CookieName = DEFAULT_COOKIE_NAME;
     private int m_SessionTimeoutMs = DEFAULT_SESSION_TIMEOUT_MS;

    #endregion

    #region Properties

      /// <summary>
      /// Specifies session cookie name
      /// </summary>
      public string CookieName
      {
        get { return m_CookieName ?? DEFAULT_COOKIE_NAME;}
        set { m_CookieName = value; }
      }

      /// <summary>
      /// Specifies session inactivity timeout in milliseconds.
      /// For default implementation: assign 0 to use App.ObjectStore default object timeout value
      /// </summary>
      public int SessionTimeoutMs
      {
        get { return m_SessionTimeoutMs;}
        set { m_SessionTimeoutMs = value<0 ? 0 : value; }
      }


      /// <summary>
      /// When set, bumps the gate variable name if it is enabled
      /// </summary>
      public string GateNewSessionVar
      {
        get { return m_GateNewSessionVar; }
        set { GateNewSessionVar = value; }
      }

    #endregion

    #region Protected

      protected sealed override void DoFilterWork(WorkContext work, IList<WorkFilter> filters, int thisFilterIndex)
      {
        if (work.m_SessionFilter==null)
        {
          try
          {
            work.m_SessionFilter = this;
            this.InvokeNextWorker(work, filters, thisFilterIndex);
          }
          finally
          {
            StowSession(work);
            work.m_SessionFilter = null;
          }
        }
        else this.InvokeNextWorker(work, filters, thisFilterIndex);
      }

      /// <summary>
      /// Override to get session object using whatever parameters are available in response context (i.e. a cookie),
      /// or create a new one if 'onlyExisting'=false(default)
      /// </summary>
      protected internal virtual void FetchExistingOrMakeNewSession(WorkContext work, bool onlyExisting = false)
      {
        if (work.Session!=null) return;
        WaveSession session = null;
        ulong sidSecret = 0;
        var sid = ExtractSessionID(work, out sidSecret);

        if (sid.HasValue)
        {
          session = App.ObjectStore.CheckOut(sid.Value) as WaveSession;

          if (session!=null && session.IDSecret!=sidSecret)
          {
            App.ObjectStore.UndoCheckout(sid.Value);
            session = null;//The secret password does not match
            if (Server.m_InstrumentationEnabled)
              Interlocked.Increment(ref Server.m_stat_SessionInvalidID);
          }
        }

        var foundExisting = true;

        if (session==null)
        {
          if (onlyExisting)
          {
            session = TryMakeSessionFromExistingLongTermToken(work);
            if (session==null) return;//do not create anything
          }

          if (session==null)
          {
            foundExisting = false;
            var vn = m_GateNewSessionVar;
            if (NetGate!=null && NetGate.Enabled && vn.IsNotNullOrWhiteSpace())
               NetGate.IncreaseVariable(IO.Net.Gate.TrafficDirection.Incoming,
                                     work.EffectiveCallerIPEndPoint.Address.ToString(),
                                     vn,
                                     1);

            session = MakeNewSession(work);
          }
        }

        if (foundExisting && Server.m_InstrumentationEnabled)
           Interlocked.Increment(ref Server.m_stat_SessionExisting);

        session.Acquire();
        if (work.GeoEntity!=null)
          session.GeoEntity = work.GeoEntity;
        work.m_Session = session;
        Apps.ExecutionContext.__SetThreadLevelSessionContext(session);

        work.SetAuthenticated(session.User.IsAuthenticated);
      }

      /// <summary>
      /// Override in session filters that support long-term tokens.
      /// This method tries to re-create "existing" session from a valid long-term token, otherwise
      /// null should be returned (the base implementation)
      /// </summary>
      protected virtual WaveSession TryMakeSessionFromExistingLongTermToken(WorkContext work)
      {
        return null;
      }

      /// <summary>
      /// Override to put session object back into whatever storage medium is provided (i.e. DB) and
      /// respond with appropriate session identifying token(i.e. a cookie)
      /// </summary>
      protected virtual void StowSession(WorkContext work)
      {
        var session = work.m_Session;
        if (session==null) return;
        try
        {
          if (!session.IsEnded)
          {
            var regenerated = session.OldID.HasValue;

            if (regenerated)
             App.ObjectStore.CheckInUnderNewKey(session.OldID.Value, session.ID, session, m_SessionTimeoutMs);
            else
             App.ObjectStore.CheckIn(session.ID, session, m_SessionTimeoutMs);

            if (session.IsNew || regenerated)
            {
              var apiVersion = work.Request.Headers[SysConsts.HEADER_API_VERSION];
              if (apiVersion!=null)
                work.Response.AddHeader(SysConsts.HEADER_API_SESSION, EncodeSessionID( work, session, true));
              else
                work.Response.SetClientVar(m_CookieName, EncodeSessionID( work, session, false ));//the cookie is set only for non-api call
            }
          }
          else
          {
            App.ObjectStore.Delete(session.ID);

            work.Response.SetClientVar(m_CookieName, null);


            if (Server.m_InstrumentationEnabled)
               Interlocked.Increment(ref Server.m_stat_SessionEnd);
          }
        }
        finally
        {
          session.Release();
          work.m_Session = null;
          Apps.ExecutionContext.__SetThreadLevelSessionContext(null);
        }
      }

      /// <summary>
      /// Extracts session ID from work request. The default implementation uses cookie
      /// </summary>
      protected virtual Guid? ExtractSessionID(WorkContext work, out ulong idSecret)
      {
        var cv = work.Response.GetClientVar(m_CookieName);// work.Request.Cookies[m_CookieName];

        var apiHeaders = false;
        if (cv.IsNullOrWhiteSpace())
        {
          cv = work.Request.Headers[SysConsts.HEADER_API_SESSION];
          apiHeaders = true;
        }

        if (cv.IsNotNullOrWhiteSpace())
        {
          var guid = DecodeSessionID(work, cv, apiHeaders, out idSecret);
          if (guid.HasValue) return guid.Value;

          if (Server.m_InstrumentationEnabled)
              Interlocked.Increment(ref Server.m_stat_SessionInvalidID);
        }

        idSecret = 0;
        return null;
      }

      /// <summary>
      /// Override to encode session ID GUID into string representation
      /// </summary>
      protected virtual string EncodeSessionID(WorkContext work, WaveSession session, bool hasApiHeaders)
      {
        var encoded = new ELink(session.IDSecret, session.ID.ToByteArray());
        return encoded.Link;
      }

      /// <summary>
      /// Override to decode session ID GUID from string representation. Return null if conversion not possible
      /// </summary>
      protected virtual Guid? DecodeSessionID(WorkContext work, string id, bool hasApiHeaders, out ulong idSecret)
      {
        ELink encoded;
        Guid guid;
        ulong secret;

        try
        {
          encoded = new ELink(id);
          secret = encoded.ID;
          guid = new Guid(encoded.Metadata);
        }
        catch
        {
          idSecret = 0;
          return null;
        }

        idSecret = secret;
        return guid;
      }


      /// <summary>
      /// Called to create a new session
      /// </summary>
      protected WaveSession MakeNewSession(WorkContext work)
      {
        if (Server.m_InstrumentationEnabled)
          Interlocked.Increment(ref Server.m_stat_SessionNew);

        return MakeNewSessionInstance(work);
      }

      /// <summary>
      /// Override to create a new session instance
      /// </summary>
      protected virtual WaveSession MakeNewSessionInstance(WorkContext work)
      {
        return new WaveSession(Guid.NewGuid(), App.Random.NextRandomUnsignedLong);
      }


    #endregion

    #region .pvt

    #endregion
  }

}
