    Title: Secret Santa challenge in D
    Date: 2015-02-08T00:01:00
    Tags: D

<p>I attended the first London meetup group for the D programming language last week with my friend David. Near the end we had a chance to try and solve a cool programming problem called the secret santa challenge.</p>
<p><em>disclaimer &ndash; I am a complete D 'lol newb' so there might be nicer stuff I could do with syntax etc! Also I have not yet fixed my syntax highlighter to include auto, mixin, assert, and others</em></p>
<!-- more -->

<h2>The Challenge</h2>
<p>The secret santa problem is deceptively simple. You are given a list of names like so.</p>
```d
enum string[] names = 
[ 
 "Vicky Pollard", 
 "Keith Pollard", 
 "Ian Smith", 
 "Dave Pollard", 
 "Maria Osawa", 
 "Mark Kelly", 
 "John Pollard", 
 "Sarah Kelly" 
];
```

<p>Each person must give and receive a present. A person may not give to themselves, nor may they give to anyone else in their family as determined by their last names.</p>
<p>Clearly, this problem can easily be brute forced, but that would not be at all efficient and would not work with large sets of data.</p>
<p>We did not have a lot of time for this, and initially David and I designed an way to try and ensure that when sorted, family members would not appear next to each other. This would allow us to then perform one pass over the array and assign a present to each neighbour with a special wrap-around case at the end. To achieve this we assigned weights to the members, and where there were more than one family member, the weight would increase by some arbitrarily large amount, to split them away from the others.</p>
<p>This algorithm worked for the data in question (which is not the same as above, I have added some to it) but I later realised that our algorithm had 2 fatal flaws.</p>
<ol>
<li>It relies on having some families with around the same amount of members in each. If you have one big family and several loners, it would dump most of the family members together at the end of the array</li>
<li>There was no way to guarantee the start and end members would not be from the same family</li>
</ol>
<p>It should be possible to still use a weighting approach if you calculated some ratios, but I thought there might be a simpler way.</p>
<h2>A revised algorithm</h2>
<p>After thinking about this on the train home from work a few days ago, and trying lots of different stuff in my head, I eventually realised the the real problem here is the biggest family. If x is how big a family is, and you sort the data by x, you can guarantee that by skipping ahead max(x) positions that you will never encounter another of the same family member (unless the problem is unsolvable).</p>
<p>D is a multi-paradigm language and with this algorithm I have used a mainly imperative style, allowing me to make the minimum amount of passes on the data.</p>
```d
struct person 
{ 
  string first; 
  string last; 
};

string distribute(const string[] names) 
{ 
 assert(names.length%2==0); 
 int[string] familyCount; 
 person[] people;

 // create people and max family count 
 int maxFamilyMembers = 0; 
 foreach(name;names) 
 { 
   auto split = split(name); 
   assert(split.length==2); 
   auto p = person(split[0],split[1]); 
   int count; 
   
   if(p.last in familyCount) 
   {
     count = ++familyCount[p.last]; 
   }
   else 
   {
     count = familyCount[p.last] = 1; 
   }
   
   if(count > maxFamilyMembers)
   { 
     maxFamilyMembers = count; 
   }
   
   people ~= p; 
 }

 // sort by family count 
 people.sort!((p1,p2) => familyCount[p1.last] > familyCount[p2.last]);

 string result; 
 // assign presents using max count as an offset 
 for(int i=0; i&lt;people.length; i++)
 { 
   auto source = people[i]; 
   auto dest = people[(i+maxFamilyMembers)%people.length]; 
   result ~= join([source.first," ",source.last," -> ",dest.first," ",dest.last,"\n"]); 
 }

 return result; 
}

int main(string[] argv) 
{ 
  auto results = distribute(names); 
  writeln(results); 
  readln(); 
  return 0; 
}
```

<p>You will see in the first section I mix the splitting of strings and creating people, counting the families and remembering the biggest family in the same pass. The data is then sorted by the family counts, and finally presents are distributed using the maxFamilyCount as an offset and modulus wrap-around logic.</p>
<p>Clearly, it isn&rsquo;t very random in its gift giving &ndash; and your hands are quite tied on this as you must make sure opposing family members cancel each other out before you start using loners to pair with trailing family members. However, you could quite easily random sort each family in place. It is also possible to go in opposite direction within the present-giving part, or even go forwards and backwards together at the same time. Whilst the pairs would be the same, the giver/receiver would be reversed in the other direction.</p>
<h2>Metaprogramming 1</h2>
<p>I am mainly interested in D for its awesome metaprogramming features, and given I know a list of names at compile time I should be able to turn this into a metaprogram and have the compiler work this out instead of it happening at runtime, right? How can I go about that? Brace yourselves, it is quite difficult.</p>
```d
int main(string[] argv) 
{ 
  static results = distribute(names); 
  writeln(results); 
  readln(); 
  return 0; 
}
```

<p>D has a rather nifty feature called <em>Compile Time Function Evaluation (CTFE)</em> which essentially lets you call any compatible function (with obvious constraints) either at runtime or compile time, and this is determined <em>at the call site</em>! So here I simply change <em>auto</em> to <em>static</em> and the compiler executed the same function during compilation and inlined the result for me. Pretty cool!</p>
<h2>Metaprogramming 2</h2>
<p>D has a very powerful constructed called a <em>mixin</em> which is different from the same-named construct in other languages. During compilation, D will interpret strings passed to a mixin, and assuming they are interpretable (the D compiler hosts a D interpreter!), the compiler will insert their definitions as if you had written them in the source code yourself. You can think T4 code gen on acid and steroids simultaneously. Whilst you might be thinking &ldquo;strings! surely not&rdquo;. Whilst the strings are a bit, will, stringy, they lead themselves to easy and interesting manipulation techniques (as opposed to splicing quoted code expressions or similar) and they are still evaluated at compile time, so you do something wrong, you will know about it up-front.</p>
<p>Let&rsquo; see how we can use a mixin to generate some code. I have decided that I would like a struct created for each person, and each struct will have a method <em>giveTo() </em>that will return a string with some text that shows which person they are giving to this year. To achieve this I will modify the distribute function to create the equivalent code strings instead:</p>
```d
string code; 
// assign presents using max count as an offset 
for(int i=0; i&lt;people.length; i++)
{ 
  auto source = people[i]; 
  auto dest = people[(i+maxFamilyMembers)%people.length]; 
  auto myName = join([source.first,source.last]); 
  auto theirName = join([dest.first," ",dest.last]); 
  code 
    ~= "struct " 
    ~ myName 
    ~ "{ string giveTo() { return \"This year I am giving to " 
    ~ theirName 
    ~ "\"; } " 
    ~ "}\n"; 
}

return code;
```

<p>Now in the main program I can write</p>
```d
int main(string[] argv) 
{ 
  mixin(distribute(names)); 
  auto vp = VickyPollard(); 
  writeln(vp.giveTo()); 
  readln(); 
  return 0;
}
```

<p>In this way I am using the CTFE to feed the mixin code-generation facility, which in turn verifies and inserts the code as if it had been written where the mixin statement is. This means I can now access the <em>VickyPollard</em> struct and call her <em>giveTo() </em>method. This is not dynamic &ndash; all statically checked at compile time! Combining these CTFE and mixins can be extremely powerful for generating code. D has another feature called <em>mixin templates</em> that let you define commonly used code blocks to insert into other places, which is more like the <em>mixin </em>from some other languages, except this is just a scope of code that can contain almost anything at all. (I think, I am still new to this!)</p>
<h2>One more thing</h2>
<p>D has single inheritance, interfaces and a really a long stream of other nice bits such as <em>Uniform Function Call Syntax (UFCS)</em> which is basically like automatic extension methods. What I would like to show in this post however is a feature called <em>alias this.</em> You can write a bunch of these in your classes / structs, the effect it has is that the compiler can then treat your class as if it was the same type of <em>member. </em>This allows almost multiple inheritance but is also very useful when combined with the metaprogramming features (although perhaps this will not be apparent from this silly example)</p>
<p>Lets say I would like each of my new structs to also be a person. I could use inheritance this for but lets not do that &ndash; instead I will create a static immutable person object inside each one than then <em>alias this </em>it. I have changed the string-code to output the equivalent of the following</p>
```d
struct VickyPollard 
{ 
  private static immutable me = person("Vicky","Pollard"); 
  string giveTo() {return "This year I am giving to Mark Kelly";} 
  alias me this; 
}
```

<p>and now I can fire up any of my mixin generated types and pass them anywhere that accepts <em>person</em> even though they are not a <em>person</em> at all.</p>
```d
void printPerson(person p) 
{ 
  writeln("this is person ", p.first," ", p.last); 
}

int main(string[] argv) 
{ 
  mixin(distribute(names)); 
  auto vp = VickyPollard(); 
  printPerson(vp); 
  writeln(vp.giveTo()); 
  readln(); 
  return 0; 
}
```

<p>Pretty cool! This post just explored a couple of the features in D I have played around with so far, but I am working on a roguelike game - the traditional thing I do when learning new languages - so I may write about other interesting language features as I make use of them.</p>