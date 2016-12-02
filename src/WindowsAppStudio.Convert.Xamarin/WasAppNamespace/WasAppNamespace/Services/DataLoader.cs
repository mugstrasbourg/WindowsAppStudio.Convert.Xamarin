using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using AppStudio.DataProviders;

#if UWP
using AppStudio.Uwp.Cache;
using AppStudio.Uwp.Services;
#else
using AppStudio.Xamarin.Cache;
using AppStudio.Xamarin.Services;
#endif

using WasAppNamespace.Sections;

namespace WasAppNamespace.Services
{
    class LoaderSettings
    {
        public string CacheKey { get; set; }
        public TimeSpan CacheExpiration { get; set; }
        public bool NeedsNetwork { get; set; }
        public bool UseStorage { get; set; }
        public bool ForceRefresh { get; set; }

        public static LoaderSettings FromSection<T>(Section<T> section, string cacheKey, bool forceRefresh) where T : SchemaBase
        {
            if (section == null)
            {
                throw new ArgumentNullException("section");
            }
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentNullException("cacheKey");
            }

            return new LoaderSettings
            {
                CacheKey = cacheKey,
                CacheExpiration = section.CacheExpiration,
                NeedsNetwork = section.NeedsNetwork,
                UseStorage = section.NeedsNetwork,
                ForceRefresh = forceRefresh
            };
        }

        public static LoaderSettings NoCache(bool needsNetwork)
        {
            return new LoaderSettings
            {
                CacheKey = null,
                CacheExpiration = new TimeSpan(),
                NeedsNetwork = needsNetwork,
                UseStorage = needsNetwork,
                ForceRefresh = true
            };
        }
    }

    class LoaderOutcome
    {
        public DateTime? Timestamp { get; set; }
        public bool IsFreshData { get; set; }
    }

    static class DataLoader
    {
        public static async Task<LoaderOutcome> LoadAsync<T>(LoaderSettings settings, Func<Task<IEnumerable<T>>> loadFunc, Action<IEnumerable<T>> parseItems) where T : SchemaBase
        {
            var outcome = new LoaderOutcome();

            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }
            if (loadFunc == null)
            {
                throw new ArgumentNullException("loadFunc");
            }
            if (parseItems == null)
            {
                throw new ArgumentNullException("parseItems");
            }

            CachedContent<T> dataInCache = null;

            if (!string.IsNullOrEmpty(settings.CacheKey))
            {
                dataInCache = await AppCache.GetItemsAsync<T>(settings.CacheKey);
                if (dataInCache != null)
                {
                    parseItems(dataInCache.Items);
                }
            }

            if (CanPerformLoad<T>(settings.NeedsNetwork) && (settings.ForceRefresh || DataNeedToBeUpdated(dataInCache, settings.CacheExpiration)))
            {
                dataInCache = dataInCache ?? new CachedContent<T>();

                outcome.IsFreshData = true;
                dataInCache.Timestamp = DateTime.Now;
                dataInCache.Items = await loadFunc();

                if (!string.IsNullOrEmpty(settings.CacheKey))
                {
                    await AppCache.AddItemsAsync(settings.CacheKey, dataInCache, settings.UseStorage);
                }

                parseItems(dataInCache.Items);
            }
            outcome.Timestamp = dataInCache?.Timestamp;
            return outcome;
        }

        private static bool CanPerformLoad<T>(bool needNetwork)
        {
#if UWP
            return !needNetwork || InternetConnection.IsInternetAvailable();
#else
            return true;
#endif
        }

        private static bool DataNeedToBeUpdated<T>(CachedContent<T> dataInCache, TimeSpan expiration)
        {
            return dataInCache == null || (DateTime.Now - dataInCache.Timestamp > expiration);
        }
    }
}
