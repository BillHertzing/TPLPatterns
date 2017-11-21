using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using TPLPatternExamples;
using Xunit;
using Xunit.Abstractions;

namespace TPLPatternsUnitTests {
    public class ConcurrentDictionaryForResultsBasicTests {
        [Theory]
        [InlineData("k1=1", "k2=2", 1.1)]
        public void CRFlattened1And1(string k1, string k2, decimal pr) {
            TPLPatterns tplPatterns = new TPLPatterns();
            tplPatterns.RecordCalculatedResults(k1, k2, pr);
            string fs = string.Empty;
            tplPatterns.calculatedResults.SelectMany(pair => pair.Value.SelectMany(innerPair => $"{pair.Key},{innerPair.Key},{innerPair.Value};"))
                .ToList()
                .ForEach(x => fs += x.ToString());
            Assert.Equal("k1=1,k2=2,1.1;", fs);
        }

        // Some of the InlineData is not sorted, but the query over the dictionaries will sort, so the asserts are for the results in sorted order
        [Theory]
        [InlineData("k1=2,k2=2,2.2;k1=2,k2=1,2.1;k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        public void CRFlattened2And2(string str) {
            TPLPatterns tplPatterns = new TPLPatterns();
            Regex RE = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);");
            var match = RE.Match(str);
            while(match.Success) {
                tplPatterns.RecordCalculatedResults(match.Groups["k1"].Value,
                                                    match.Groups["k2"].Value,
                                                    decimal.Parse(match.Groups["pr"].Value));
                match = match.NextMatch();
            }
            string fs = string.Empty;
            tplPatterns.calculatedResults.OrderBy(kp => kp.Key)
                .SelectMany(pair => pair.Value.OrderBy(kp => kp.Key)
                                        .SelectMany(innerPair => $"{pair.Key},{innerPair.Key},{innerPair.Value};"))
                .ToList()
                .ForEach(x => fs += x.ToString());
            Assert.Equal("k1=1,k2=1,1.1;k1=1,k2=2,1.2;k1=2,k2=1,2.1;k1=2,k2=2,2.2;",
                         fs);
        }

        [Theory]
        [InlineData("k1=1", "k2=2", 1.0)]
        public void ValidateCRHasOneCount(string k1, string k2, decimal pr) {
            TPLPatterns tplPatterns = new TPLPatterns();
            tplPatterns.RecordCalculatedResults(k1, k2, pr);
            Assert.Equal(1, tplPatterns.calculatedResults.Count);
        }
    }

    public class ObservableConcurrentDictionaryForResultsBasicTests {
        readonly ITestOutputHelper output;

        public ObservableConcurrentDictionaryForResultsBasicTests(ITestOutputHelper output) {
            this.output = output;
        }

        // Some of the InlineData is not sorted, but the query over the dictionaries will sort, so the asserts are for the results in sorted order
        [Theory]
        [InlineData("k1=2,k2=2,2.2;k1=2,k2=1,2.1;k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        public void OCRFlattened2And2(string str) {
            WithObservableConcurrentDictionary withObservableConcurrentDictionary = new WithObservableConcurrentDictionary();
            var match = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);").Match(str);
            while(match.Success) {
                withObservableConcurrentDictionary.RecordCalculatedResults(match.Groups["k1"].Value,
                                                                           match.Groups["k2"].Value,
                                                                           decimal.Parse(match.Groups["pr"].Value));
                match = match.NextMatch();
            }
            string fs = string.Empty;
            withObservableConcurrentDictionary.calculatedResults.OrderBy(kp => kp.Key)
                .SelectMany(pair => pair.Value.OrderBy(kp => kp.Key)
                                        .SelectMany(innerPair => $"{pair.Key},{innerPair.Key},{innerPair.Value};"))
                .ToList()
                .ForEach(x => fs += x.ToString());
            Assert.Equal("k1=1,k2=1,1.1;k1=1,k2=2,1.2;k1=2,k2=1,2.1;k1=2,k2=2,2.2;",
                         fs);
        }

        [Theory]
        [InlineData("k1=1", "k2=2", 1.0)]
        public void OCRHasOneCount(string k1, string k2, decimal pr) {
            WithObservableConcurrentDictionary withObservableConcurrentDictionary = new WithObservableConcurrentDictionary();
            withObservableConcurrentDictionary.RecordCalculatedResults(k1,
                                                                       k2,
                                                                       pr);
            Assert.Equal(1,
                         withObservableConcurrentDictionary.calculatedResults.Count());
        }

        // Test via the Observer pattern that the NotifyCollectionChanged event gets raised correctly
        [Theory]
        [InlineData("k1=1,k2=1,1.1;")]
        [InlineData("k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        [InlineData("k1=2,k2=2,2.2;k1=2,k2=1,2.1;k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        public void OCRPropertyChangedEventsViaObserver(string str) {
            // Going to use a ConcurrentDictionary to hold the information written by the event handlers
            ConcurrentDictionary<string, string> receivedEvents = new ConcurrentDictionary<string, string>();

            // These event handler will be attached/detached from the ObservableConcurrentDictionary via that class' constructor and dispose method
            void onNotifyCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                string s = $"Ticks: {DateTime.Now.Ticks} Event: NotifyCollectionChanged  Action: {e.Action}  ";
                switch(e.Action) {
                    case NotifyCollectionChangedAction.Add:
                        s += $"NumItemsToAdd { e.NewItems.Count}";
                        break;
                    case NotifyCollectionChangedAction.Move:
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        s += $"NumItemsToDel {e.OldItems.Count}";
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        break;
                    default:
                        break;
                }
                receivedEvents[s] = DateTime.Now.ToLongTimeString();
            };
            void onPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                receivedEvents[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();

            };
            //These event handlers will be attached to each innerDictionary
            void onNotifyNestedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                string s = $"Ticks: {DateTime.Now.Ticks} Event: NotifyNestedCollectionChanged  Action: {e.Action}  ";
                switch(e.Action) {
                    case NotifyCollectionChangedAction.Add:
                        s += $"NumItemsToAdd { e.NewItems.Count}";
                        break;
                    case NotifyCollectionChangedAction.Move:
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        s += $"NumItemsToDel {e.OldItems.Count}";
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        break;
                    default:
                        break;
                }
                receivedEvents[s] = DateTime.Now.ToLongTimeString();
            };
            void onNestedPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                receivedEvents[$"Ticks: {DateTime.Now.Ticks} Event: NestedPropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();

            };
            WithObservableConcurrentDictionaryAndEventHandlers withObservableConcurrentDictionaryAndEventHandlers = new WithObservableConcurrentDictionaryAndEventHandlers(onNotifyCollectionChanged,
                                                                                                                                                                           onPropertyChanged,
                                                                                                                                                                           onNotifyNestedCollectionChanged,
                                                                                                                                                                           onNestedPropertyChanged);
            int RecordResults(string instr)
            {
                var match = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);").Match(instr);
                int _numResultsRecorded = default;
                while(match.Success) {
                    withObservableConcurrentDictionaryAndEventHandlers.RecordCalculatedResults(match.Groups["k1"].Value,
                                                                                               match.Groups["k2"].Value,
                                                                                               decimal.Parse(match.Groups["pr"].Value));
                    _numResultsRecorded++;
                    match = match.NextMatch();
                }
                return _numResultsRecorded;
            }

            // populate the OCR
            var numResultsRecorded = RecordResults(str);
            // send the observed events to test output
            receivedEvents.Keys.OrderBy(x => x)
                .ToList()
                .ForEach(x => output.WriteLine($"{x} : {receivedEvents[x]}"));
            // There should be as many inner NotifyCollectionChanged events are there are results recorded.
            var numInnerNotifyCollectionChanged = receivedEvents.Keys.Where(x => x.Contains("Event: NotifyNestedCollectionChanged"))
                                                      .ToList()
                                                      .Count;
            Assert.Equal(numResultsRecorded, numInnerNotifyCollectionChanged);
            // There should be as many outer NotifyCollectionChanged events are there are unique values of K1 in the input str.
            var matchUniqueK1Values = new Regex("(?<k1>.*?),.*?;").Match(str);
            var dictUniqueK1Values = new Dictionary<string, int>();
            while(matchUniqueK1Values.Success) {
                dictUniqueK1Values[matchUniqueK1Values.Groups["k1"].Value] = 0;
                matchUniqueK1Values = matchUniqueK1Values.NextMatch();
            }
            var numUniqueK1Values = dictUniqueK1Values.Keys.Count;
            var numOuterNotifyCollectionChanged = receivedEvents.Keys.Where(x => x.Contains("Event: NotifyCollectionChanged"))
                                                      .ToList()
                                                      .Count;
            Assert.Equal(numUniqueK1Values, numOuterNotifyCollectionChanged);
        }
    }
}
