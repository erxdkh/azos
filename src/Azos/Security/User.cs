/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;

using System.Security.Principal;
using System.Runtime.Serialization;

namespace Azos.Security
{
  /// <summary>
  /// Marker interface denoting entities that represent information about users
  /// depending on the particular security system implementation
  /// </summary>
  public interface IIdentityDescriptor
  {
    /// <summary>
    /// Represents identity of the descriptor such as User.ID, User.GDID etc.
    /// Specifics depend on the system
    /// </summary>
    object IdentityDescriptorID { get; }

    /// <summary>
    /// Provides descriptor name such as User.Name, User.ScreenName etc.
    /// </summary>
    string IdentityDescriptorName { get; }

    /// <summary>
    /// Denotes types of identities: Users, Groups etc.
    /// </summary>
    IdentityType IdentityDescriptorType { get; }
  }

  /// <summary>
  /// Represents information about user identity
  /// </summary>
  public struct UserIdentityDescriptor : IIdentityDescriptor
  {
    public UserIdentityDescriptor(object id, string name)
    {
      IdentityDescriptorID   = id;
      IdentityDescriptorName = name;
    }

    public object IdentityDescriptorID { get; }
    public string IdentityDescriptorName { get; }
    public IdentityType IdentityDescriptorType { get { return IdentityType.User; } }
  }

  /// <summary>
  /// Provides base user functionality. Particular security manager implementations may return users derived from this class
  /// </summary>
  [Serializable]
  public class User : IIdentityDescriptor, IIdentity, IPrincipal
  {
    #region Static
      private static readonly User s_FakeUserInstance = new User(BlankCredentials.Instance,
                                          new AuthenticationToken(),
                                          UserStatus.Invalid,
                                            "John Doe",
                                            "Fake user",
                                            Rights.None,
                                            Ambient.UTCNow);

      /// <summary>
      /// Returns default instance of the fake user that has no rights
      /// </summary>
      public static User Fake => s_FakeUserInstance;
    #endregion

    #region .ctor
      private User() { } //for quicker serialization

      public User(Credentials credentials,
                  AuthenticationToken token,
                  UserStatus status,
                  string name,
                  string descr,
                  Rights rights,
                  DateTime? utcNow = null)
      {
          m_Credentials = credentials;
          m_AuthenticationToken = token;
          m_Status = status;
          m_Name = name;
          m_Description = descr;
          m_Rights = rights;
          m_StatusTimeStampUTC = utcNow ?? Ambient.UTCNow;
      }

      public User(Credentials credentials,
                  AuthenticationToken token,
                  string name,
                  Rights rights, DateTime? utcNow = null) : this(credentials, token, UserStatus.User, name, null, rights, utcNow)
      {

      }
    #endregion

    #region Fields

      private DateTime m_StatusTimeStampUTC;

      private Credentials m_Credentials;
      private AuthenticationToken m_AuthenticationToken;

      private UserStatus m_Status;
      private string m_Name;

      private string m_Description;

      [NonSerialized]//Important, rights are NOT serializable
      private Rights m_Rights;

    #endregion

    #region Properties

      /// <summary>
      /// Captures timestamp when this user was set to current status (created/set rights)
      /// Security managers may elect to re-fetch user rights after some period
      /// </summary>
      public DateTime StatusTimeStampUTC => m_StatusTimeStampUTC;

      public Credentials Credentials => m_Credentials ?? BlankCredentials.Instance;

      public AuthenticationToken AuthToken => m_AuthenticationToken;

      public string Name => m_Name ?? string.Empty;

      public string Description => m_Description ?? string.Empty;

      public UserStatus Status => m_Status;

      /// <summary>
      /// Returns data bag that contains user rights. This is a framework-only internal property
      ///  which should not be used by application developers. This bag may get populated fully-or-partially
      ///   by ISecurityManager implementation. Use User[permission] indexer or Application.SecurityManager.Authorize()
      ///    to obtain AccessLevel
      /// </summary>
      public Rights Rights => m_Rights;

    #endregion

    #region Public

      /// <summary>
      /// Makes user invalid
      /// </summary>
      public void Invalidate()
      {
        m_Status = UserStatus.Invalid;
      }

      /// <summary>
      /// Framework-internal. Do not call
      /// </summary>
      public void ___update_status(
                    UserStatus status,
                    string name,
                    string descr,
                    Rights rights,
                    DateTime utcNow)
      {
          if (object.ReferenceEquals(this, s_FakeUserInstance)) return;//Fake user is immutable
          m_Status = status;
          m_Name = name;
          m_Description = descr;
          m_Rights = rights;
          m_StatusTimeStampUTC = utcNow;
      }

      public override string ToString()
      {
        return "[{0}]{1},{2}".Args(Status, Name, Description);
      }

    #endregion

    #region IIdentity Members

      /// <summary>
      /// Implementation of IIdentity interface
      /// </summary>
      public string AuthenticationType
      {
        get { return m_AuthenticationToken.Realm ?? string.Empty; }
      }

      /// <summary>
      /// Implementation of IIdentity interface. Checks to see if user is not in invalid status
      /// </summary>
      public bool IsAuthenticated
      {
        get { return m_Status != UserStatus.Invalid; }
      }

    #endregion

    #region IPrincipal Members

      /// <summary>
      /// Implementation of IPrincipal interface
      /// </summary>
      public IIdentity Identity
      {
        get { return this; }
      }

      /// <summary>
      /// Determines whether the current principal belongs to the specified role.
      /// This method implements IPrincipal and has little application in Azos framework context
      /// as Azos permissions are more granular than just boolean. This method really checks user kind (User/Admin/Sys).
      /// Confusion comes from the fact that what Microsoft calls role really is just a single named permission -
      ///  a role is a named permission set in Azos.
      /// </summary>
      public bool IsInRole(string role)
      {
        return m_Status.ToString().ToUpperInvariant() == role.ToUpperInvariant();
      }

    #endregion

  #region IIdentityDescriptor

    public virtual string IdentityDescriptorName { get { return this.Name; } }
    public virtual object IdentityDescriptorID { get { return AuthToken; } }
    public virtual IdentityType IdentityDescriptorType { get { return IdentityType.User; } }

  #endregion

  }
}
