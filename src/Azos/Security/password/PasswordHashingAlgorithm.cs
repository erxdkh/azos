/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;

using Azos.Apps;
using Azos.Conf;
using Azos.Instrumentation;

namespace Azos.Security
{
  /// <summary>
  /// Marker interface for options used in password hashing functionality
  /// </summary>
  public interface IPasswordHashingOptions{ }

  /// <summary>
  /// Represents an abstraction of password algorithm that performs hashing and verification of passwords supplied as SecureBuffer
  /// </summary>
  public abstract class PasswordHashingAlgorithm : DaemonWithInstrumentation<IPasswordManagerImplementation>, Collections.INamed
  {
    #region .ctor
    protected PasswordHashingAlgorithm(IPasswordManagerImplementation director, string name) : base(director)
    {
      this.Name = name;
      m_StrengthLevel = PasswordStrengthLevel.Normal;
    }
    #endregion

    #region Fields

    [Config("$default|$is-default")]
    private bool m_IsDefault;

    [Config(Default = PasswordStrengthLevel.Normal)]
    private PasswordStrengthLevel m_StrengthLevel;

    private bool m_InstrumentationEnabled;

    #endregion

    #region Properties
    public override string ComponentLogTopic => CoreConsts.SECURITY_TOPIC;

    [Config(Default = false)]
    [ExternalParameter(CoreConsts.EXT_PARAM_GROUP_INSTRUMENTATION, CoreConsts.EXT_PARAM_GROUP_PAY)]
    public override bool InstrumentationEnabled
    {
      get { return m_InstrumentationEnabled; }
      set { m_InstrumentationEnabled = value; }
    }

    public bool IsDefault => m_IsDefault;
    public PasswordStrengthLevel StrengthLevel => m_StrengthLevel;

    #endregion

    #region Public

    public virtual bool Match(PasswordFamily family) => true;

    public HashedPassword ComputeHash(PasswordFamily family, SecureBuffer password)
    {
      if (password == null)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "PasswordManager.ComputeHash(password==null)");
      if (!password.IsSealed)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "PasswordManager.ComputeHash(!password.IsSealed)");

      CheckDaemonActive();

      return DoComputeHash(family, password);
    }

    public bool Verify(SecureBuffer password, HashedPassword hash, out bool needRehash)
    {
      if (password == null || hash == null)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "PasswordManager.Verify((password|hash)==null)");
      if (!password.IsSealed)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "PasswordManager.Verify(!password.IsSealed)");

      needRehash = false;
      if (!Running)
        return false;

      return DoVerify(password, hash, out needRehash);
    }

    /// <summary>
    /// Returns true if two hashes are equal in their content (passwords match 100%).
    /// WARNING: This function must use length-constant time comparison without
    /// revealing partial correctness via its timing. See `SlowEquals()` cryptography topic
    /// See: https://stackoverflow.com/questions/21100985/why-is-the-slowequals-function-important-to-compare-hashed-passwords
    /// Use HashedPassword.AreStringsLengthConstantTimeEqual(a,b)
    /// </summary>
    public bool AreEquivalent(HashedPassword hash, HashedPassword rehash)
    {
      if (hash == null || rehash == null) return false;
      if (!hash.AlgoName.EqualsOrdIgnoreCase(rehash.AlgoName)) return false;

      //The function below
      return DoAreEquivalent(hash, rehash);
    }

    #endregion

    #region Protected

    protected abstract HashedPassword DoComputeHash(PasswordFamily family, SecureBuffer password);
    protected abstract bool DoVerify(SecureBuffer password, HashedPassword hash, out bool needRehash);

    /// <summary>
    /// WARNING: This function must use length-constant time comparison without
    /// revealing partial correctness via its timing. See `SlowEquals()` cryptography topic
    /// See: https://stackoverflow.com/questions/21100985/why-is-the-slowequals-function-important-to-compare-hashed-passwords
    /// Use HasshedPassword.AreStringsLengthConstantTimeEqual(a,b)
    /// </summary>
    protected abstract bool DoAreEquivalent(HashedPassword hash, HashedPassword rehash);

    #endregion
  }


  public abstract class PasswordHashingAlgorithm<TOptions> : PasswordHashingAlgorithm where TOptions : IPasswordHashingOptions
  {
    public PasswordHashingAlgorithm(IPasswordManagerImplementation director, string name) : base(director, name)
    {}

    public HashedPassword ComputeHash(PasswordFamily family, SecureBuffer password, TOptions options)
    {
      if (password == null)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "PasswordManager.ComputeHash(password==null)");
      if (!password.IsSealed)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "PasswordManager.ComputeHash(!password.IsSealed)");

      CheckDaemonActive();

      return DoComputeHash(family, password, options);
    }

    public TOptions ExtractPasswordHashingOptions(HashedPassword hash, out bool needRehash)
    {
      if (hash == null)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "PasswordHashingAlgorithm.ExtractPasswordHashingOptions(hash==null)");
      if (!hash.AlgoName.EqualsOrdSenseCase(Name))
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "PasswordHashingAlgorithm.ExtractPasswordHashingOptions(hash[algo] invalid)");

      return DoExtractPasswordHashingOptions(hash, out needRehash);
    }

    protected override HashedPassword DoComputeHash(PasswordFamily family, SecureBuffer password)
     => DoComputeHash(family, password, DefaultPasswordHashingOptions);

    protected override bool DoVerify(SecureBuffer password, HashedPassword hash, out bool needRehash)
    {
      var options = ExtractPasswordHashingOptions(hash, out needRehash);
      var rehash = ComputeHash(hash.Family, password, options);
      return AreEquivalent(hash, rehash);//this is done in length-constant time
    }

    protected abstract HashedPassword DoComputeHash(PasswordFamily family, SecureBuffer password, TOptions options);
    protected abstract TOptions DefaultPasswordHashingOptions { get; }
    protected abstract TOptions DoExtractPasswordHashingOptions(HashedPassword hash, out bool needRehash);
  }
}
