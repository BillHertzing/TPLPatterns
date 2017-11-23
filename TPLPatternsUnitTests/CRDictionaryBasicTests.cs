using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TPLPatternExamples;
using Xunit;
using Xunit.Abstractions;


namespace TPLPatternsUnitTests
{
    public class ConcurrentDictionaryForResultsBasicTests
    {
        [Theory]
        [InlineData("k1=1", "k2=2", 1.1)]
        public void CRFlattened1And1(string k1, string k2, decimal pr)
        {
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
        public void CRFlattened2And2(string str)
        {
            TPLPatterns tplPatterns = new TPLPatterns();
            Regex RE = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);");
            var match = RE.Match(str);
            while (match.Success)
            {
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
        public void ValidateCRHasOneCount(string k1, string k2, decimal pr)
        {
            TPLPatterns tplPatterns = new TPLPatterns();
            tplPatterns.RecordCalculatedResults(k1, k2, pr);
            Assert.Equal(1, tplPatterns.calculatedResults.Count);
        }
    }

    public class ObservableConcurrentDictionaryForResultsBasicTests
    {
        readonly ITestOutputHelper output;

        public ObservableConcurrentDictionaryForResultsBasicTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        // Some of the InlineData is not sorted, but the query over the dictionaries will sort, so the asserts are for the results in sorted order
        [Theory]
        [InlineData("k1=2,k2=2,2.2;k1=2,k2=1,2.1;k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        public void CORFlattened2And2(string str)
        {
            WithConcurrentObservableDictionary withObservableConcurrentDictionary = new WithConcurrentObservableDictionary();
            var match = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);").Match(str);
            while (match.Success)
            {
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
        public void CORHasOneCount(string k1, string k2, decimal pr)
        {
            WithConcurrentObservableDictionary withObservableConcurrentDictionary = new WithConcurrentObservableDictionary();
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
        public void CORPropertyChangedEventsViaObserver(string str)
        {
            // Going to use a ConcurrentDictionary to hold the information written by the event handlers
            ConcurrentDictionary<string, string> receivedEvents = new ConcurrentDictionary<string, string>();

            // The messages to be written to the receivedEvent dictionary
            string Message(string depth, NotifyCollectionChangedEventArgs e)
            {
                string s = $"Ticks: {DateTime.Now.Ticks} Event: Notify{depth}CollectionChanged  Action: {e.Action}  ";
                switch (e.Action)
                {
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
                return s;

            }

            // These event handler will be attached/detached from the ObservableConcurrentDictionary via that class' constructor and dispose method
            void onNotifyCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                receivedEvents[Message("Outer", e)] = DateTime.Now.ToLongTimeString();
            }
            void onPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                receivedEvents[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();

            }
            //These event handlers will be attached to each innerDictionary
            void onNotifyNestedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                receivedEvents[Message("Nested", e)] = DateTime.Now.ToLongTimeString();
            };
            void onNestedPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                receivedEvents[$"Ticks: {DateTime.Now.Ticks} Event: NestedPropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();

            };

            // Create the Results with the specified event handlers
            WithObservableConcurrentDictionaryAndEventHandlers withObservableConcurrentDictionaryAndEventHandlers = new WithObservableConcurrentDictionaryAndEventHandlers(onNotifyCollectionChanged,
                                                                                                                                                                           onNotifyNestedCollectionChanged);
            // A method that parses the input string and record it into the Results dictionary, returning the number of inserts it performed
            int RecordResults(string instr)
            {
                var match = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);").Match(instr);
                int _numResultsRecorded = default;
                while (match.Success)
                {
                    withObservableConcurrentDictionaryAndEventHandlers.RecordCalculatedResults(match.Groups["k1"].Value,
                                                                                               match.Groups["k2"].Value,
                                                                                               decimal.Parse(match.Groups["pr"].Value));
                    _numResultsRecorded++;
                    match = match.NextMatch();
                }
                return _numResultsRecorded;
            }

            // populate the COR
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
            while (matchUniqueK1Values.Success)
            {
                dictUniqueK1Values[matchUniqueK1Values.Groups["k1"].Value] = 0;
                matchUniqueK1Values = matchUniqueK1Values.NextMatch();
            }
            var numUniqueK1Values = dictUniqueK1Values.Keys.Count;
            var numOuterNotifyCollectionChanged = receivedEvents.Keys.Where(x => x.Contains("Event: NotifyOuterCollectionChanged"))
                                                      .ToList()
                                                      .Count;
            Assert.Equal(numUniqueK1Values, numOuterNotifyCollectionChanged);
        }
    }

    // Create a disposable TestDataFixture for common and reusable methods
    public class CORHelpersFirst : IDisposable
    {
        // create a ConcurrentDictionary to hold the information written by the event handlers
        public ConcurrentDictionary<string, string> receivedResultsEvents = new ConcurrentDictionary<string, string>();

        // The messages to be written to the receivedEvent dictionary
        public string Message(string depth, NotifyCollectionChangedEventArgs e)
        {
            string s = $"Ticks: {DateTime.Now.Ticks} Event: Notify{depth}CollectionChanged  Action: {e.Action}  ";
            switch (e.Action)
            {
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
            return s;
        }

        public void Dispose()
        {
        }

        //These event handlers will be attached to each innerDictionary
        public void onNotifyNestedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            receivedResultsEvents[Message("Nested", e)] = DateTime.Now.ToLongTimeString();
        }

        public void onNestedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            receivedResultsEvents[$"Ticks: {DateTime.Now.Ticks} Event: NestedPropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }

        // These event handler will be attached/detached from the ObservableConcurrentDictionary via that class' constructor and dispose method
        public void onNotifyOuterCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            receivedResultsEvents[Message("Outer", e)] = DateTime.Now.ToLongTimeString();
        }

        public void onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            receivedResultsEvents[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }

        // parse the input and call the recordResults method repeatedly, returning the number of time it is called
        public int RecordResults(string str, Action<string, string, decimal> recordResults)
        {
            var match = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);").Match(str);
            int _numResultsRecorded = default;
            while (match.Success)
            {
                recordResults(match.Groups["k1"].Value,
                              match.Groups["k2"].Value,
                              decimal.Parse(match.Groups["pr"].Value));
                _numResultsRecorded++;
                match = match.NextMatch();
            }
            return _numResultsRecorded;
        }
    }

    // At this point, start using the collection fixture
    public class CORFromDataFlowBlockBasicTests : IClassFixture<CORHelpersFirst>
    {
        CORHelpersFirst _fixture;
        readonly ITestOutputHelper output;

        public CORFromDataFlowBlockBasicTests(ITestOutputHelper output, CORHelpersFirst cORHelpers)
        {
            this.output = output;
            this._fixture = cORHelpers;
        }

        // Some of the InlineData is not sorted, but the query over the dictionaries will sort, so the asserts are for the results in sorted order
        [Theory]
        [InlineData("k1=1,k2=1,1.1;")]
        [InlineData("k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        [InlineData("k1=2,k2=2,2.2;k1=2,k2=1,2.1;k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        public void CORBasic(string str)
        {
            // since the receivedResultsEvents list in the fixture is shared between test, the list needs to be cleared
            _fixture.receivedResultsEvents.Clear();
            // Create the Results with the specified event handlers
            WithObservableConcurrentDictionaryAndEventHandlers withObservableConcurrentDictionaryAndEventHandlers = new WithObservableConcurrentDictionaryAndEventHandlers(_fixture.onNotifyOuterCollectionChanged,
                                                                                                                                                                           _fixture.onNotifyNestedCollectionChanged);

            // populate the COR
            var numResultsRecorded = _fixture.RecordResults(str,
                                                            withObservableConcurrentDictionaryAndEventHandlers.RecordCalculatedResults);
            //Ensure events have a chance to propagate
            Task.Delay(100);
            // send the observed events to test output
            _fixture.receivedResultsEvents.Keys.OrderBy(x => x)
                .ToList()
                .ForEach(x => output.WriteLine($"{x} : {_fixture.receivedResultsEvents[x]}"));
            // There should be as many inner NotifyCollectionChanged events are there are results recorded.
            var numInnerNotifyCollectionChanged = _fixture.receivedResultsEvents.Keys.Where(x => x.Contains("Event: NotifyNestedCollectionChanged"))
                                                      .ToList()
                                                      .Count;
            Assert.Equal(numResultsRecorded, numInnerNotifyCollectionChanged);
            // There should be as many outer NotifyCollectionChanged events are there are unique values of K1 in the input str.
            var matchUniqueK1Values = new Regex("(?<k1>.*?),.*?;").Match(str);
            var dictUniqueK1Values = new Dictionary<string, int>();
            while (matchUniqueK1Values.Success)
            {
                dictUniqueK1Values[matchUniqueK1Values.Groups["k1"].Value] = 0;
                matchUniqueK1Values = matchUniqueK1Values.NextMatch();
            }
            var numUniqueK1Values = dictUniqueK1Values.Keys.Count;
            var numOuterNotifyCollectionChanged = _fixture.receivedResultsEvents.Keys.Where(x => x.Contains("Event: NotifyOuterCollectionChanged"))
                                                      .ToList()
                                                      .Count;
            Assert.Equal(numUniqueK1Values, numOuterNotifyCollectionChanged);
            // since the fixture is shared between test, the fixture needs to be cleared
            _fixture.receivedResultsEvents.Clear();
        }
        [Theory]
        [InlineData("k1=1,k2=1,1.1;")]
        [InlineData("k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        [InlineData("k1=2,k2=2,2.2;k1=2,k2=1,2.1;k1=1,k2=1,1.1;k1=1,k2=2,1.2;")]
        public void CORViaTrivialDataFlowBlock(string testInStr)
        {
            // since the receivedResultsEvents list in the fixture is shared between test, the list needs to be cleared
            _fixture.receivedResultsEvents.Clear();
            // Create the Results CODict with the specified event handlers
            using (WithObservableConcurrentDictionaryAndEventHandlers withObservableConcurrentDictionaryAndEventHandlers = new WithObservableConcurrentDictionaryAndEventHandlers(_fixture.onNotifyOuterCollectionChanged,
                                                                                                                                                                           _fixture.onNotifyNestedCollectionChanged))
            {
                var REinner = new Regex("(?<k1>.*?),(?<k2>.*?),(?<pr>.*?);");
                // create the individual results via a transform block
                // this block takes in one small string, and uses a regEx to split it into three fields, and output it as a Tuple
                var calculateResults = new TransformBlock<string, Tuple<string, string, decimal>>(instr =>
                {
                    var outtup = default(Tuple<string, string, decimal>);
                    var match = REinner.Match(instr);
                    if (match.Success)
                    {
                        outtup = new Tuple<string, string, decimal>(match.Groups["k1"].Value,
                                      match.Groups["k2"].Value,
                                      decimal.Parse(match.Groups["pr"].Value));
                        ;
                    }

                    return outtup;
                });
                // populate the Results via an actionBlock
                // This is a trivial ActionBlock
                var populateResults = new ActionBlock<Tuple<string, string, decimal>>(intup =>
                {
                    withObservableConcurrentDictionaryAndEventHandlers.RecordCalculatedResults(intup.Item1, intup.Item2, intup.Item3);
                });

                //Link calculateResults to populateResults
                calculateResults.LinkTo(populateResults);

                // Link together the completion/continuation tasks
                calculateResults.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted) ((IDataflowBlock)populateResults).Fault(t.Exception);
                    else populateResults.Complete();
                });

                // Split the testInStr string on the ;, and send each substring into the head of the pipeline
                var REouter = new Regex("(?<oneTuple>.*?;)");
                var matchOuter = REouter.Match(testInStr);
                while (matchOuter.Success)
                {
                    // no error checking
                    calculateResults.Post(matchOuter.Groups["oneTuple"].Value);
                    matchOuter = matchOuter.NextMatch();
                }

                // inform the head that there is no more data
                calculateResults.Complete();

                // wait for the tail of the pipeline to indicate completion
                populateResults.Completion.Wait();
            } // the COD will be disposed of at this point
            //Ensure events have a chance to propagate
            Task.Delay(100);
            // send the observed events to test output
            _fixture.receivedResultsEvents.Keys.OrderBy(x => x)
                .ToList()
                .ForEach(x => output.WriteLine($"{x} : {_fixture.receivedResultsEvents[x]}"));
            // Count the number of inner and outer CollectionChanged events that occurred
            var numInnerNotifyCollectionChanged = _fixture.receivedResultsEvents.Keys.Where(x => x.Contains("Event: NotifyNestedCollectionChanged"))
                                                      .ToList()
                                                      .Count;
            var numOuterNotifyCollectionChanged = _fixture.receivedResultsEvents.Keys.Where(x => x.Contains("Event: NotifyOuterCollectionChanged"))
                                                      .ToList()
                                                      .Count;
            // find the number of unique values of K1 and the number of k1k2 pairs in the test's input data
            // There should be as many outer NotifyCollectionChanged events are there are unique values of K1 in the input data.
            // There should be as many inner NotifyCollectionChanged events are there are unique values of K1K2 pairs in the input data.
            var matchUniqueKValues = new Regex("(?<k1>.*?),(?<k2>.*?),.*?;").Match(testInStr);
            var uniqueK1Values = new HashSet<string>();
            var uniqueK1K2PairValues = new HashSet<string>();
            while (matchUniqueKValues.Success)
            {
                // Nice thing about HashSets is, they won't complain if you try to add a duplicate
                uniqueK1Values.Add(matchUniqueKValues.Groups["k1"].Value);
                uniqueK1K2PairValues.Add(matchUniqueKValues.Groups["k1"].Value + matchUniqueKValues.Groups["k2"].Value);
                matchUniqueKValues = matchUniqueKValues.NextMatch();
            }
            // number of unique values of K1 in the test's input data
            var numUniqueK1Values = uniqueK1Values.Count;
            // number of unique values of K1K2 pairs in the test's input data
            var numUniqueK1K2PairValues = uniqueK1K2PairValues.Count;
            // There should be as many outer NotifyCollectionChanged events are there are unique values of K1 in the input data.
            Assert.Equal(numUniqueK1Values, numOuterNotifyCollectionChanged);
            // There should be as many inner NotifyCollectionChanged events are there are unique values of K1K2 pairs in the input data.
            Assert.Equal(numUniqueK1K2PairValues, numInnerNotifyCollectionChanged);
            // since the fixture is shared between test, the fixture needs to be cleared
            _fixture.receivedResultsEvents.Clear();
        }
    }

    public class ResultsAndData1Basic : IClassFixture<CORHelpersFirst>
    {
        CORHelpersFirst _fixture;
        readonly ITestOutputHelper output;

        public ResultsAndData1Basic(ITestOutputHelper output, CORHelpersFirst cORHelpers)
        {
            this.output = output;
            this._fixture = cORHelpers;
        }

        [Theory]
        [InlineData("k1=1,k2=1,c1=1,1.11;")]
        [InlineData("k1=1,k2=1,c1=1,1.11;k1=1,k2=2,c1=1,1.21;")]
        [InlineData("k1=2,k2=2,c1=1,2.21;k1=2,k2=1,c1=1,2.11;k1=1,k2=1,c1=1,1.11;k1=1,k2=2,c1=1,1.21;")]
        public void CORViaRoutedDataFlowBlock(string testInStr)
        {
            // since the receivedResultsEvents list in the fixture is shared between test, the list needs to be cleared
            _fixture.receivedResultsEvents.Clear();
            // Create the Results CODict with the specified event handlers
            using (WithObservableConcurrentDictionaryAndEventHandlers withObservableConcurrentDictionaryAndEventHandlers = new WithObservableConcurrentDictionaryAndEventHandlers(_fixture.onNotifyOuterCollectionChanged,
                    _fixture.onNotifyNestedCollectionChanged))
            {
                // declare this RegEx outside the transform block so it only will be compiled once
                var REinner = new Regex("(?<k1>.*?),(?<k2>.*?),(?<c1>.*?),(?<hr>.*?);");
                // create the individual results via a transform block
                // the output is k1, k2, c1, bool, and the output is routed on the bool value
                var Accept1 = new TransformBlock<string, (string k1, string k2, string c1, double hr, bool isReadyToCalculate)>(_input =>
               {
                   var match = REinner.Match(_input);
                   if (match.Success)
                   {
                       var outtup = (
                           match.Groups["k1"].Value,
                                     match.Groups["k2"].Value, match.Groups["c1"].Value,
                                     double.Parse(match.Groups["hr"].Value), true);
                       return outtup;
                   }
                   throw new ArgumentException($"{_input} does not match the needed input pattern");
               });

                // this block takes in a tuple that is isReadyToCalculate, and calculates pr
                var calculateResults = new TransformBlock<(string k1, string k2, string c1, double hr, bool isReadyToCalculate), (string k1, string k2, decimal pr)>(_input =>
               {

                   return (_input.k1, _input.k2, Decimal.Parse(_input.hr.ToString()));
               });
                // populate the Results via an actionBlock
                // This is a trivial ActionBlock
                var populateResults = new ActionBlock<(string k1, string k2, decimal pr)>(_input =>
                {
                    withObservableConcurrentDictionaryAndEventHandlers.RecordCalculatedResults(_input.k1, _input.k2, _input.pr);
                });

                // Link Accept1 to 
                Accept1.LinkTo(calculateResults, mc => mc.isReadyToCalculate);
                //Link calculateResults to populateResults
                calculateResults.LinkTo(populateResults);

                // Link together the completion/continuation tasks
                Accept1.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted) ((IDataflowBlock)calculateResults).Fault(t.Exception);
                    else calculateResults.Complete();
                });
                calculateResults.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted) ((IDataflowBlock)populateResults).Fault(t.Exception);
                    else populateResults.Complete();
                });

                // Split the testInStr string on the ;, and send each substring into the head of the pipeline
                var REouter = new Regex("(?<oneTuple>.*?;)");
                var matchOuter = REouter.Match(testInStr);
                while (matchOuter.Success)
                {
                    // no error checking
                    Accept1.Post(matchOuter.Groups["oneTuple"].Value);
                    matchOuter = matchOuter.NextMatch();
                }

                // inform the head that there is no more data
                Accept1.Complete();

                // wait for the tail of the pipeline to indicate completion
                populateResults.Completion.Wait();
            } // the COD will be disposed of at this point
            //Ensure events have a chance to propagate
            Task.Delay(100);
            // send the observed events to test output
            _fixture.receivedResultsEvents.Keys.OrderBy(x => x)
                .ToList()
                .ForEach(x => output.WriteLine($"{x} : {_fixture.receivedResultsEvents[x]}"));
            // Count the number of inner and outer CollectionChanged events that occurred
            var numInnerNotifyCollectionChanged = _fixture.receivedResultsEvents.Keys.Where(x => x.Contains("Event: NotifyNestedCollectionChanged"))
                                                      .ToList()
                                                      .Count;
            var numOuterNotifyCollectionChanged = _fixture.receivedResultsEvents.Keys.Where(x => x.Contains("Event: NotifyOuterCollectionChanged"))
                                                      .ToList()
                                                      .Count;
            // find the number of unique values of K1 and the number of k1k2 pairs in the test's input data
            // There should be as many outer NotifyCollectionChanged events are there are unique values of K1 in the input data.
            // There should be as many inner NotifyCollectionChanged events are there are unique values of K1K2 pairs in the input data.
            var matchUniqueKValues = new Regex("(?<k1>.*?),(?<k2>.*?),.*?;").Match(testInStr);
            var uniqueK1Values = new HashSet<string>();
            var uniqueK1K2PairValues = new HashSet<string>();
            while (matchUniqueKValues.Success)
            {
                // Nice thing about HashSets is, they won't complain if you try to add a duplicate
                uniqueK1Values.Add(matchUniqueKValues.Groups["k1"].Value);
                uniqueK1K2PairValues.Add(matchUniqueKValues.Groups["k1"].Value + matchUniqueKValues.Groups["k2"].Value);
                matchUniqueKValues = matchUniqueKValues.NextMatch();
            }
            // number of unique values of K1 in the test's input data
            var numUniqueK1Values = uniqueK1Values.Count;
            // number of unique values of K1K2 pairs in the test's input data
            var numUniqueK1K2PairValues = uniqueK1K2PairValues.Count;
            // There should be as many outer NotifyCollectionChanged events are there are unique values of K1 in the input data.
            Assert.Equal(numUniqueK1Values, numOuterNotifyCollectionChanged);
            // There should be as many inner NotifyCollectionChanged events are there are unique values of K1K2 pairs in the input data.
            Assert.Equal(numUniqueK1K2PairValues, numInnerNotifyCollectionChanged);
            // since the fixture is shared between test, the fixture needs to be cleared
            _fixture.receivedResultsEvents.Clear();
        }
    }

    // Extend the collection fixture to support Data1
    public class CORHelpersData1 : CORHelpersFirst, IDisposable
    {

        // create a ConcurrentDictionary to hold the information written by the event handlers
        public ConcurrentDictionary<string, string> receivedData1Events = new ConcurrentDictionary<string, string>();

        // These event handler will be attached/detached from the Data1Dictionary via that class' constructor and dispose method
        public void onNotifyData1CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            receivedData1Events[Message("Data1", e)] = DateTime.Now.ToLongTimeString();
        }

        public void onData1PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            receivedData1Events[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }
        //ToDo ensure that the dispose for the Data1 dictionary is called so that the event handlers are deregistered

    }
    // here we start using a shorter name, RecordResults, for the method

}