using System.Collections.Generic;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.Goodreads.Rss
{
    public class GoodreadsRssImportListRequestGenerator : IImportListRequestGenerator
    {
        public GoodreadsRssImportListSettings Settings { get; set; }

        public int MaxPages { get; set; }
        public int PageSize { get; set; }

        public GoodreadsRssImportListRequestGenerator()
        {
            MaxPages = 1;
            PageSize = 100; // Goodreads RSS feeds are limited to 100 items
        }

        public virtual ImportListPageableRequestChain GetListItems()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetPagedRequests());

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetPagedRequests()
        {
            yield return new ImportListRequest(Settings.RssUrl, HttpAccept.Rss);
        }
    }
}
