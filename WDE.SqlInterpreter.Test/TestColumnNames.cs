using System.Linq;
using NUnit.Framework;

namespace WDE.SqlInterpreter.Test
{
    public class TestColumnNames
    {
        private QueryEvaluator evaluator = null!;
    
        [SetUp]
        public void Setup()
        {
            evaluator = new();
        }

        [Test]
        public void NoQuotes()
        {
            /* this should be supported, but it isn't now because of this antlr4 grammar */
            /* if grammar is fixed (to accept column names without quotes, this test can be fixed */
            var result = evaluator.ExtractInserts("INSERT INTO `abc` (guid, name, surname) VALUES (5, 6, 7);").ToList();
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void QuotesSingle()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` ('guid', 'name', 'surname') VALUES (5, 6, 7);").ToList();
            CollectionAssert.AreEqual(new []{"guid", "name", "surname"}, result[0].Columns);
        }

        [Test]
        public void QuotesDouble()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` (\"guid\", \"name\", \"surname\") VALUES (5, 6, 7);").ToList();
            CollectionAssert.AreEqual(new []{"guid", "name", "surname"}, result[0].Columns);
        }

        [Test]
        public void QuotesBacktick()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` (`guid`, `name`, `surname`) VALUES (5, 6, 7);").ToList();
            CollectionAssert.AreEqual(new []{"guid", "name", "surname"}, result[0].Columns);
        }
    }
}