    Title: ReadOnlyCollection&lt;T&gt; and .AsReadOnly()
    Date: 2009-08-08T20:17:00
    Tags: .NET, C#, software engineering
<!-- more -->

<p>I stumbled across a collection type the other day that was implemented with .NET 2.0.  This, like many other things in 2.0 slipped past me completely until now.  This collection type offers a solution to a common problem; How do you expose the collections in your class but prevent the consumer from modifying them? This is a "problem" with all reference types really, but often it is desirable to expose your collections to consumers.  To illustrate this problem consider the following class:</p>
<p></p>
<p>
<pre class='brush: csharp'>
public class John
    {
        private readonly List&lt;string&gt; _volvos = new List&lt;string&gt;();

        public List&lt;string&gt; Volvos
        {
            get { return _volvos; }
        }

        public John()
        {
            _volvos.Add("Volvo A");
            _volvos.Add("Volvo B");
        }
    }

```

</p>
<p></p>
<p>This is about the best you can do. However, even with a readonly collection and using a readonly property, the consumer still ends up with the reference to the collection, and thus can modify it at will. The readonly prevents the consumer from setting the reference to null or otherwise pointing it at something else, but correctly does bugger all other than that. This program illustrates the behavior:</p>
<p> 
<pre class='brush: csharp'>
class Program
    {
        static void Main(string[] args)
        {
            John john = new John();
            
            Console.WriteLine("Contents of collection before:");
            john.Volvos.ForEach(Console.WriteLine);

            john.Volvos.Add("Volvic!");

            Console.WriteLine("Contents of collection after:");
            john.Volvos.ForEach(Console.WriteLine);

            Console.Read();
        }
    }

```

</p>
<p></p>
<p>This program compiles and will indeed allow the modifcation of the collection. An attempt here to set john.Volvos = null would cause a compile time error.</p>
<p>So, the new collection, ReadOnlyCollection&lt;T&gt; is simply a wrapper around an existing collection. it is not a collection in itself at all. It is indeed somewhat strange, as it implements the IList&lt;T&gt; interface but obviously it can't fulfil most of the contract... This is a bit of an odd design, but it has to be this way as interfaces like IList are not split into read/write interfaces, so in order to use this collection in place of an existing one, it still needs to "Implement" all of IList, however only directly exposes Contains(), CopyTo(), and the Count property and hides the rest by implementing them explicitly.</p>
<p>Anyway, you can create one of these readonly wrappers in two ways, one is to create the collection instance and pass an IList instance as the parameter in the constructor like so:</p>
<p></p>
<p><pre class='brush: csharp'>ReadOnlyCollection&lt;string&gt; readonlyVolvos = new ReadOnlyCollection&lt;string&gt;(_volvos);
```
</p>
<p></p>
<p>Or you can simply call the .AsReadOnlyMethod on the collection itself and it will create a wrapper.</p>
<p></p>
<p><pre class='brush: csharp'>ReadOnlyCollection&lt;string&gt; readonlyVolvos = _volvos.AsReadOnly();
```
</p>
<p></p>
<p>I must admit, I haven't looked at the IL of these, but I believe they both work in the same way, initialising a new instance each time.. that wouldn't be ideal if the list was to be accessed a lot. Therefore you could simply store a wrapper that you create on intialisation, something like this:</p>
<p>
<pre class='brush: csharp'>
public class John
    {
        private readonly List&lt;string&gt; _volvos = new List&lt;string&gt;();
        private readonly ReadOnlyCollection&lt;string&gt; _readonlyVolvos;
    
        public ReadOnlyCollection&lt;string&gt; Volvos
        {
            get { return _readonlyVolvos; }
        }

        public John()
        {
            _readonlyVolvos = _volvos.AsReadOnly();
            _volvos.Add("Volvo A");
            _volvos.Add("Volvo B");
        }
    }

```
</p>
<p></p>
<p>What I've done in the past is to simply expose the collection as IEnumerable&lt;T&gt; and yield return the results, which has the rather large drawback that the consumer can't use it with anything that requires a IList. This looks like a nice way of doing it. The ReadOnlyContainer looks like it could solve the problem quite nicely. One problem though - this only works with IList! This means you can't use it with dictionaries, stacks or queues :/ I am looking at my own implementation that will allow any type of collection to be used. I have seen several read-only-dictionary implementations which are pretty heavyweight, and I'm fairly sure i can use some trickery to do this for all collections with a little code... watch this space.</p>