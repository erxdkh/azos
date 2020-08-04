/// <summary>
  /// Accesses remote GDID generation authority using web
  /// </summary>
  public sealed class GdidWebAuthorityAccessor : ApplicationComponent<IApplicationComponent>, IGdidAuthorityAccessor
  {
    public GdidWebAuthorityAccessor(IApplicationComponent dir) : base(dir)
    {

    }

    public override string ComponentLogTopic => "idgen";


    public async Task<GdidBlock> AllocateBlockAsync(string scopeName, string sequenceName, int blockSize, ulong? vicinity = 1152921504606846975)
    {
      var result = await ComponentDirector.CallServiceAsync(ESConsts.SVC_PATH_GDID, ++m_Shard,
       async (client) =>
       {
         var json = await client.PostAndGetJsonMapAsync("", new {scopeName, sequenceName, blockSize, vicinity});
         var block = new GdidBlock();
         JsonReader.ToDoc(block, json);
         return block;
       }
      );

      return result;
    }

    public void Configure(IConfigSectionNode node)
    {
      ConfigAttribute.Apply(this, node);
    }
  }
