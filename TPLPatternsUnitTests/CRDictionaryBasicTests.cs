using System;
using Xunit;
using TPLPatternExamples;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TPLPatternsUnitTests
{
    public class CRDictionaryBasicTests
    {
        [Theory, InlineData("k1=1","k2=2",1.0)]
        public void ValidateCRHasOneCount(string k1,string k2, decimal pr)
        {
            TPLPatterns tplPatterns = new TPLPatterns();
            tplPatterns.RecordCalculatedResults(k1, k2, pr);
            Assert.Equal(1, tplPatterns.calculatedResults.Count);
        }
        [Theory, InlineData("k1=1", "k2=2", 1.1)]
        public void ValidateCRFlattened1And1(string k1, string k2, decimal pr)
        {
            TPLPatterns tplPatterns = new TPLPatterns();
            tplPatterns.RecordCalculatedResults(k1, k2, pr);
            string fs = "";
            tplPatterns.calculatedResults.SelectMany(
                pair => pair.Value.SelectMany(
                    innerPair => $"{pair.Key},{innerPair.Key},{innerPair.Value};")).ToList().ForEach(x => fs += x.ToString());
            Assert.Equal("k1=1,k2=2,1.1;", fs);
        }

        // Some of the InlineData is not sorted, but the query over the dictionaries will sort, so the asserts are for the results in sorted order
        [Theory, InlineData( "k1=2,k2=2,2.2;k1=2,k2=1,2.1;k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        public void ValidateCRFlattened2And2(string str)
        {
            TPLPatterns tplPatterns = new TPLPatterns();
            Regex RE = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);");
            var match = RE.Match(str);
            while (match.Success)
            {
                tplPatterns.RecordCalculatedResults(match.Groups["k1"].Value, match.Groups["k2"].Value, decimal.Parse(match.Groups["pr"].Value));
                match = match.NextMatch();
            }
            string fs = "";
            tplPatterns.calculatedResults.OrderBy(kp=>kp.Key).SelectMany(
                pair => pair.Value.OrderBy(kp => kp.Key).SelectMany(
                    innerPair => $"{pair.Key},{innerPair.Key},{innerPair.Value};"))
                    .ToList().ForEach(x => fs += x.ToString());
            Assert.Equal("k1=1,k2=1,1.1;k1=1,k2=2,1.2;k1=2,k2=1,2.1;k1=2,k2=2,2.2;", fs);
        }
    }
    public class ObservableConcurrentDictionaryForResultsBasicTests
    {
        [Theory, InlineData("k1=1", "k2=2", 1.0)]
        public void ValidateOCRHasOneCount(string k1, string k2, decimal pr)
        {
            WithObservableConcurrentDictionary withObservableConcurrentDictionary = new WithObservableConcurrentDictionary();
            withObservableConcurrentDictionary.RecordCalculatedResults(k1, k2, pr);
            Assert.Equal(1, withObservableConcurrentDictionary.calculatedResults.Count());
        }
        // Some of the InlineData is not sorted, but the query over the dictionaries will sort, so the asserts are for the results in sorted order
        [Theory, InlineData("k1=2,k2=2,2.2;k1=2,k2=1,2.1;k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        public void ValidateOOCRFlattened2And2(string str)
        {
            WithObservableConcurrentDictionary withObservableConcurrentDictionary = new WithObservableConcurrentDictionary();
            var match = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);").Match(str);
            while (match.Success)
            {
                withObservableConcurrentDictionary.RecordCalculatedResults(match.Groups["k1"].Value, match.Groups["k2"].Value, decimal.Parse(match.Groups["pr"].Value));
                match = match.NextMatch();
            }
            string fs = "";
            withObservableConcurrentDictionary.calculatedResults.OrderBy(kp => kp.Key).SelectMany(
                pair => pair.Value.OrderBy(kp => kp.Key).SelectMany(
                    innerPair => $"{pair.Key},{innerPair.Key},{innerPair.Value};"))
                    .ToList().ForEach(x => fs += x.ToString());
            Assert.Equal("k1=1,k2=1,1.1;k1=1,k2=2,1.2;k1=2,k2=1,2.1;k1=2,k2=2,2.2;", fs);
        }

    }
}
