using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Goodreads.Rss
{
    public class GoodreadsRssImportListParser : IParseImportListResponse
    {
        protected readonly Logger _logger;
        private ImportListResponse _importListResponse;

        public GoodreadsRssImportListParser()
        {
            _logger = NzbDroneLogger.GetLogger(this);
        }

        public IList<ImportListItemInfo> ParseResponse(ImportListResponse importListResponse)
        {
            _importListResponse = importListResponse;

            var items = new List<ImportListItemInfo>();

            if (!PreProcess(_importListResponse))
            {
                return items;
            }

            var document = LoadXmlDocument(_importListResponse);
            var rssItems = GetItems(document).ToList();

            foreach (var item in rssItems)
            {
                try
                {
                    var itemInfo = ProcessItem(item);
                    if (itemInfo != null)
                    {
                        items.Add(itemInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "An error occurred while processing RSS feed item");
                }
            }

            return items;
        }

        protected virtual bool PreProcess(ImportListResponse importListResponse)
        {
            if (importListResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(importListResponse, "Import List API call resulted in an unexpected StatusCode [{0}]", importListResponse.HttpResponse.StatusCode);
            }

            return true;
        }

        protected virtual XDocument LoadXmlDocument(ImportListResponse importListResponse)
        {
            try
            {
                var content = importListResponse.Content;

                using (var xmlTextReader = XmlReader.Create(new StringReader(content), new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true }))
                {
                    return XDocument.Load(xmlTextReader);
                }
            }
            catch (XmlException ex)
            {
                var contentSample = importListResponse.Content.Substring(0, Math.Min(importListResponse.Content.Length, 512));
                _logger.Debug("Truncated response content (originally {0} characters): {1}", importListResponse.Content.Length, contentSample);

                throw new ImportListException(importListResponse, "XML parsing failed: {0}", ex.Message);
            }
        }

        protected IEnumerable<XElement> GetItems(XDocument document)
        {
            var root = document.Root;

            if (root == null)
            {
                return Enumerable.Empty<XElement>();
            }

            var channel = root.Element("channel");

            if (channel == null)
            {
                return Enumerable.Empty<XElement>();
            }

            return channel.Elements("item");
        }

        protected virtual ImportListItemInfo ProcessItem(XElement item)
        {
            // Goodreads RSS namespace
            XNamespace gr = "http://www.goodreads.com/rss/";

            var title = item.Element("title")?.Value;
            var authorName = item.Element(gr + "author_name")?.Value;
            var bookId = item.Element(gr + "book_id")?.Value;

            // Extract book title from the title field
            // Goodreads RSS title format is typically: "Book Title (Author Name)"
            var bookTitle = title;
            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(authorName))
            {
                // Try to remove author name from title if it's in parentheses
                var authorInParens = $"({authorName})";
                if (title.Contains(authorInParens))
                {
                    bookTitle = title.Replace(authorInParens, "").Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(bookTitle) && string.IsNullOrWhiteSpace(authorName))
            {
                return null;
            }

            var itemInfo = new ImportListItemInfo
            {
                Book = bookTitle?.CleanSpaces(),
                Author = authorName?.CleanSpaces()
            };

            // Add Goodreads ID if available
            if (!string.IsNullOrWhiteSpace(bookId))
            {
                itemInfo.EditionGoodreadsId = bookId;
            }

            return itemInfo;
        }
    }
}
