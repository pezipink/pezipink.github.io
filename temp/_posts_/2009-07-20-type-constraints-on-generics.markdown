    Title: Type constraints on generics
    Date: 2009-07-20T22:54:00
    Tags: .NET
<!-- more -->

<p>Time to start using this a bit more for general ramblings on things I've been up to. Have 0 time for electronics, robotics and the like at the moment.<br /><br />I have been working on the game server a lot recently, which was originally written in .NET 1.1. Unfortunately it still has a lot of this old code and one thing that as been annoying me in particular is the serialization mechanism. .NET 1.1 did not have any support for generics, but sine we've upgraded to .NET 2.0 (ages ago) this stuff hasn't been upgraded. This is massively annoying given that the old rubbishy ArrayList is the only thing it knows about for collections. Why it wasn't implemented with ICollection or something I really have no idea.<br /><br />These days on the server, I am busy throwing around type-safe strongly typed Lists of objects which are far superior to the (obsolete - hell List&lt;Object&gt; over ArrayList) ArrayList. This means everytime I want to serialize these I have to make a copy of the List into new ArrayList and then pass that to the serialization class. Fail.<br /><br />Example. Given a list is declared as a member varaible for some Item or other like so:</p>
<p></p>
```fsharp
private List&lt;SomeItem&gt; m_SomeItemList =  new List&lt;SomeItem&gt;();
public override void Serialize(GenericWriter writer)
{
  base.Serialize(writer);

  writer.Write(0);  //version

  writer.WriteItemList(new ArrayList(m_SomeItemList ));  // FAIL<br />
}
```

<p>Then in the serialize method you have to do this:</p>
<p><br />Rubbish - totally unnecessary and scary copy of the collection to an inferior type. Our serialization class has three methods for writing collections of specific types - Items, Mobiles and Guilds. In effect these are all the same they just have slightly different logic that works on the different types. It doesn't STOP you passing totally the wrong data, mixtures of data or anything of the sort though. Something like Item is only really a base class that has tons of other classes inheriting from it, and others from those - so this problem is obviously suited to generics. The problem here is that depending on the item type some different logic is applied - this won't work then with generics because the T isn't known about at compile time and thus you can't access the type's specific fields/methods/etc. Enter type constraints on generics. These allow you to tell the compiler to expect the T to be at LEAST of a certain type, like so:<br /><br /></p>
```fsharp
public abstract void WriteItemList&lt;T&gt;(List&lt;T&gt; list) where T : Item;
```

<p><br />Now when this method is implemented in the serilializers, the compiler will know to expect a Item of some kind even if it has several other parent classes before Item in the inheritance tree, thus allowing us access to the properties and methods of the Item class directly. There are other bits you can add to these constrains like New which will ensure a class is passed in which is capable of having a constructor called.<br /><br />This principle is also applied to the deserialization that also has methods to read these collections as serialized by the write methods. Before now I would have to iterate across the collection it returned and manually add each item into my strongly type list - waste. I have written new overloads for these methods defined as followed:</p>
```fsharp
public abstract List&lt;T&gt; ReadItemList&lt;T&gt;() where T : Item;
public abstract List&lt;T&gt; ReadMobileList&lt;T&gt;() where T : Mobile;       
public abstract List&lt;T&gt; ReadGuildList&lt;T&gt;() where T: BaseGuild;

public abstract void WriteItemList&lt;T&gt;(List&lt;T&gt; list) where T : Item;
public abstract void WriteItemList&lt;T&gt;(List&lt;T&gt; list, bool tidy) where T : Item;
public abstract void WriteMobileList&lt;T&gt;(List&lt;T&gt; list) where T : Mobile;
public abstract void WriteMobileList&lt;T&gt;(List&lt;T&gt; list, bool tidy) where T : Mobile;
public abstract void WriteGuildList&lt;T&gt;(List&lt;T&gt; list) where T : BaseGuild;
public abstract void WriteGuildList&lt;T&gt;(List&lt;T&gt; list, bool tidy) where T : BaseGuild;
```

<p><br /><br />Now we can simply read and write strongly typed lists of stuff with no unnecessary copying of collections, loss of type safety and any other such rubbishness. Example of use now :<br /><br /></p>
```fsharp
writer.WriteItemList&lt;SomeItemt&gt;(m_SomeItems);

m_SomeItems = reader.ReadItemList&lt;SomeItem&gt;();
```

<p><br /><br /><br /></p>