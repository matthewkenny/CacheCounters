CacheCounters
=============

CacheCounters is a simple Sitecore `Hook` that periodically polls the Sitecore caches and reports their status (size, number of items, maximum size) using several Windows performance counters.

Note that this solution requires that the performance counters be created beforehand - it will not attempt to create them itself as website users typically have insufficient permissions to do this anyway.