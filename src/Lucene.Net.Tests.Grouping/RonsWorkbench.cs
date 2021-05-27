/*
 *
 * Ron's Workbench
 *
*/

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Search.Grouping.Terms;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search.Grouping
{

    public class RonsWorkbench : LuceneTestCase
    {
               

        [Test]
        public void Verify_TwoAtomicReaders_Demo()
        {
            IndexReader reader = GetReaderOf20Models(CreateRandomPhysicalDir(), deleteRec18: true);   //reader has applyAllDeletes: true
            IndexSearcher searcher = new IndexSearcher(reader);

            int i = 0;

            foreach(AtomicReaderContext atomicReaderContext in reader.Leaves)
            {
                i++;
            }
            assertEquals(2, i);
        }


        /// <summary>
        /// Demonstrates how to seek a term and itterate to terms after that.
        /// Also demonstrates how to get the list of Live (not deleted) docs
        /// for each term.  Test data used specifically has one doc deleted.
        /// </summary>
        [Test]
        public void Terms_Seek_and_MoveNext_Demo()
        {
            IndexReader reader = GetReaderOf20Models(CreateRandomPhysicalDir(), deleteRec18:true);   //reader has applyAllDeletes: true
            IndexSearcher searcher = new IndexSearcher(reader);

            string fieldName = "model";
            BytesRef ValueBytesRef = new BytesRef("Aspire");

            Index.Terms terms = MultiFields.GetTerms(reader, fieldName);
            TermsEnum termsEnum = terms.GetEnumerator(null);

            IBits liveDocs = MultiFields.GetLiveDocs(reader);                   //Reader's applyAllDeletes parm affects this

            /// Attempts to seek to the exact term, returning
            /// true if the term is found.  If this returns false, the
            /// enum is unpositioned.  For some codecs, SeekExact(BytesRef) may
            /// be substantially faster than SeekCeil(BytesRef).
            bool success = termsEnum.SeekExact(ValueBytesRef);
            assertTrue(success);
            assertEquals("Aspire", termsEnum.Term.Utf8ToString());

            DocsEnum docsEnum = termsEnum.Docs(liveDocs, reuse: null);
            string docsCsv = GetDocsCsv(docsEnum);
            assertEquals("12", docsCsv);

            //Next term
            success = termsEnum.MoveNext();
            assertTrue(success);
            assertEquals("Azure", termsEnum.Term.Utf8ToString());

            docsEnum = termsEnum.Docs(liveDocs, reuse: null);
            docsCsv = GetDocsCsv(docsEnum);
            assertEquals("10", docsCsv);                    //Doc 18 not in doc list because it was deleted.

            //Next term
            success = termsEnum.MoveNext();
            assertTrue(success);
            assertEquals("Bronco", termsEnum.Term.Utf8ToString());

            docsEnum = termsEnum.Docs(liveDocs, reuse: null);
            docsCsv = GetDocsCsv(docsEnum);
            assertEquals("2, 11, 19", docsCsv);

            /*
             This is what the data looks like sorted by brand then by sampleId
             which in a sense is how it will be encounted via the searcher.
             Ie. Terms ares sorted, and each term's posting list is sorted by docId.
                    0	4Runner
                    1	4Runner
                    16	4Runner
                    7	A3
                    13	A3
                    15	Arnage
                    12	Aspire
                    10	Azure
                    18	Azure   *deleted
                    2	Bronco
                    11	Bronco
                    19	Bronco
                    8	Camry
                    17	Camry
                    5	Celica
                    3	Expedition
                    4	Expedition
                    9	F150 Truck
                    6	S4
                    14	S4
             */
        }




        /// <summary>
        /// Creates a index with 20 car models and returns a reader to that data.
        /// The index is comprised of two segments.
        /// Reader has applyAllDeletes set to true
        /// </summary>
        /// <param name="indexDir"></param>
        /// <param name="indexDir"></param>
        /// <returns></returns>
        public IndexReader GetReaderOf20Models(Directory indexDir, bool deleteRec18 = false)
        {
            string[,] carData = GetCarData();

            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            Document doc = new Document();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 20; i++)
            {
                doc.Fields.Clear();
                doc.Add(new StringField("sampleId", ZeroPad11(i), Field.Store.YES));
                string modelName = carData[i, 1];

                doc.Add(new StringField("model", modelName, Field.Store.YES));
                doc.Add(new StringField("description", $"This is the text for rec {i}", Field.Store.YES));
                writer.AddDocument(doc);

                sb.AppendLine(modelName);

                if(i == 10)
                {
                    writer.Commit();            //creates first segment
                }
            }
            writer.Commit();                    //creates second segment

            if (deleteRec18)
            {
                TermQuery termQuery = new TermQuery(new Term("sampleId", ZeroPad11(18)));
                writer.DeleteDocuments(termQuery);
                writer.Commit();
            }

            return writer.GetReader(applyAllDeletes: true);

            /*
             This is what the data looks like sorted by brand then by sampleId
             which in a sense is how it will be encounted via the searcher.
             Ie. Terms ares sorted, and each term's posting list is sorted by docId.
                    0	4Runner
                    1	4Runner
                    16	4Runner
                    7	A3
                    13	A3
                    15	Arnage
                    12	Aspire
                    10	Azure
                    18	Azure       (optionally deleted)
                    2	Bronco
                    11	Bronco
                    19	Bronco
                    8	Camry
                    17	Camry
                    5	Celica
                    3	Expedition
                    4	Expedition
                    9	F150 Truck
                    6	S4
                    14	S4
             */
        }


        /// <summary>
        /// Itterates over the docsEnum starting at the current location
        /// and returns a csv list of the doc Ids;
        /// </summary>
        /// <param name="docsEnum"></param>
        /// <returns></returns>
        public string GetDocsCsv(DocsEnum docsEnum)
        {
            StringBuilder sb = new StringBuilder();
            while (docsEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                sb.Append($"{docsEnum.DocID}, ");
            }
            if (sb.Length > 2)
            {
                sb.Length -= 2;  //remove trailing comma space
            }
            return sb.toString();
        }

        public Directory CreateRandomPhysicalDir()
        {
            System.IO.DirectoryInfo dirInfo = CreateTempDir($"rontest.workbench.{Random.nextInt()}");       //use DISK based index
            return NewFSDirectory(dirInfo);
        }

        public static string ZeroPad11(Int32 val)
        {
            return val.ToString().PadLeft(11, '0');
        }

        private string[,] GetCarData()
        {

            return new string[,] {
                { "Toyota", "4Runner" , "Blue" },
                { "Toyota", "4Runner" , "Green" },
                { "Ford", "Bronco" , "Green" },
                { "Ford", "Expedition" , "Yellow" },
                { "Ford", "Expedition" , "Blue" },
                { "Toyota", "Celica" , "Orange" },
                { "Audi", "S4" , "Yellow" },
                { "Audi", "A3" , "Orange" },
                { "Toyota", "Camry" , "Yellow" },
                { "Ford", "F150 Truck" , "Green" },
                { "Bently", "Azure" , "Green" },
                { "Ford", "Bronco" , "Orange" },
                { "Ford", "Aspire" , "Yellow" },
                { "Audi", "A3" , "Green" },
                { "Audi", "S4" , "Blue" },
                { "Bently", "Arnage" , "Grey" },
                { "Toyota", "4Runner" , "Yellow" },
                { "Toyota", "Camry" , "Blue" },
                { "Bently", "Azure" , "Blue" },
                { "Ford", "Bronco" , "Blue" },
                { "Ford", "Expedition" , "Green" },
                { "Ford", "F150 Truck" , "Blue" },
                { "Toyota", "Celica" , "Blue" },
                { "Ford", "F150 Truck" , "Yellow" },
                { "Ford", "Aspire" , "Blue" },
                { "Audi", "A3" , "Blue" },
                { "Bently", "Arnage" , "Blue" },
            };
        }
    }
}
