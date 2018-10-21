
namespace Azos.Web.Pay.Braintree
{
  public class BraintreeSession : PaySession
  {
    public BraintreeSession(BraintreeSystem system, BraintreeConnectionParameters cParams, IPaySessionContext context = null)
      : base(system, cParams, context)
    {
    }

    public object ClientToken { get { return PaySystem.GenerateClientToken(this); } }

    protected new BraintreeSystem PaySystem { get { return base.PaySystem as BraintreeSystem; } }

    public string MerchantID
    {
      get
      {
        if (!IsValid) return string.Empty;
        var credentials = User.Credentials as BraintreeCredentials;
        if (credentials == null) return string.Empty;
        return credentials.MerchantID;
      }
    }
  }
}