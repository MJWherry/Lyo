using Lyo.Metrics;
using Lyo.Metrics.Models;

namespace Lyo.Cache.Tests;

public static class TestModels
{
    internal class TestEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    internal class TestProduct
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }

    internal class TestOrder
    {
        public int Id { get; set; }

        public DateTime OrderDate { get; set; }
    }

    internal class TestUnconfiguredType
    {
        public int Id { get; set; }
    }

    internal class TestMetrics : IMetrics
    {
        public List<(string Name, long Value, Dictionary<string, string>? Tags)> HitCounters { get; } = new();

        public List<(string Name, TimeSpan Duration, Dictionary<string, string>? Tags)> HitTimings { get; } = new();

        public List<(string Name, long Value, Dictionary<string, string>? Tags)> MissCounters { get; } = new();

        public List<(string Name, TimeSpan Duration, Dictionary<string, string>? Tags)> MissTimings { get; } = new();

        public List<(string Name, long Value, Dictionary<string, string>? Tags)> SetCounters { get; } = new();

        public List<(string Name, TimeSpan Duration, Dictionary<string, string>? Tags)> SetTimings { get; } = new();

        public List<(string Name, long Value, Dictionary<string, string>? Tags)> RemoveCounters { get; } = new();

        public List<(string Name, TimeSpan Duration, Dictionary<string, string>? Tags)> RemoveTimings { get; } = new();

        public List<(string Name, long Value, Dictionary<string, string>? Tags)> RemoveByTagCounters { get; } = new();

        public List<(string Name, TimeSpan Duration, Dictionary<string, string>? Tags)> RemoveByTagTimings { get; } = new();

        public List<(string Name, double Value, Dictionary<string, string>? Tags)> RemoveByTagGauges { get; } = new();

        public List<(string Name, Exception Exception, Dictionary<string, string>? Tags)> Errors { get; } = new();

        public List<(string Name, double Value, Dictionary<string, string>? Tags)> CacheSizeGauges { get; } = new();

        public void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
        {
            var counterValue = value != null ? Convert.ToInt64(value) : 1L;
            var dictTags = tags?.ToDictionary(t => t.Item1, t => t.Item2);
            if (name == Constants.Metrics.HitSuccess)
                HitCounters.Add((name, counterValue, dictTags));
            else if (name == Constants.Metrics.MissSuccess)
                MissCounters.Add((name, counterValue, dictTags));
            else if (name == Constants.Metrics.SetSuccess)
                SetCounters.Add((name, counterValue, dictTags));
            else if (name == Constants.Metrics.RemoveSuccess)
                RemoveCounters.Add((name, counterValue, dictTags));
            else if (name == Constants.Metrics.RemoveByTagSuccess)
                RemoveByTagCounters.Add((name, counterValue, dictTags));
        }

        public void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
        {
            // Not used in cache metrics
        }

        public void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null)
        {
            var gaugeValue = Convert.ToDouble(value);
            var dictTags = tags?.ToDictionary(t => t.Item1, t => t.Item2);
            if (name == Constants.Metrics.CacheSize)
                CacheSizeGauges.Add((name, gaugeValue, dictTags));
            else if (name == Constants.Metrics.RemoveByTagItemsRemoved)
                RemoveByTagGauges.Add((name, gaugeValue, dictTags));
        }

        public void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null)
        {
            // Not used in cache metrics
        }

        public void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null)
        {
            var dictTags = tags?.ToDictionary(t => t.Item1, t => t.Item2);
            if (name == Constants.Metrics.HitDuration)
                HitTimings.Add((name, duration, dictTags));
            else if (name == Constants.Metrics.MissDuration)
                MissTimings.Add((name, duration, dictTags));
            else if (name == Constants.Metrics.SetDuration)
                SetTimings.Add((name, duration, dictTags));
            else if (name == Constants.Metrics.RemoveDuration)
                RemoveTimings.Add((name, duration, dictTags));
            else if (name == Constants.Metrics.RemoveByTagDuration)
                RemoveByTagTimings.Add((name, duration, dictTags));
        }

        public MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null) => new(new(this, name, tags));

        public void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null)
        {
            var dictTags = tags?.ToDictionary(t => t.Item1, t => t.Item2);
            Errors.Add((name, exception, dictTags));
        }

        public void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
        {
            // Not used in cache metrics
        }
    }
}