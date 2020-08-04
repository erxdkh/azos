﻿using System;
using System.Collections.Generic;
using System.Text;
using Azos.Data;
using Azos.Data.Business;
using Azos.Scripting;
using Azos.Serialization.JSON;

namespace Azos.Tests.Nub.DataAccess
{
  [Runnable]
  public class EntityIdTests
  {
    [Run]
    public void NotAssigned()
    {
      var v = default(EntityId);
      Aver.IsFalse(v.IsAssigned);
      Aver.IsFalse(v.CheckRequired(null));
    }

    [Run]
    public void HasCodeEquals()
    {
      var v1 = new EntityId(Atom.Encode("sys"), Atom.Encode("tp1"), "address");
      var v2 = new EntityId(Atom.Encode("sys"), Atom.Encode("tp2"), "address");
      var v3 = new EntityId(Atom.Encode("sys"), Atom.Encode("tp1"), "address2");
      var v4 = new EntityId(Atom.Encode("sYs"), Atom.Encode("tp1"), "address");
      var v5 = new EntityId(Atom.Encode("sys"), Atom.Encode("tp1"), "address");

      var v6 = new EntityId(Atom.Encode("sys"), Atom.ZERO, "address");
      var v7 = new EntityId(Atom.Encode("sys"), Atom.ZERO, "address");
      var v8 = new EntityId(Atom.Encode("sys"), Atom.ZERO, "address-1");


      Aver.AreObjectsEqual(v1, v5);
      Aver.AreObjectsEqual(v5, v1);
      Aver.AreObjectsNotEqual(v1, v2);
      Aver.AreObjectsNotEqual(v1, v3);
      Aver.AreObjectsNotEqual(v1, v4);
      Aver.AreObjectsNotEqual(v4, v5);

      Aver.AreObjectsEqual(v6, v7);
      Aver.AreObjectsNotEqual(v7, v8);


      Aver.AreEqual(v1.GetHashCode(), v5.GetHashCode());
      Aver.AreEqual(v5.GetHashCode(), v1.GetHashCode());
      Aver.AreNotEqual(v1.GetHashCode(), v2.GetHashCode());
      Aver.AreNotEqual(v1.GetHashCode(), v3.GetHashCode());
      Aver.AreNotEqual(v1.GetHashCode(), v4.GetHashCode());
      Aver.AreNotEqual(v4.GetHashCode(), v5.GetHashCode());
      Aver.AreEqual(v6.GetHashCode(), v7.GetHashCode());
      Aver.AreNotEqual(v7.GetHashCode(), v8.GetHashCode());

      Aver.AreEqual   (v1.GetDistributedStableHash(), v5.GetDistributedStableHash());
      Aver.AreEqual   (v5.GetDistributedStableHash(), v1.GetDistributedStableHash());
      Aver.AreNotEqual(v1.GetDistributedStableHash(), v2.GetDistributedStableHash());
      Aver.AreNotEqual(v1.GetDistributedStableHash(), v3.GetDistributedStableHash());
      Aver.AreNotEqual(v1.GetDistributedStableHash(), v4.GetDistributedStableHash());
      Aver.AreNotEqual(v4.GetDistributedStableHash(), v5.GetDistributedStableHash());
      Aver.AreEqual   (v6.GetDistributedStableHash(), v7.GetDistributedStableHash());
      Aver.AreNotEqual(v7.GetDistributedStableHash(), v8.GetDistributedStableHash());
    }

    [Run]
    public void TryParse00()
    {
      Aver.IsTrue(EntityId.TryParse(null, out var v));
      Aver.IsFalse(v.IsAssigned);

      Aver.IsTrue(EntityId.TryParse("", out v));
      Aver.IsFalse(v.IsAssigned);

      Aver.IsTrue(EntityId.TryParse("               ", out v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse01()
    {
      Aver.IsTrue(EntityId.TryParse("a@b::adr1", out var v));
      Aver.IsTrue(v.IsAssigned);
      Aver.AreEqual(Atom.Encode("a"), v.Type);
      Aver.AreEqual(Atom.Encode("b"), v.System);
      Aver.AreEqual("adr1", v.Address);
    }

    [Run]
    public void TryParse02()
    {
      Aver.IsTrue(EntityId.TryParse("b::adr1", out var v));
      Aver.IsTrue(v.IsAssigned);
      Aver.AreEqual(Atom.ZERO, v.Type);
      Aver.AreEqual(Atom.Encode("b"), v.System);
      Aver.AreEqual("adr1", v.Address);
    }

    [Run]
    public void TryParse03()
    {
      Aver.IsTrue(EntityId.TryParse("system01::@://long-address::-string", out var v));
      Aver.IsTrue(v.IsAssigned);
      Aver.AreEqual(Atom.ZERO, v.Type);
      Aver.AreEqual(Atom.Encode("system01"), v.System);
      Aver.AreEqual("@://long-address::-string", v.Address);
    }

    [Run]
    public void TryParse04()
    {
      Aver.IsFalse(EntityId.TryParse("::abc", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse05()
    {
      Aver.IsFalse(EntityId.TryParse("aa::", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse06()
    {
      Aver.IsFalse(EntityId.TryParse("bbb@aa::", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse07()
    {
      Aver.IsFalse(EntityId.TryParse("bbb@::", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse08()
    {
      Aver.IsFalse(EntityId.TryParse("aaa::             ", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse09()
    {
      Aver.IsFalse(EntityId.TryParse("         @aaa::gggg", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse10()
    {
      Aver.IsFalse(EntityId.TryParse("@", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse11()
    {
      Aver.IsFalse(EntityId.TryParse("a b@dd::aaa", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse12()
    {
      Aver.IsFalse(EntityId.TryParse("ab@d d::aaa", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse13()
    {
      Aver.IsFalse(EntityId.TryParse("ab@d*d::aaa", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse14()
    {
      Aver.IsFalse(EntityId.TryParse("ab@dd::                             ", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void TryParse15()
    {
      Aver.IsFalse(EntityId.TryParse("::", out var v));
      Aver.IsFalse(v.IsAssigned);
    }

    [Run]
    public void JSON01()
    {
      var v = EntityId.Parse("abc@def::12:15:178");
      var obj = new {a =  v};
      var json = obj.ToJson();
      json.See();
      var map = json.JsonToDataObject() as JsonDataMap;
      var got = EntityId.Parse( map["a"].ToString() );

      Aver.AreEqual(v, got);
    }

    public class Doc1 : TypedDoc
    {
      [Field] public EntityId V1{  get; set;}
      [Field] public EntityId? V2 { get; set; }
    }

    [Run]
    public void JSON02()
    {
      var d1 = new  Doc1{ V1 = EntityId.Parse("abc@def::12:15:178") };
      var json = d1.ToJson(JsonWritingOptions.PrettyPrintRowsAsMap);
      json.See();
      var got = JsonReader.ToDoc<Doc1>(json);
      got.See();

      Aver.AreEqual(d1.V1, got.V1);
      Aver.IsNull(got.V2);
    }

    [Run]
    public void JSON03()
    {
      var d1 = new Doc1 { V1 = EntityId.Parse("abc@def::12:15:178"), V2 = EntityId.Parse("lic::i9973od") };
      var json = d1.ToJson(JsonWritingOptions.PrettyPrintRowsAsMap);
      json.See();
      var got = JsonReader.ToDoc<Doc1>(json);
      got.See();

      Aver.AreEqual(d1.V1, got.V1);
      Aver.AreEqual(d1.V2, got.V2);
    }

    [Run]
    public void JSON04()
    {
      var d1 = new Doc1 { V1 = EntityId.Parse("abc@def::abc@def::456"), V2 = EntityId.Parse("lic:::::") };
      var json = d1.ToJson(JsonWritingOptions.PrettyPrintRowsAsMap);
      json.See();
      var got = JsonReader.ToDoc<Doc1>(json);
      got.See();

      Aver.AreEqual("abc@def::456", got.V1.Address);
      Aver.AreEqual(":::", got.V2.Value.Address);
    }
  }
}
