using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Diagnostics;
using NUnit.Framework;

namespace Lucene.Net.Analysis
{
    using Lucene.Net.Support;

    //using RandomizedContext = com.carrotsearch.randomizedtesting.RandomizedContext;
    using System.IO;

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

    using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;

    /// <summary>
    /// Tokenizer for testing.
    /// <p>
    /// this tokenizer is a replacement for <seealso cref="#WHITESPACE"/>, <seealso cref="#SIMPLE"/>, and <seealso cref="#KEYWORD"/>
    /// tokenizers. If you are writing a component such as a TokenFilter, its a great idea to test
    /// it wrapping this tokenizer instead for extra checks. this tokenizer has the following behavior:
    /// <ul>
    ///   <li>An internal state-machine is used for checking consumer consistency. These checks can
    ///       be disabled with <seealso cref="#setEnableChecks(boolean)"/>.
    ///   <li>For convenience, optionally lowercases terms that it outputs.
    /// </ul>
    /// </summary>
    public class MockTokenizer : Tokenizer
    {
        /// <summary>
        /// Acts Similar to WhitespaceTokenizer </summary>
        public static readonly CharacterRunAutomaton WHITESPACE = new CharacterRunAutomaton(new RegExp("[^ \t\r\n]+").ToAutomaton());

        /// <summary>
        /// Acts Similar to KeywordTokenizer.
        /// TODO: Keyword returns an "empty" token for an empty reader...
        /// </summary>
        public static readonly CharacterRunAutomaton KEYWORD = new CharacterRunAutomaton(new RegExp(".*").ToAutomaton());

        /// <summary>
        /// Acts like LetterTokenizer. </summary>
        // the ugly regex below is incomplete Unicode 5.2 [:Letter:]
        public static readonly CharacterRunAutomaton SIMPLE = new CharacterRunAutomaton(new RegExp("[A-Za-zªµºÀ-ÖØ-öø-ˁ一-鿌]+").ToAutomaton());

        private readonly CharacterRunAutomaton runAutomaton;
        private readonly bool lowerCase;
        private readonly int maxTokenLength;
        public static readonly int DEFAULT_MAX_TOKEN_LENGTH = int.MaxValue;
        private int state;

        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;
        internal int off = 0;

        // buffered state (previous codepoint and offset). we replay this once we
        // hit a reject state in case its permissible as the start of a new term.
        internal int bufferedCodePoint = -1; // -1 indicates empty buffer

        internal int bufferedOff = -1;

        // TODO: "register" with LuceneTestCase to ensure all streams are closed() ?
        // currently, we can only check that the lifecycle is correct if someone is reusing,
        // but not for "one-offs".
        new private enum State
        {
            SETREADER, // consumer set a reader input either via ctor or via reset(Reader)
            RESET, // consumer has called reset()
            INCREMENT, // consumer is consuming, has called IncrementToken() == true
            INCREMENT_FALSE, // consumer has called IncrementToken() which returned false
            END, // consumer has called end() to perform end of stream operations
            CLOSE // consumer has called close() to release any resources
        }

        private State streamState = State.CLOSE;
        private int lastOffset = 0; // only for asserting
        private bool enableChecks = true;

        // evil: but we don't change the behavior with this random, we only switch up how we read
        private readonly Random random = new Random(/*RandomizedContext.Current.Random.nextLong()*/);

        public MockTokenizer(AttributeFactory factory, TextReader input, CharacterRunAutomaton runAutomaton, bool lowerCase, int maxTokenLength)
            : base(factory, input)
        {
            this.runAutomaton = runAutomaton;
            this.lowerCase = lowerCase;
            this.state = runAutomaton.InitialState;
            this.streamState = State.SETREADER;
            this.maxTokenLength = maxTokenLength;
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        public MockTokenizer(TextReader input, CharacterRunAutomaton runAutomaton, bool lowerCase, int maxTokenLength)
            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, runAutomaton, lowerCase, maxTokenLength)
        {
        }

        public MockTokenizer(TextReader input, CharacterRunAutomaton runAutomaton, bool lowerCase)
            : this(input, runAutomaton, lowerCase, DEFAULT_MAX_TOKEN_LENGTH)
        {
        }

        /// <summary>
        /// Calls <seealso cref="#MockTokenizer(Reader, CharacterRunAutomaton, boolean) MockTokenizer(Reader, WHITESPACE, true)"/> </summary>
        public MockTokenizer(TextReader input)
            : this(input, WHITESPACE, true)
        {
        }

        public MockTokenizer(AttributeFactory factory, TextReader input, CharacterRunAutomaton runAutomaton, bool lowerCase)
            : this(factory, input, runAutomaton, lowerCase, DEFAULT_MAX_TOKEN_LENGTH)
        {
        }

        /// <summary>
        /// Calls <c>MockTokenizer(AttributeFactory, TextReader, WHITESPACE, true)</c>
        /// </summary>
        public MockTokenizer(AttributeFactory factory, TextReader input)
            : this(input, WHITESPACE, true)
        {
        }

        public sealed override bool IncrementToken()
        {
            //Debug.Assert(!EnableChecks_Renamed || (StreamState == State.RESET || StreamState == State.INCREMENT), "IncrementToken() called while in wrong state: " + StreamState);
            ClearAttributes();
            for (; ; )
            {
                int startOffset;
                int cp;
                if (bufferedCodePoint >= 0)
                {
                    cp = bufferedCodePoint;
                    startOffset = bufferedOff;
                    bufferedCodePoint = -1;
                }
                else
                {
                    startOffset = off;
                    cp = ReadCodePoint();
                }
                if (cp < 0)
                {
                    break;
                }
                else if (IsTokenChar(cp))
                {
                    int endOffset;
                    do
                    {
                        char[] chars = Character.ToChars(Normalize(cp));
                        for (int i = 0; i < chars.Length; i++)
                        {
                            termAtt.Append(chars[i]);
                        }
                        endOffset = off;
                        if (termAtt.Length >= maxTokenLength)
                        {
                            break;
                        }
                        cp = ReadCodePoint();
                    } while (cp >= 0 && IsTokenChar(cp));

                    if (termAtt.Length < maxTokenLength)
                    {
                        // buffer up, in case the "rejected" char can start a new word of its own
                        bufferedCodePoint = cp;
                        bufferedOff = endOffset;
                    }
                    else
                    {
                        // otherwise, its because we hit term limit.
                        bufferedCodePoint = -1;
                    }
                    int correctedStartOffset = CorrectOffset(startOffset);
                    int correctedEndOffset = CorrectOffset(endOffset);
                    Assert.True(correctedStartOffset >= 0);
                    Assert.True(correctedEndOffset >= 0);
                    Assert.True(correctedStartOffset >= lastOffset);
                    lastOffset = correctedStartOffset;
                    Assert.True(correctedEndOffset >= correctedStartOffset);
                    offsetAtt.SetOffset(correctedStartOffset, correctedEndOffset);
                    if (state == -1 || runAutomaton.IsAccept(state))
                    {
                        // either we hit a reject state (longest match), or end-of-text, but in an accept state
                        streamState = State.INCREMENT;
                        return true;
                    }
                }
            }
            streamState = State.INCREMENT_FALSE;
            return false;
        }

        protected internal virtual int ReadCodePoint()
        {
            int ch = ReadChar();
            if (ch < 0)
            {
                return ch;
            }
            else
            {
                Assert.True(!char.IsLowSurrogate((char)ch), "unpaired low surrogate: " + ch.ToString("x"));
                off++;
                if (char.IsHighSurrogate((char)ch))
                {
                    int ch2 = ReadChar();
                    if (ch2 >= 0)
                    {
                        off++;
                        Assert.True(char.IsLowSurrogate((char)ch2), "unpaired high surrogate: " + ch.ToString("x") + ", followed by: " + ch2.ToString("x"));
                        return Character.ToCodePoint((char)ch, (char)ch2);
                    }
                    else
                    {
                        Assert.True(false, "stream ends with unpaired high surrogate: " + ch.ToString("x"));
                    }
                }
                return ch;
            }
        }

        protected internal virtual int ReadChar()
        {
            switch (random.Next(0, 10))
            {
                case 0:
                    {
                        // read(char[])
                        char[] c = new char[1];
                        int ret = m_input.Read(c, 0, c.Length);
                        return ret <= 0 ? -1 : c[0];
                    }
                case 1:
                    {
                        // read(char[], int, int)
                        char[] c = new char[2];
                        int ret = m_input.Read(c, 1, 1);
                        return ret <= 0 ? -1 : c[1];
                    }
                // LUCENENET NOTE: CharBuffer not supported
                //case 2:
                //    {
                //        // read(CharBuffer)
                //        char[] c = new char[1];
                //        CharBuffer cb = CharBuffer.Wrap(c);
                //        int ret = m_input.Read(cb);
                //        return ret < 0 ? ret : c[0];
                //    }
                default:
                    // read()
                    return m_input.Read();
            }
        }

        protected internal virtual bool IsTokenChar(int c)
        {
            if (state < 0)
            {
                state = runAutomaton.InitialState;
            }
            state = runAutomaton.Step(state, c);
            if (state < 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        protected internal virtual int Normalize(int c)
        {
            return lowerCase ? Character.ToLower(c) : c;
        }

        public override void Reset()
        {
            base.Reset();
            state = runAutomaton.InitialState;
            lastOffset = off = 0;
            bufferedCodePoint = -1;
            Assert.True(!enableChecks || streamState != State.RESET, "double reset()");
            streamState = State.RESET;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                // in some exceptional cases (e.g. TestIndexWriterExceptions) a test can prematurely close()
                // these tests should disable this check, by default we check the normal workflow.
                // TODO: investigate the CachingTokenFilter "double-close"... for now we ignore this
                Assert.True(!enableChecks || streamState == State.END || streamState == State.CLOSE, "close() called in wrong state: " + streamState);
                streamState = State.CLOSE;
            }
        }

        internal override bool SetReaderTestPoint()
        {
            Assert.True(!enableChecks || streamState == State.CLOSE, "setReader() called in wrong state: " + streamState);
            streamState = State.SETREADER;
            return true;
        }

        public override void End()
        {
            base.End();
            int finalOffset = CorrectOffset(off);
            offsetAtt.SetOffset(finalOffset, finalOffset);
            // some tokenizers, such as limiting tokenizers, call end() before IncrementToken() returns false.
            // these tests should disable this check (in general you should consume the entire stream)
            try
            {
                //Debug.Assert(!EnableChecks_Renamed || StreamState == State.INCREMENT_FALSE, "end() called before IncrementToken() returned false!");
            }
            finally
            {
                streamState = State.END;
            }
        }

        /// <summary>
        /// Toggle consumer workflow checking: if your test consumes tokenstreams normally you
        /// should leave this enabled.
        /// </summary>
        public virtual bool EnableChecks
        {
            set
            {
                this.enableChecks = value;
            }
        }
    }
}