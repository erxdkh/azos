/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using Azos.Apps;
using Azos.Conf;

namespace Azos.Web
{
  /// <summary>
  /// Configures ServicePointManager class.
  /// Use singleton Instance property
  /// </summary>
  public sealed class ServicePointManagerConfigurator : ApplicationComponent, IConfigurable
  {
    public const int DEFAULT_CONNECT_LEASE_TIMEOUT_MS = 2 * 60_000;// 2 minutes



    #region Inner types

    /// <summary>
    /// Provides contract and default implementation for certificate trust and endpoint binding
    /// </summary>
    public class OperationPolicy : IConfigurable
    {
      protected class _uri : Collections.INamed
      {
        public string Name { get; internal set; }
        public bool Trusted { get; internal set; }
      }

      public const string CONFIG_DEFAULT_CERTIFICATE_VALIDATION_SECTION = "default-certificate-validation";
      public const string CONFIG_CASE_SECTION = "case";

      public const string CONFIG_TRUSTED_ATTR = "trusted";


      protected Collections.Registry<_uri> m_DefaultCertValUris = new Collections.Registry<_uri>();

      public bool CertValTrustedDefault { get; set; }

      public virtual void Configure(IConfigSectionNode node)
      {
        if (node == null || !node.Exists) return;
        ConfigAttribute.Apply(this, node);
        var cvn = node[CONFIG_DEFAULT_CERTIFICATE_VALIDATION_SECTION];
        CertValTrustedDefault = cvn.AttrByName(CONFIG_TRUSTED_ATTR).ValueAsBool();

        foreach (var un in cvn.Children.Where(c => c.IsSameName(CONFIG_CASE_SECTION)))
        {
          m_DefaultCertValUris.Register(new _uri { Name = un.AttrByName(CONFIG_URI_ATTR).Value,
                                      Trusted = un.AttrByName(CONFIG_TRUSTED_ATTR).ValueAsBool()});
        }
      }

      public virtual bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
      {
        if (sslPolicyErrors == SslPolicyErrors.None) return true;

        var request = sender as HttpWebRequest;
        if (request != null)
        {
          var entry = m_DefaultCertValUris.FirstOrDefault(u => request.RequestUri.AbsoluteUri.StartsWith(u.Name));
          if (entry != null) return entry.Trusted;
        }
        return CertValTrustedDefault;
      }

      public virtual IPEndPoint BindIPEndPoint(ServicePointConfigurator servicePointConfigurator, IPEndPoint remoteEndPoint, int retryCount)
      {
        return null;
      }
    }



    public sealed class ServicePointConfigurator
    {
      internal ServicePointConfigurator(ServicePointManagerConfigurator manager, ServicePoint sp, IConfigSectionNode node)
      {
        this.Manager = manager;
        this.ServicePoint = sp;
        sp.BindIPEndPointDelegate += bindIPEndPoint;
        ConfigAttribute.Apply(this, node);

        if (node.AttrByName(CONFIG_TCP_KEEPALIVE_ENABLED_ATTR).ValueAsBool())
        {
          sp.SetTcpKeepAlive(
            true,
            node.AttrByName(CONFIG_TCP_KEEPALIVE_TIME_MS_ATTR).ValueAsInt(),
            node.AttrByName(CONFIG_TCP_KEEPALIVE_INTERVAL_MS_ATTR).ValueAsInt());
        }
      }

      public readonly ServicePointManagerConfigurator Manager;
      public readonly ServicePoint ServicePoint;

      [Config(Default = DEFAULT_CONNECT_LEASE_TIMEOUT_MS)]
      public int ConnectionLeaseTimeout
      {
        get { return this.ServicePoint.ConnectionLeaseTimeout; }
        private set { this.ServicePoint.ConnectionLeaseTimeout = value; }
      }

      [Config]
      public int ConnectionLimit
      {
        get { return this.ServicePoint.ConnectionLimit; }
        private set { this.ServicePoint.ConnectionLimit = value; }
      }

      [Config("$expect-100-continue")]
      public bool Expect100Continue
      {
        get { return this.ServicePoint.Expect100Continue; }
        private set { this.ServicePoint.Expect100Continue = value; }
      }

      [Config]
      public int MaxIdleTime
      {
        get { return this.ServicePoint.MaxIdleTime; }
        private set { this.ServicePoint.MaxIdleTime = value; }
      }

      [Config]
      public int ReceiveBufferSize
      {
        get { return this.ServicePoint.ReceiveBufferSize; }
        private set { this.ServicePoint.ReceiveBufferSize = value; }
      }

      [Config]
      public bool UseNagleAlgorithm
      {
        get { return this.ServicePoint.UseNagleAlgorithm; }
        private set { this.ServicePoint.UseNagleAlgorithm = value; }
      }

      private IPEndPoint bindIPEndPoint(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
      {
        var policy = Manager.m_Policy;
        if (policy != null)
          return policy.BindIPEndPoint(this, remoteEndPoint, retryCount);

        return null;
      }
    }

    #endregion

    #region CONSTS

      public const string CONFIG_POLICY_SECTION = "policy";
      public const string CONFIG_SERVICE_POINT_SECTION = "service-point";

      public const string CONFIG_TCP_KEEPALIVE_ENABLED_ATTR = "tcp-keepalive-enabled";
      public const string CONFIG_TCP_KEEPALIVE_TIME_MS_ATTR = "tcp-keepalive-time-ms";
      public const string CONFIG_TCP_KEEPALIVE_INTERVAL_MS_ATTR = "tcp-keepalive-interval-ms";

      public const string CONFIG_URI_ATTR = "uri";

    #endregion


    internal ServicePointManagerConfigurator(IApplication app):base(app) { }

    #region Fields

      private object m_Lock = new object();
      private OperationPolicy m_Policy;
      private List<ServicePointConfigurator> m_ServicePoints = new List<ServicePointConfigurator>();

    #endregion

    #region Properties

    public override string ComponentLogTopic => CoreConsts.WEB_TOPIC;

      [Config]
      public bool CheckCertificateRevocationList
      {
        get { return ServicePointManager.CheckCertificateRevocationList; }
        private set { ServicePointManager.CheckCertificateRevocationList = value; }
      }

      [Config]
      public int DefaultConnectionLimit
      {
        get { return ServicePointManager.DefaultConnectionLimit; }
        private set { ServicePointManager.DefaultConnectionLimit = value; }
      }

      [Config]
      public int DnsRefreshTimeout
      {
        get { return ServicePointManager.DnsRefreshTimeout; }
        private set { ServicePointManager.DnsRefreshTimeout = value; }
      }

      [Config]
      public bool EnableDnsRoundRobin
      {
        get { return ServicePointManager.EnableDnsRoundRobin; }
        private set { ServicePointManager.EnableDnsRoundRobin = value; }
      }

      [Config("$expect-100-continue")]
      public bool Expect100Continue
      {
        get { return ServicePointManager.Expect100Continue; }
        private set { ServicePointManager.Expect100Continue = value; }
      }

      [Config]
      public int MaxServicePointIdleTime
      {
        get { return ServicePointManager.MaxServicePointIdleTime; }
        private set { ServicePointManager.MaxServicePointIdleTime = value; }
    }

      [Config]
      public int MaxServicePoints
      {
        get { return ServicePointManager.MaxServicePoints; }
        private set { ServicePointManager.MaxServicePoints = value; }
      }

      [Config]
      public SecurityProtocolType SecurityProtocol
      {
        get { return ServicePointManager.SecurityProtocol; }
        private set { ServicePointManager.SecurityProtocol = value; }
      }

      [Config]
      public bool UseNagleAlgorithm
      {
        get { return ServicePointManager.UseNagleAlgorithm; }
        private set { ServicePointManager.UseNagleAlgorithm = value; }
      }

      public OperationPolicy Policy
      {
        get { return m_Policy; }
        private set { m_Policy = value; }
      }

      public IEnumerable<ServicePointConfigurator> ServicePoints { get { return m_ServicePoints; } }

    #endregion

    #region Public

      void IConfigurable.Configure(IConfigSectionNode node)
      {
        lock(m_Lock)
        {
          if (node == null || !node.Exists)
            node = App.ConfigRoot[WebSettings.CONFIG_WEBSETTINGS_SECTION][WebSettings.CONFIG_SERVICEPOINTMANAGER_SECTION];

          if (!node.Exists) return;

          ConfigAttribute.Apply(this, node);

          configureRoot(node);

          ServicePointManager.ServerCertificateValidationCallback += onServerCertificateValidationCallback;
        }
      }

    #endregion

    #region .pvt

      private void configureRoot(IConfigSectionNode node)
      {
        var pnode = node[CONFIG_POLICY_SECTION];
        if (pnode.Exists)
          m_Policy = FactoryUtils.MakeAndConfigure<OperationPolicy>(pnode, typeof(OperationPolicy));

        if (node.AttrByName(CONFIG_TCP_KEEPALIVE_ENABLED_ATTR).ValueAsBool())
        {
          ServicePointManager.SetTcpKeepAlive(
            true,
            node.AttrByName(CONFIG_TCP_KEEPALIVE_TIME_MS_ATTR).ValueAsInt(),
            node.AttrByName(CONFIG_TCP_KEEPALIVE_INTERVAL_MS_ATTR).ValueAsInt());
        }

        var lst = new List<ServicePointConfigurator>();
        foreach (var nsp in node.Children.Where(c => c.IsSameName(CONFIG_SERVICE_POINT_SECTION)))
        {

          var addr = nsp.AttrByName(CONFIG_URI_ATTR).Value;
          if (addr.IsNullOrWhiteSpace()) continue;

          var sp = ServicePointManager.FindServicePoint(new Uri(addr));

          lst.Add( new ServicePointConfigurator(this, sp, nsp));
        }
        System.Threading.Thread.MemoryBarrier();
        m_ServicePoints = lst; // atomic
      }

      private bool onServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
      {
        var policy = m_Policy;
        if (policy == null) return false;

        return policy.ServerCertificateValidationCallback(sender, certificate, chain, sslPolicyErrors);
      }

    #endregion
  }

}
