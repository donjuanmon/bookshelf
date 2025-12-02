using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.Goodreads.Rss
{
    public class GoodreadsRssImportList : HttpImportListBase<GoodreadsRssImportListSettings>
    {
        public override string Name => "Goodreads RSS";

        public override ImportListType ListType => ImportListType.Goodreads;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);
        public override int PageSize => 100; // Goodreads RSS feeds are limited to 100 items

        public GoodreadsRssImportList(IHttpClient httpClient,
            IImportListStatusService importListStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new GoodreadsRssImportListRequestGenerator { Settings = Settings };
        }

        public override IParseImportListResponse GetParser()
        {
            return new GoodreadsRssImportListParser();
        }
    }
}
