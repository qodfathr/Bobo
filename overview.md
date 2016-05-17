# The Architecture of Daxat ESS 2005

The origin of Bobo is the Daxat Extensible Search Server 2005 (ESS), a monolithic indexing and query
server written in the Microsoft .NET Framework 1.0 (and migrated to 1.1 prior to final release).

ESS is comprised of the following major architectural areas:

1. A [B-Tree](https://en.wikipedia.org/wiki/B-tree) based [inverted word index](https://en.wikipedia.org/wiki/Inverted_index).
1. A query [lexical analyizer](https://en.wikipedia.org/wiki/Lexical_analysis)
1. A query execution engine
1. A scheduler
1. Several types of crawlers -- for the [web](https://en.wikipedia.org/wiki/Lexical_analysis), file systems, databases, etc.
1. An extensibilty API
1. A desktop administrative GUI

Other than the administration tool, every part of ESS runs in a single, monolithic, multi-threaded process as part of a large Windows service.
Limited isolation is achieved via .NET Framework [AppDomains](https://msdn.microsoft.com/en-us/library/system.appdomain%28v=vs.71%29.aspx).

## Inverted Word Index

At the heart of ESS is an inverted word index, hereafter refered to simply as the index. The index at the 
back of a textbook is an example of an inverted word index -- it's an alphabetical list of the important words
in the book, with a list of every page containing that word. e.g., imahine the following snippet from a
receipe book:

* hamburger: 12, 13, 19, 27, 35, 42
* hotdog: 13, 15, 22, 26, 35

Even with this small snippet, a lot can be determined:

* `hamburger` appears on 6 pages
* `hotdog` appears on 5 pages.
* They appear on two pages in common (13 and 35).
* The word `herb` is not in the book (or at least isn't considered to be important), since it's lexically between
`hamburger` and `hotdog`, and is not found between these two words in the index.

Moreover, as the list of pages is given in increasing order, certain optimizations can be used when trying
to compute some answers. For example, to find the pages common between `hamburger` and `hotdog` requires
finding the interestion between the two list of pages: {12, 13, 19, 27, 35, 42} ∩ {13, 15, 22, 26, 35}. However,
knowing that the two sets are monotonically increasing, the intersection can be computed in O(n) time with O(1) space.
Imagine having two pointers -- one at the start of the first set and one at the start of the second. Compare the two values.
If they are the same, then that value is in the intersection. Otherwise, advance the pointer with the lowest
value. Assume `a` is the pointer into the first set and `b` is the pointer into the second set. Then the
intersection can be found as follows:

* `a`: 12, `b`: 13. Advance `a`.
* `a`: 13, `b`: 13. Add 13 to the intersection. Advance `a` and `b`.
* `a`: 19, `b`: 15. Advance `b`.
* `a`: 19, `b`: 22. Advance `a`.
* `a`: 27, `b`: 22. Advance `b`.
* `a`: 27, `b`: 26. Advance `b`.
* `a`: 27, `b`: 35. Advance `a`.
* `a`: 35, `b`: 35. Add 35 to the intersection. Advance `a` and `b`.
* End of `b` reached; operation complete. Interestion: { 13, 35 }

ESS constucts a similar index. Whereas a book has a index which refers to pages in the book, ESS has an index
which refers to `nodes`. A `node` is simply a generic term for the level of granularity of the search results.
Common web search engines, such as Google and Bing, return individual web pages in their search results. So in
that example, a `node` would be a web page (or, more correctly, the URL of a web page, plus some meta data about
the web page). However, a `node` can be at any level of granularity. An index of a database table may map
individual rows from the table to `nodes`. An index of a file system may map individual files to `nodes`,
but it could even map individual lines of text files to `nodes` -- permitting search results for not
just the matching text files, but also the individual text lines of those text files.

The crawler which is indexing the data has the responsibility for deciding what constitues a `node`.

ESS then goes a level deeper -- instead of creating a index of words mapped to `nodes`, it actually maps
the words to `positions`. A `position` is a monotonically increasing count of the number of words seen. For example,
imagine a file system crawler has found two text files, and maps text files to `nodes`. Further, magine these
are the two text files, with there contents:

* A.TXT: The quick brown fox jumps over the lazy dog.
* B.TXT: A brown dog quickly becomes lazy.

The file `A.TXT` maps to `node 1` and it has 9 words, spanning 9 positions, ranging from position 1 to 9.
e.g. `The` is at position 1, `quick` is at position 2, `brown` is at position 3, and so on.

The file `B.TXT` maps to `node 2` and it has 6 words, spanning 6 positions, ranging from positions _10 to 15_.
The reason that the positions are 10 to 15 is because `node 1` consumed position 1 to 9.

The actual index looks similar to this:

* becomes: 14
* brown: 3, 11
* dog: 9, 12
* fox: 4
* jumps: 5
* lazy: 8, 15
* over: 6
* quick: 2
* quickly: 13
* the: 7
* A: 10
* The: 1

Moreover, we can think of the `nodes` as also being mapped into the index, relative to the starting and ennding
positions of the nodes:

* `_NODESTART`: 1, 10
* `_NODEEND`: 9, 15

Note that some words are seemingly in the index more than once -- for example, `the` and `The` both
appear seperately, although they only differ by case. Also, `quick` and `quickly` both appear,
although they are variations of the same word.

The reason for this is because an ESS index is not actually comprised of words but rather
it is comprised of `tokens`. And a `token` is any string (even with spaces), and 
every character must exactly match for two tokens to be the same. So `The` and `the`
are two distinctly different tokens.

The ESS search engine matches `tokens`. Therefore, a search for `The` is not the same
as a search for `the` or a search for `THE`. How this is handled in a sane manner for
the user will be described later.

The special `tokens` `_NODESTART` and `_NODEEND` are just two examples of special `tokens`
which ESS places into the index.

So imagine both `Fox` and `fox` were in the index. The normal expectation of a user is that
a search for `fox` would match against both instances. ESS has various ways of
handling this situation, but an important mechanism is the way `words` are
converted into `tokens`. Typically, when a `word` is added to the index, all of the
various `token` forms of the `word` are added to the index. But what's special
about this is that these `tokens` are added at the _exact same position_.

If you think of the index as just an array of individual words, you'll have a slightly
incorrect mental model. The reason why this is wrong is because an inverted
word index is a mapping of words to positions; it's not a positional array of words.

The following is a perfectly legal index:

* a: 1
* index: 1 
* is: 1
* legal: 1
* This: 1

Note that every single token has exactly one instance, and each one of those instances
is at position 1.

That's an extremely example. But now consider indexing the following node:

`node`: Résumés can be a useful personal marketing tool

Obviously, the index would contain the following `token`:

* Résumés: 1

However, it would be common for an end-user to search for `résumés` (lower-case), `resumes`
(no diacritical marks) or even resume (lower-case, non-diacrtic singular form). In ESS
this is addressed by indexing all of these `token` forms of the `word`:

* a: 4
* be: 3
* can: 2
* marketing: 7
* personal: 6
* resume: 1
* résumé: 1
* resumes: 1
* résumés: 1
* tool: 8
* useful: 5
* Resume: 1
* Résumé: 1
* Resumes: 1
* Résumés: 1

Note that a search for `RESUME` (all upper case) will not match anythjing in the index
(the reasoning for this will be explained later). Moreover, the tokens are generally
created by taking the word and _simplifying_ it, but not going in the other
direction. For example:

`node`: She waited for the show to resume

would only result in a single form of `resume` in the index.

Again, the reasoning for this will be covered in depth later.  