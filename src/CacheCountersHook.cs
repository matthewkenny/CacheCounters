﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using Sitecore.Caching;
using Sitecore.Events.Hooks;

namespace CacheCounters
{
    /// <summary>
    /// Sitecore hook to periodically record cache status to
    /// the Windows performance counters.
    /// </summary>
    public class CacheCountersHook : IHook
    {
        private const string CounterCategoryName = "CacheCounters";

        /// <summary>
        /// Gets or sets a value indicating whether to sample the cache item counts.
        /// </summary>
        public bool SampleCount { get; set; }

        /// <summary>
        /// Gets or sets the sampling interval in milliseconds.
        /// </summary>
        public int SampleInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to sample the cache max sizes.
        /// </summary>
        public bool SampleMaxSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to sample the cache sizes.
        /// </summary>
        public bool SampleSize { get; set; }

        private readonly Dictionary<Tuple<string, string>, PerformanceCounter> _performanceCounterCache = new Dictionary<Tuple<string, string>, PerformanceCounter>();

        private readonly Timer _timer = new Timer();

        /// <summary>
        /// Entry point to hook.  Verifies that counters exist and initiates timer.
        /// </summary>
        public void Initialize()
        {
            try
            {
                Sitecore.Diagnostics.Log.Info("Initialising CacheCounter hook", this);
                if (!PerformanceCounterCategory.Exists(CounterCategoryName))
                {
                    Sitecore.Diagnostics.Log.Warn("Could not initialise CacheCounter hook - counters did not seem to be present.", this);
                }

                _timer.Elapsed += TimerElapsed;
                _timer.AutoReset = false;
                _timer.Start();
            }
            catch (Exception ex)
            {
                _timer.Stop();
                _timer.Elapsed -= TimerElapsed;

                Sitecore.Diagnostics.Log.Error("Initialisation of CacheCounter hook failed - hook has been disabled.", ex, this);
            }
        }

        /// <summary>
        /// Retrieves a reference to a specific performance counter
        /// </summary>
        /// <param name="category">Category name of the counter</param>
        /// <param name="name">Name of the counter</param>
        /// <param name="instanceName">Instance name of the counter</param>
        /// <returns>The performance counter, or <c>null</c> if an error occurred</returns>
        protected virtual PerformanceCounter GetPerformanceCounter(string category, string name, string instanceName)
        {
            // Instantiating PerformanceCounter instances is relatively expensive, so re-use references.
            var tuple = new Tuple<string, string>(name, instanceName);
            if (_performanceCounterCache.ContainsKey(tuple))
            {
                return _performanceCounterCache[tuple];
            }

            try
            {
                var counter = new PerformanceCounter(category, name, instanceName, false);
                _performanceCounterCache.Add(tuple, counter);

                return counter;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (Win32Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Timer event handler
        /// <para>Also called upon counter initialisation, where sender will be <c>this</c>.</para>
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected virtual void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            UpdateCounters();

            _timer.Start();
        }

        /// <summary>
        /// Update a specific counter.
        /// </summary>
        /// <param name="category">Category name of the counter</param>
        /// <param name="name">Name of the counter</param>
        /// <param name="instanceName">Instance name of the counter</param>
        /// <param name="value">New counter value</param>
        protected void UpdateCounter(string category, string name, string instanceName, long value)
        {
            var counter = GetPerformanceCounter(category, name, instanceName);
            if (counter != null)
            {
                counter.RawValue = value;
            }
        }

        /// <summary>
        /// Update the enabled counters for all caches.
        /// </summary>
        protected virtual void UpdateCounters()
        {
            foreach (var cache in CacheManager.GetAllCaches())
            {
                if (SampleCount)
                {
                    UpdateCounter(CounterCategoryName, "CacheCount", cache.Name, cache.Count);
                }

                if (SampleMaxSize)
                {
                    UpdateCounter(CounterCategoryName, "CacheMaxSize", cache.Name, cache.MaxSize);
                }

                if (SampleSize)
                {
                    UpdateCounter(CounterCategoryName, "CacheSize", cache.Name, cache.Size);
                }
            }
        }
    }
}