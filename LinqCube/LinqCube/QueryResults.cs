﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dasz.LinqCube
{
    public class QueryResult : Dictionary<IDimension, IDimensionResult>
    {
        public QueryResult()
        {
        }

        public IDimensionResult this[IDimensionEntry key]
        {
            get
            {
                return this[key.Root][key];
            }
        }
    }

    public interface IDimensionResult
    {
        DimensionResultEntriesDictionary Entries { get; }
        DimensionResultOtherDimensionsDictionary OtherDimensions { get; }

        IDimensionResult this[IDimension key] { get; }
        IDimensionEntryResult this[IDimensionEntry key] { get; }
        IDimensionEntryResult this[string key] { get; }
    }

    public interface IDimensionEntryResult : IDimensionResult
    {
        MeasureResultDictionary Values { get; }
    }

    public class DimensionResult<TFact> : IDimensionResult
    {
        public DimensionResult(IQueryDimension dim, IEnumerable<IMeasure> measures)
        {
            QueryDimension = dim;
            Entries = new DimensionResultEntriesDictionary();
            OtherDimensions = new DimensionResultOtherDimensionsDictionary();
            Measures = measures;
        }

        public IQueryDimension QueryDimension { get; private set; }
        public DimensionResultEntriesDictionary Entries { get; private set; }
        public DimensionResultOtherDimensionsDictionary OtherDimensions { get; private set; }
        public IEnumerable<IMeasure> Measures { get; private set; }

        public void Initialize(IEnumerable<IQueryDimension> others)
        {
            foreach (var child in QueryDimension.Dimension.Children)
            {
                var result = new DimensionEntryResult<TFact>(child, Measures);
                Entries[child] = result;
                result.Initialize(others);
            }
        }

        public IDimensionResult this[IDimension key]
        {
            get
            {
                return OtherDimensions[key];
            }
        }

        public IDimensionEntryResult this[string key]
        {
            get
            {
                return Entries[key];
            }
        }

        public IDimensionEntryResult this[IDimensionEntry key]
        {
            get
            {
                IDimensionEntryResult result;
                if (Entries.TryGetValue(key, out result))
                {
                    return result;
                }
                else
                {
                    if (key.Parent == null) throw new ArgumentOutOfRangeException("key", "key does not match dimension");
                    return this[key.Parent][key];
                }
            }
        }
    }

    public class DimensionEntryResult<TFact> : IDimensionEntryResult
    {
        public DimensionEntryResult(IDimensionEntry e, IEnumerable<IMeasure> measures)
        {
            Entry = e;
            Entries = new DimensionResultEntriesDictionary();
            OtherDimensions = new DimensionResultOtherDimensionsDictionary();
            Measures = measures;
            Values = new MeasureResultDictionary();
        }

        public IDimensionEntry Entry { get; private set; }
        public DimensionResultEntriesDictionary Entries { get; private set; }
        public DimensionResultOtherDimensionsDictionary OtherDimensions { get; private set; }
        public MeasureResultDictionary Values { get; private set; }
        public IEnumerable<IMeasure> Measures { get; private set; }

        public void Initialize(IEnumerable<IQueryDimension> others)
        {
            foreach (var child in Entry.Children)
            {
                var result = new DimensionEntryResult<TFact>(child, Measures);
                Entries[child] = result;
                result.Initialize(others);
            }

            foreach (var other in others)
            {
                var otherResult = new DimensionResult<TFact>(other, Measures);
                OtherDimensions[other] = otherResult;
                otherResult.Initialize(others.Where(i => i != other));
            }

            foreach (var measure in Measures)
            {
                Values[measure] = measure.CreateResult();
            }
        }

        public IDimensionResult this[IDimension key]
        {
            get
            {
                return OtherDimensions[key];
            }
        }

        public IDimensionEntryResult this[string key]
        {
            get
            {
                return Entries[key];
            }
        }

        public IDimensionEntryResult this[IDimensionEntry key]
        {
            get
            {
                IDimensionEntryResult result;
                if (Entries.TryGetValue(key, out result))
                {
                    return result;
                }
                else
                {
                    if (key.Parent != null)
                    {
                        return this[key.Parent][key];
                    }
                    else
                    {
                        foreach (var dim in OtherDimensions)
                        {
                            if (dim.Key.Dimension == key.Root)
                            {
                                return dim.Value[key];
                            }
                        }
                    }

                    throw new ArgumentOutOfRangeException("key", "key does not match dimension");
                }
            }
        }
    }
}
