using Swordfish.NET.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TPLPatternExamples
{
    public class TPLPatterns
    {
        public ConcurrentDictionary<string, ConcurrentDictionary<string, decimal>> calculatedResults;
        public void RecordCalculatedResults(string k1, string k2, decimal pr)
        {
            if (calculatedResults.ContainsKey(k1)) {
                var innerDictionary = calculatedResults[k1];
                if (innerDictionary.ContainsKey(k2))
                {
                    throw new NotSupportedException("This pattern expects only one entry per k1k2 pair");
                } else
                {
                    if (!innerDictionary.TryAdd(k2, pr)) throw new Exception($"adding {pr} to {k1}'s innerDictionary keyed by {k2} failed");
                }
            } else {
                var innerDictionary = new ConcurrentDictionary<string, decimal>();
                if (!innerDictionary.TryAdd(k2, pr)) throw new Exception($"adding {pr} to the new innerDictionary keyed by {k2} failed");
                if (!calculatedResults.TryAdd(k1, innerDictionary)) throw new Exception($"adding the new innerDictionary to calculatedResults keyed by {k1} failed");
            };
        }
        public TPLPatterns()
        {
            calculatedResults = new ConcurrentDictionary<string, ConcurrentDictionary<string, decimal>>();
        }
    }
    public class WithObservableConcurrentDictionary
    {
        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>  calculatedResults;


        public void RecordCalculatedResults(string k1, string k2, decimal pr)
        {
            if (calculatedResults.ContainsKey(k1))
            {
                var innerDictionary = calculatedResults[k1];
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
                try { innerDictionary.Add(k2, pr); } catch { new Exception($"adding {pr} to the new innerDictionary keyed by {k2} failed"); }
                try { calculatedResults.Add(k1, innerDictionary); } catch { new Exception($"adding the new innerDictionary to calculatedResults keyed by {k1} failed"); }
            };
        }
        public WithObservableConcurrentDictionary()
        {
            calculatedResults = new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>();
        }
    }

    public class WithObservableConcurrentDictionaryAndEventHandlers : IDisposable
    {
        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> calculatedResults;

        NotifyCollectionChangedEventHandler onCollectionChanged;
        PropertyChangedEventHandler onPropertyChanged;
        public void RecordCalculatedResults(string k1, string k2, decimal pr)
        {
            if (calculatedResults.ContainsKey(k1))
            {
                var innerDictionary = calculatedResults[k1];
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
                try { innerDictionary.Add(k2, pr); } catch { new Exception($"adding {pr} to the new innerDictionary keyed by {k2} failed"); }
                try { calculatedResults.Add(k1, innerDictionary); } catch { new Exception($"adding the new innerDictionary to calculatedResults keyed by {k1} failed"); }
            };
        }
        public WithObservableConcurrentDictionaryAndEventHandlers(NotifyCollectionChangedEventHandler OnCollectionChanged, PropertyChangedEventHandler OnPropertyChanged)
        {
            calculatedResults = new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>();
            this.onCollectionChanged = OnCollectionChanged;
            this.onPropertyChanged = OnPropertyChanged;
            calculatedResults.CollectionChanged += this.onCollectionChanged;
            calculatedResults.PropertyChanged += this.onPropertyChanged;
        }

        public void TearDown() { 
            calculatedResults.CollectionChanged -= this.onCollectionChanged;
            calculatedResults.PropertyChanged -= this.onPropertyChanged;
}

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

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