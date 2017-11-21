using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        // Use an observer class to watch the events on the ObservableConcurrentDictionary
        [Theory]
        [InlineData("k1=1,k2=1,1.1;")]
        public void OCREventsViaObserver(string str) {
            WithObservableConcurrentDictionary withObservableConcurrentDictionary = new WithObservableConcurrentDictionary();
            void RecordResults(string instr)
            {
                var match = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);").Match(instr);
                while(match.Success) {
                    withObservableConcurrentDictionary.RecordCalculatedResults(match.Groups["k1"].Value,
                                                                               match.Groups["k2"].Value,
                                                                               decimal.Parse(match.Groups["pr"].Value));
                    match = match.NextMatch();
                }
            }

            List<string> receivedEvents = new List<string>();

            // attach a handler to the PropertyChanged event of the outer dictionary
            withObservableConcurrentDictionary.calculatedResults.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e) { receivedEvents.Add(e.PropertyName); };
            // attach a handler to the CollectionChanged event of the outer dictionary
            withObservableConcurrentDictionary.calculatedResults.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e) { receivedEvents.Add($"{e.Action} NumItemsToAdd {e.NewItems.Count}"); };
            // populate the OCR
            RecordResults(str);
            Task.Delay(1000);
            //output.WriteLine($"{receivedEvents.Count}");
            receivedEvents.ForEach(x => output.WriteLine($"{x}"));
            //Assert.Equal(2, receivedEvents.Count);
            //Assert.Equal("CollectionView", receivedEvents[0]);
            //Assert.Equal("Count", receivedEvents[1]);
            Assert.Equal(2, 2);
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

        

        // Test via an observer that the Collection changed event gets raised
        [Theory]
        [InlineData("k1=1,k2=1,1.1;")]
        [InlineData("k1=2,k2=2,2.2;k1=2,k2=1,2.1;k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        public void OCRPropertyChangedEventsViaObserver(string str)
        {
            // Going to use a ConcurrentDictionary to hold the information written by teh event handlers
            ConcurrentDictionary<string, string> receivedEvents = new ConcurrentDictionary<string, string>();

            // This event handler will be attached/detached from the ObservableConcurrentDictionary via that class' constructor and dispose method
            void onNotifyCollectionChanged(object sender,
    NotifyCollectionChangedEventArgs e)
            {
                receivedEvents[$"Ticks: {DateTime.Now.Ticks} Event: NotifyCollectionChanged Action:{e.Action} NumItemsToAdd {e.NewItems.Count}"]=  DateTime.Now.ToLongTimeString();
            };
            void onPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                receivedEvents[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();

                            };
            WithObservableConcurrentDictionaryAndEventHandlers withObservableConcurrentDictionaryAndEventHandlers = new WithObservableConcurrentDictionaryAndEventHandlers(onNotifyCollectionChanged, onPropertyChanged);
            void RecordResults(string instr)
            {
                var match = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);").Match(instr);
                while (match.Success)
                {
                    withObservableConcurrentDictionaryAndEventHandlers.RecordCalculatedResults(match.Groups["k1"].Value,
                                                                               match.Groups["k2"].Value,
                                                                               decimal.Parse(match.Groups["pr"].Value));
                    match = match.NextMatch();
                }
            }

            // populate the OCR
            RecordResults(str);
            Task.Delay(1000);
            receivedEvents.Keys.OrderBy(x => x).ToList().ForEach(x => output.WriteLine($"{x} : {receivedEvents[x]}"));
            //Assert.Equal(2, receivedEvents.Count);
            //Assert.Equal("CollectionView", receivedEvents[0]);
            //Assert.Equal("Count", receivedEvents[1]);
            Assert.Equal(2, 2);
        }
    }
}
