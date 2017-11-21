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
                    throw new Exception($"adding the new innerDictionary to calculatedResults keyed by {k1} failed");
                }
            };
        }
    }

    public class WithObservableConcurrentDictionary {
        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> calculatedResults;

        public WithObservableConcurrentDictionary() {
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
                try { calculatedResults.Add(k1, innerDictionary); } catch { new Exception($"adding the new innerDictionary to calculatedResults keyed by {k1} failed"); }
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
                try { calculatedResults.Add(k1, innerDictionary); } catch { new Exception($"adding the new innerDictionary to calculatedResults keyed by {k1} failed"); }
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
}