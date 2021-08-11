﻿// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Miscellaneous
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// New WordDelimiterFilter tests... most of the tests are in ConvertedLegacyTest
    /// TODO: should explicitly test things like protWords and not rely on
    /// the factory tests in Solr.
    /// </summary>
    public class TestWordDelimiterFilter : BaseTokenStreamTestCase
    {
        // public void TestPerformance() throws IOException {
        //  String s = "now is the time-for all good men to come to-the aid of their country.";
        //  Token tok = new Token();
        //  long start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
        //  int ret=0;
        //  for (int i=0; i<1000000; i++) {
        //    StringReader r = new StringReader(s);
        //    TokenStream ts = new WhitespaceTokenizer(r);
        //    ts = new WordDelimiterFilter(ts, 1,1,1,1,0);
        // 
        //    while (ts.next(tok) != null) ret++;
        //  }
        // 
        //  System.out.println("ret="+ret+" time="+(J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond-start)); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
        // }

        [Test]
        public virtual void TestOffsets()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            // test that subwords and catenated subwords have
            // the correct offsets.
            WordDelimiterFilter wdf = new WordDelimiterFilter(TEST_VERSION_CURRENT, new SingleTokenTokenStream(new Token("foo-bar", 5, 12)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "foo", "foobar", "bar" }, new int[] { 5, 5, 9 }, new int[] { 8, 12, 12 });

            wdf = new WordDelimiterFilter(TEST_VERSION_CURRENT, new SingleTokenTokenStream(new Token("foo-bar", 5, 6)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "foo", "bar", "foobar" }, new int[] { 5, 5, 5 }, new int[] { 6, 6, 6 });
        }

        [Test]
        public virtual void TestOffsetChange()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            WordDelimiterFilter wdf = new WordDelimiterFilter(TEST_VERSION_CURRENT, new SingleTokenTokenStream(new Token("übelkeit)", 7, 16)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "übelkeit" }, new int[] { 7 }, new int[] { 15 });
        }

        [Test]
        public virtual void TestOffsetChange2()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            WordDelimiterFilter wdf = new WordDelimiterFilter(TEST_VERSION_CURRENT, new SingleTokenTokenStream(new Token("(übelkeit", 7, 17)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "übelkeit" }, new int[] { 8 }, new int[] { 17 });
        }

        [Test]
        public virtual void TestOffsetChange3()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            WordDelimiterFilter wdf = new WordDelimiterFilter(TEST_VERSION_CURRENT, new SingleTokenTokenStream(new Token("(übelkeit", 7, 16)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "übelkeit" }, new int[] { 8 }, new int[] { 16 });
        }

        [Test]
        public virtual void TestOffsetChange4()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            WordDelimiterFilter wdf = new WordDelimiterFilter(TEST_VERSION_CURRENT, new SingleTokenTokenStream(new Token("(foo,bar)", 7, 16)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "foo", "foobar", "bar" }, new int[] { 8, 8, 12 }, new int[] { 11, 15, 15 });
        }

        public virtual void doSplit(string input, params string[] output)
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            WordDelimiterFilter wdf = new WordDelimiterFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader(input), MockTokenizer.KEYWORD, false), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, output);
        }

        [Test]
        public virtual void TestSplits()
        {
            doSplit("basic-split", "basic", "split");
            doSplit("camelCase", "camel", "Case");

            // non-space marking symbol shouldn't cause split
            // this is an example in Thai    
            doSplit("\u0e1a\u0e49\u0e32\u0e19", "\u0e1a\u0e49\u0e32\u0e19");
            // possessive followed by delimiter
            doSplit("test's'", "test");

            // some russian upper and lowercase
            doSplit("Роберт", "Роберт");
            // now cause a split (russian camelCase)
            doSplit("РобЕрт", "Роб", "Ерт");

            // a composed titlecase character, don't split
            doSplit("aǅungla", "aǅungla");

            // a modifier letter, don't split
            doSplit("ســـــــــــــــــلام", "ســـــــــــــــــلام");

            // enclosing mark, don't split
            doSplit("test⃝", "test⃝");

            // combining spacing mark (the virama), don't split
            doSplit("हिन्दी", "हिन्दी");

            // don't split non-ascii digits
            doSplit("١٢٣٤", "١٢٣٤");

            // don't split supplementaries into unpaired surrogates
            doSplit("𠀀𠀀", "𠀀𠀀");
        }

        public virtual void doSplitPossessive(int stemPossessive, string input, params string[] output)
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS;
            flags |= (stemPossessive == 1) ? WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE : 0;
            WordDelimiterFilter wdf = new WordDelimiterFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader(input), MockTokenizer.KEYWORD, false), flags, null);

            AssertTokenStreamContents(wdf, output);
        }

        /*
         * Test option that allows disabling the special "'s" stemming, instead treating the single quote like other delimiters. 
         */
        [Test]
        public virtual void TestPossessives()
        {
            doSplitPossessive(1, "ra's", "ra");
            doSplitPossessive(0, "ra's", "ra", "s");
        }

        /*
         * Set a large position increment gap of 10 if the token is "largegap" or "/"
         */
        private sealed class LargePosIncTokenFilter : TokenFilter
        {
            private readonly TestWordDelimiterFilter outerInstance;

            internal ICharTermAttribute termAtt;
            internal IPositionIncrementAttribute posIncAtt;

            internal LargePosIncTokenFilter(TestWordDelimiterFilter outerInstance, TokenStream input) : base(input)
            {
                this.outerInstance = outerInstance;
                this.termAtt = AddAttribute<ICharTermAttribute>();
                this.posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            public override bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
                    if (termAtt.ToString().Equals("largegap", StringComparison.Ordinal) || termAtt.ToString().Equals("/", StringComparison.Ordinal))
                    {
                        posIncAtt.PositionIncrement = 10;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [Test]
        public virtual void TestPositionIncrements()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;

            CharArraySet protWords = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "NUTCH" }, false);

            /* analyzer that uses whitespace + wdf */
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new WordDelimiterFilter(TEST_VERSION_CURRENT, tokenizer, flags, protWords));
            });

            /* in this case, works as expected. */
            AssertAnalyzesTo(a, "LUCENE / SOLR", new string[] { "LUCENE", "SOLR" }, new int[] { 0, 9 }, new int[] { 6, 13 }, new int[] { 1, 1 });

            /* only in this case, posInc of 2 ?! */
            AssertAnalyzesTo(a, "LUCENE / solR", new string[] { "LUCENE", "sol", "solR", "R" }, new int[] { 0, 9, 9, 12 }, new int[] { 6, 12, 13, 13 }, new int[] { 1, 1, 0, 1 });

            AssertAnalyzesTo(a, "LUCENE / NUTCH SOLR", new string[] { "LUCENE", "NUTCH", "SOLR" }, new int[] { 0, 9, 15 }, new int[] { 6, 14, 19 }, new int[] { 1, 1, 1 });

            /* analyzer that will consume tokens with large position increments */
            Analyzer a2 = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new WordDelimiterFilter(TEST_VERSION_CURRENT, new LargePosIncTokenFilter(this, tokenizer), flags, protWords));
            });

            /* increment of "largegap" is preserved */
            AssertAnalyzesTo(a2, "LUCENE largegap SOLR", new string[] { "LUCENE", "largegap", "SOLR" }, new int[] { 0, 7, 16 }, new int[] { 6, 15, 20 }, new int[] { 1, 10, 1 });

            /* the "/" had a position increment of 10, where did it go?!?!! */
            AssertAnalyzesTo(a2, "LUCENE / SOLR", new string[] { "LUCENE", "SOLR" }, new int[] { 0, 9 }, new int[] { 6, 13 }, new int[] { 1, 11 });

            /* in this case, the increment of 10 from the "/" is carried over */
            AssertAnalyzesTo(a2, "LUCENE / solR", new string[] { "LUCENE", "sol", "solR", "R" }, new int[] { 0, 9, 9, 12 }, new int[] { 6, 12, 13, 13 }, new int[] { 1, 11, 0, 1 });

            AssertAnalyzesTo(a2, "LUCENE / NUTCH SOLR", new string[] { "LUCENE", "NUTCH", "SOLR" }, new int[] { 0, 9, 15 }, new int[] { 6, 14, 19 }, new int[] { 1, 11, 1 });

            Analyzer a3 = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                StopFilter filter = new StopFilter(TEST_VERSION_CURRENT, tokenizer, StandardAnalyzer.STOP_WORDS_SET);
                return new TokenStreamComponents(tokenizer, new WordDelimiterFilter(TEST_VERSION_CURRENT, filter, flags, protWords));
            });

            AssertAnalyzesTo(a3, "lucene.solr", new string[] { "lucene", "lucenesolr", "solr" }, new int[] { 0, 0, 7 }, new int[] { 6, 11, 11 }, new int[] { 1, 0, 1 });

            /* the stopword should add a gap here */
            AssertAnalyzesTo(a3, "the lucene.solr", new string[] { "lucene", "lucenesolr", "solr" }, new int[] { 4, 4, 11 }, new int[] { 10, 15, 15 }, new int[] { 2, 0, 1 });
        }

        /// <summary>
        /// concat numbers + words + all </summary>
        [Test]
        public virtual void TestLotsOfConcatenating()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_WORDS 
                | WordDelimiterFlags.CATENATE_NUMBERS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;

            /* analyzer that uses whitespace + wdf */
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new WordDelimiterFilter(TEST_VERSION_CURRENT, tokenizer, flags, null));
            });

            AssertAnalyzesTo(a, "abc-def-123-456", new string[] { "abc", "abcdef", "abcdef123456", "def", "123", "123456", "456" }, new int[] { 0, 0, 0, 4, 8, 8, 12 }, new int[] { 3, 7, 15, 7, 11, 15, 15 }, new int[] { 1, 0, 0, 1, 1, 0, 1 });
        }

        /// <summary>
        /// concat numbers + words + all + preserve original </summary>
        [Test]
        public virtual void TestLotsOfConcatenating2()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.PRESERVE_ORIGINAL 
                | WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_WORDS 
                | WordDelimiterFlags.CATENATE_NUMBERS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;

            /* analyzer that uses whitespace + wdf */
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new WordDelimiterFilter(TEST_VERSION_CURRENT, tokenizer, flags, null));
            });

            AssertAnalyzesTo(a, "abc-def-123-456", new string[] { "abc-def-123-456", "abc", "abcdef", "abcdef123456", "def", "123", "123456", "456" }, new int[] { 0, 0, 0, 0, 4, 8, 8, 12 }, new int[] { 15, 3, 7, 15, 7, 11, 15, 15 }, new int[] { 1, 0, 0, 0, 1, 1, 0, 1 });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        public virtual void TestRandomStrings()
        {
            int numIterations = AtLeast(5);
            for (int i = 0; i < numIterations; i++)
            {
                WordDelimiterFlags flags = (WordDelimiterFlags)Random.Next(512);
                CharArraySet protectedWords;
                if (Random.nextBoolean())
                {
                    protectedWords = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "a", "b", "cd" }, false);
                }
                else
                {
                    protectedWords = null;
                }

                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                    return new TokenStreamComponents(tokenizer, new WordDelimiterFilter(TEST_VERSION_CURRENT, tokenizer, flags, protectedWords));
                });
                CheckRandomData(Random, a, 1000 * RandomMultiplier);
            }
        }

        /// <summary>
        /// blast some enormous random strings through the analyzer </summary>
        [Test]
        [Slow]
        public virtual void TestRandomHugeStrings()
        {
            int numIterations = AtLeast(5);
            for (int i = 0; i < numIterations; i++)
            {
                WordDelimiterFlags flags = (WordDelimiterFlags)Random.Next(512);
                CharArraySet protectedWords;
                if (Random.nextBoolean())
                {
                    protectedWords = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "a", "b", "cd" }, false);
                }
                else
                {
                    protectedWords = null;
                }

                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                    return new TokenStreamComponents(tokenizer, new WordDelimiterFilter(TEST_VERSION_CURRENT, tokenizer, flags, protectedWords));
                });
                CheckRandomData(Random, a, 100 * RandomMultiplier, 8192);
            }
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Random random = Random;
            for (int i = 0; i < 512; i++)
            {
                WordDelimiterFlags flags = (WordDelimiterFlags)i;
                CharArraySet protectedWords;
                if (random.nextBoolean())
                {
                    protectedWords = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "a", "b", "cd" }, false);
                }
                else
                {
                    protectedWords = null;
                }

                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new KeywordTokenizer(reader);
                    return new TokenStreamComponents(tokenizer, new WordDelimiterFilter(TEST_VERSION_CURRENT, tokenizer, flags, protectedWords));
                });
                // depending upon options, this thing may or may not preserve the empty term
                CheckAnalysisConsistency(random, a, random.nextBoolean(), "");
            }
        }
    }
}