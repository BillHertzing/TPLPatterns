using System;
using System.Collections.Concurrent;

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
}