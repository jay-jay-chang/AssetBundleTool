1.support unity editor version 5.4 or later
2.provide two mode : native(default) and local specify
3.when use native mode, please remember to set Caching property:
-->Caching.compressionEnabled = true;
-->Caching.maximumAvailableDiskSpace = (number of MB) * 1024 * 1024; //should bigger than your total assetbundle size
