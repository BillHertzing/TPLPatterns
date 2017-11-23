using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using Swordfish.NET.Collections;

namespace TPLPatternExamples {
    public class TPLPatterns {
        public ConcurrentDictionary<string, ConcurrentDictionary<string, decimal>> calculatedResults;

        public TPLPatterns() {
            calculatedResults = new ConcurrentDictionary<string, ConcurrentDictionary<string, decimal>>();
        }

        public void RecordCalculatedResults(string k1, string k2, decimal pr) {
            if(calculatedResults.ContainsKey(k1)) {
                var innerDictionary = calculatedResults[k1];
                if(innerDictionary.ContainsKey(k2)) {
                    throw new NotSupportedException("This pattern expects only one entry per k1k2 pair");
                }
                else {
                    if(!innerDictionary.TryAdd(k2, pr)) {
                        throw new Exception($"adding {pr} to {k1}'s innerDictionary keyed by {k2} failed");
                    }
                }
            }
            else {
                var innerDictionary = new ConcurrentDictionary<string, decimal>();
                if(!innerDictionary.TryAdd(k2, pr)) {
                    throw new Exception($"adding {pr} to the new innerDictionary keyed by {k2} failed");
                }

                if(!calculatedResults.TryAdd(k1, innerDictionary)) {
                    throw new Exception($"adding the new innerDictionary to cODResults keyed by {k1} failed");
                }
            };
        }
    }

    public class WithConcurrentObservableDictionary {
        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> calculatedResults;

        public WithConcurrentObservableDictionary() {
            calculatedResults = new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>();
        }

        public void RecordCalculatedResults(string k1, string k2, decimal pr) {
            if(calculatedResults.ContainsKey(k1)) {
                var innerDictionary = calculatedResults[k1];
                if(innerDictionary.ContainsKey(k2)) {
                    throw new NotSupportedException("This pattern expects only one entry per k1k2 pair");
                }
                else {
                    //ToDo: Better understanding/handling of exceptions here
                    try
                    {
                        innerDictionary.Add(k2, pr);

                    }
                    catch { new Exception($"adding {pr} to {k1}'s innerDictionary keyed by {k2} failed"); }
                }
            }
            else {
                var innerDictionary = new ConcurrentObservableDictionary<string, decimal>();
                try { innerDictionary.Add(k2, pr); } catch { new Exception($"adding {pr} to the new innerDictionary keyed by {k2} failed"); }
                try { calculatedResults.Add(k1, innerDictionary); } catch { new Exception($"adding the new innerDictionary to cODResults keyed by {k1} failed"); }
            };
        }
    }

    public class WithObservableConcurrentDictionaryAndEventHandlers : IDisposable {
        NotifyCollectionChangedEventHandler onCollectionChanged;
        NotifyCollectionChangedEventHandler onNestedCollectionChanged;
        PropertyChangedEventHandler onNestedPropertyChanged;
        PropertyChangedEventHandler onPropertyChanged;
        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> calculatedResults;

        public WithObservableConcurrentDictionaryAndEventHandlers(NotifyCollectionChangedEventHandler OnCollectionChanged, PropertyChangedEventHandler OnPropertyChanged, NotifyCollectionChangedEventHandler OnNestedCollectionChanged, PropertyChangedEventHandler OnNestedPropertyChanged) {
            calculatedResults = new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>();
            this.onCollectionChanged = OnCollectionChanged;
            this.onPropertyChanged = OnPropertyChanged;
            this.onNestedCollectionChanged = OnNestedCollectionChanged;
            this.onNestedPropertyChanged = OnNestedPropertyChanged;
            calculatedResults.CollectionChanged += onCollectionChanged;
            calculatedResults.PropertyChanged += onPropertyChanged;
        }
        public WithObservableConcurrentDictionaryAndEventHandlers(NotifyCollectionChangedEventHandler OnCollectionChanged,  NotifyCollectionChangedEventHandler OnNestedCollectionChanged)
        {
            calculatedResults = new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>();
            this.onCollectionChanged = OnCollectionChanged;
            this.onNestedCollectionChanged = OnNestedCollectionChanged;
            calculatedResults.CollectionChanged += onCollectionChanged;
        }

        public void RecordCalculatedResults(string k1, string k2, decimal pr) {
            if(calculatedResults.ContainsKey(k1)) {
                var innerDictionary = calculatedResults[k1];
                if(innerDictionary.ContainsKey(k2)) {
                    throw new NotSupportedException("This pattern expects only one entry per k1k2 pair");
                }
                else {
                    //ToDo: Better understanding/handling of exceptions here
                    try { innerDictionary.Add(k2, pr); } catch { new Exception($"adding {pr} to {k1}'s innerDictionary keyed by {k2} failed"); }
                }
            }
            else {
                var innerDictionary = new ConcurrentObservableDictionary<string, decimal>();
                if (this.onNestedCollectionChanged != null) innerDictionary.CollectionChanged += this.onNestedCollectionChanged;
                if (this.onNestedPropertyChanged != null) innerDictionary.PropertyChanged += this.onNestedPropertyChanged;
                try { innerDictionary.Add(k2, pr); } catch { new Exception($"adding {pr} to the new innerDictionary keyed by {k2} failed"); }
                try { calculatedResults.Add(k1, innerDictionary); } catch { new Exception($"adding the new innerDictionary to cODResults keyed by {k1} failed"); }
            };
        }

        #region IDisposable Support
        public void TearDown() {
            calculatedResults.CollectionChanged -= onCollectionChanged;
            calculatedResults.PropertyChanged -= onPropertyChanged;
            var enumerator = calculatedResults.Keys.GetEnumerator();
            try
            {
                while(enumerator.MoveNext()) {
                    var key = enumerator.Current;
                    calculatedResults[key].CollectionChanged -= this.onNestedCollectionChanged;
                    calculatedResults[key].PropertyChanged -= this.onNestedPropertyChanged;
                }
            }
            finally
            {
                enumerator.Dispose();
            }
        }

        bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if(!disposedValue) {
                if(disposing) {
                    // dispose managed state (managed objects).
                    TearDown();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WithObservableConcurrentDictionaryAndEventHandlers() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }
        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
        }
        #endregion
    }
    public class WithCODResultsAndOneCODData : IDisposable
    {
        NotifyCollectionChangedEventHandler onResultsCollectionChanged;
        NotifyCollectionChangedEventHandler onResultsNestedCollectionChanged;
        PropertyChangedEventHandler onResultsNestedPropertyChanged;
        PropertyChangedEventHandler onResultsCollectionPropertyChanged;
        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> cODResults;
        NotifyCollectionChangedEventHandler onData1CollectionChanged;
        PropertyChangedEventHandler onData1PropertyChanged;
        public ConcurrentObservableDictionary<string,  double> cODData1;

        public WithCODResultsAndOneCODData(NotifyCollectionChangedEventHandler OnResultsCollectionChanged, PropertyChangedEventHandler OnResultsCollectionPropertyChanged, NotifyCollectionChangedEventHandler OnResultsNestedCollectionChanged, PropertyChangedEventHandler OnNestedPropertyChanged, NotifyCollectionChangedEventHandler OnData1CollectionChanged, PropertyChangedEventHandler OnData1PropertyChanged)
        {
            cODResults = new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>();
            this.onResultsCollectionChanged = OnResultsCollectionChanged;
            this.onResultsCollectionPropertyChanged = OnResultsCollectionPropertyChanged;
            this.onResultsNestedCollectionChanged = OnResultsNestedCollectionChanged;
            this.onResultsNestedPropertyChanged = OnNestedPropertyChanged;
            cODResults.CollectionChanged += onResultsCollectionChanged;
            cODResults.PropertyChanged += onResultsCollectionPropertyChanged;
            cODData1 = new ConcurrentObservableDictionary<string, double>();
            this.onData1CollectionChanged = OnData1CollectionChanged;
            this.onData1PropertyChanged = OnData1PropertyChanged;
            cODData1.CollectionChanged += OnData1CollectionChanged;
            cODData1.PropertyChanged += OnData1PropertyChanged;
        }
        public WithCODResultsAndOneCODData(NotifyCollectionChangedEventHandler OnResultsCollectionChanged, NotifyCollectionChangedEventHandler OnResultsNestedCollectionChanged, NotifyCollectionChangedEventHandler OnData1CollectionChanged)
        {
            cODResults = new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>();
            this.onResultsCollectionChanged = OnResultsCollectionChanged;
            this.onResultsNestedCollectionChanged = OnResultsNestedCollectionChanged;
            cODResults.CollectionChanged += onResultsCollectionChanged;
            cODData1 = new ConcurrentObservableDictionary<string, double>();
            this.onData1CollectionChanged = OnData1CollectionChanged;
            cODData1.CollectionChanged += OnData1CollectionChanged;
        }

        public void RecordResults(string k1, string k2, decimal pr)
        {
            if (cODResults.ContainsKey(k1))
            {
                var innerDictionary = cODResults[k1];
                if (innerDictionary.ContainsKey(k2))
                {
                    throw new NotSupportedException("This pattern expects only one entry per k1k2 pair");
                }
                else
                {
                    //ToDo: Better understanding/handling of exceptions here
                    try { innerDictionary.Add(k2, pr); } catch { new Exception($"adding {pr} to {k1}'s innerDictionary keyed by {k2} failed"); }
                }
            }
            else
            {
                var innerDictionary = new ConcurrentObservableDictionary<string, decimal>();
                if (this.onResultsNestedCollectionChanged != null) innerDictionary.CollectionChanged += this.onResultsNestedCollectionChanged;
                if (this.onResultsNestedPropertyChanged != null) innerDictionary.PropertyChanged += this.onResultsNestedPropertyChanged;
                try { innerDictionary.Add(k2, pr); } catch { new Exception($"adding {pr} to the new innerDictionary keyed by {k2} failed"); }
                try { cODResults.Add(k1, innerDictionary); } catch { new Exception($"adding the new innerDictionary to cODResults keyed by {k1} failed"); }
            };
        }

        #region IDisposable Support
        public void TearDown()
        {
            cODResults.CollectionChanged -= onResultsCollectionChanged;
            cODResults.PropertyChanged -= onResultsCollectionPropertyChanged;
            var enumerator = cODResults.Keys.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var key = enumerator.Current;
                    cODResults[key].CollectionChanged -= this.onResultsNestedCollectionChanged;
                    cODResults[key].PropertyChanged -= this.onResultsNestedPropertyChanged;
                }
            }
            finally
            {
                enumerator.Dispose();
            }
            cODData1.CollectionChanged -= onData1CollectionChanged;
            cODData1.PropertyChanged -= onData1PropertyChanged;
        }

        bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    TearDown();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Not Needed
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WithCODResultsAndOneCODData() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}