using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Plugins.ADEProvider
{
    public class ADEProvider : BaseMetadataProvider
    {
        private const string BaseUrl = "http://www.adultdvdempire.com/";
        private const string SearchUrl = BaseUrl + "allsearch/search?q={0}";
        private readonly SemaphoreSlim _resourcePool = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;

        public ADEProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IHttpClient httpClient, IProviderManager providerManager) 
            : base(logManager, configurationManager)
        {
            HttpClient = httpClient;
            _providerManager = providerManager;

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }
        
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            BaseProviderInfo data;

            if (!item.ProviderData.TryGetValue(Id, out data))
            {
                data = new BaseProviderInfo();
                item.ProviderData[Id] = data;
            }

            var adeId = item.GetProviderId("AdultDvdEmpire");

            if (!string.IsNullOrEmpty(adeId) && !force)
            {
                data.LastRefreshStatus = ProviderRefreshStatus.Success;
                return true;
            }

            var items = await GetSearchItems(item.Name, cancellationToken);

            if (items == null || !items.Any())
            {
                return false;
            }

            var probableItem = items.FirstOrDefault(x => x.Rank == 1);
            if (probableItem == null)
            {
                return false;
            }

            if (await GetItemDetails(item, probableItem, cancellationToken))
            {
                item.SetProviderId("AdultDvdEmpire", adeId);

                return true;
            }

            return false;
        }

        private async Task<bool> GetItemDetails(BaseItem item, SearchItem probableItem, CancellationToken cancellationToken)
        {
            string html;

            using (var stream = await HttpClient.Get(new HttpRequestOptions
            {
                Url = probableItem.Url,
                CancellationToken = cancellationToken,
                ResourcePool = _resourcePool
            }).ConfigureAwait(false))
            {
                html = stream.ToStringFromStream();
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            var divNodes = doc.DocumentNode.Descendants("div").ToList();

            if (!item.LockedFields.Contains(MetadataFields.Cast))
            {
                GetCast(divNodes, item);
            }

            if (!item.LockedFields.Contains(MetadataFields.Overview))
            {
                GetSynopsisAndTagLine(divNodes, item);
            }

            if (!item.LockedFields.Contains(MetadataFields.Genres))
            {
                GetCategories(divNodes, item);
            }

            GetDetails(divNodes, item);

            GetImages(divNodes, item, cancellationToken);

            item.OfficialRating = "XXX";

            return true;
        }

        private void GetDetails(IEnumerable<HtmlNode> divNodes, BaseItem item)
        {
            var detailsNode = divNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value == "Section ProductInfo");
            if (detailsNode == null)
            {
                return;
            }

            var productHtml = detailsNode.InnerHtml;
            var releasedIndex = productHtml.IndexOf("<strong>Released</strong>", StringComparison.Ordinal);
            if (releasedIndex != -1)
            {
                var released = productHtml.Substring(releasedIndex + "<strong>Released</strong>".Length);
                released = released.Substring(0, released.IndexOf("<br", StringComparison.Ordinal));
                DateTime releasedDate;
                if (DateTime.TryParse(released, out releasedDate))
                {
                    item.PremiereDate = releasedDate;
                }
            }

            var studioNode = detailsNode.Descendants("a").FirstOrDefault();
            if (studioNode != null)
            {
                item.AddStudio(studioNode.InnerText.Trim());
            }
        }

        private void GetImages(IEnumerable<HtmlNode> divNodes, BaseItem item, CancellationToken cancellationToken)
        {
            var boxCoverNode = divNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["id"] != null && x.Attributes["id"].Value == "Boxcover");
            if (boxCoverNode == null || !boxCoverNode.ChildNodes.Any())
            {
                return;
            }

            var frontCover = boxCoverNode.ChildNodes[0];
            var frontCoverUrl = GetHref(frontCover);
            if (!string.IsNullOrEmpty(frontCoverUrl))
            {
                _providerManager.SaveImage(item, frontCoverUrl, _resourcePool, ImageType.Primary, null, cancellationToken).ConfigureAwait(false);
            }

            if (boxCoverNode.ChildNodes.Count > 1)
            {
                var backCover = boxCoverNode.ChildNodes[1];
                var backCoverUrl = GetHref(backCover);

                if (!string.IsNullOrEmpty(backCoverUrl))
                {
                    _providerManager.SaveImage(item, backCoverUrl, _resourcePool, ImageType.BoxRear, null, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private string GetHref(HtmlNode frontCover)
        {
            if (frontCover.HasAttributes && frontCover.Attributes["href"] != null)
            {
                return frontCover.Attributes["href"].Value;
            }

            return string.Empty;
        }

        private void GetCategories(IEnumerable<HtmlNode> divNodes, BaseItem item)
        {
            var categoriesNode = divNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value == "Section Categories");
            if (categoriesNode == null)
            {
                return;
            }

            var cats = categoriesNode.Descendants("p").FirstOrDefault();
            if (cats == null)
            {
                return;
            }

            item.Genres.Clear();

            foreach (var cat in cats.InnerText.Split(new[] {','}))
            {
                item.AddGenre(cat.Trim());
            }
        }

        private void GetSynopsisAndTagLine(IEnumerable<HtmlNode> divNodes, BaseItem item)
        {
            var synopsisNode = divNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value == "Section Synopsis");
            if (synopsisNode == null)
            {
                return;
            }

            var synopsisText = synopsisNode.InnerText.Replace("Synopsis", string.Empty);

            var tagline = synopsisNode.ChildNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value == "Tagline");
            if (tagline != null && !string.IsNullOrEmpty(tagline.InnerText))
            {
                item.AddTagline(tagline.InnerText.Trim());
                synopsisText = synopsisText.Replace(tagline.InnerText, string.Empty);
            }

            item.Overview = synopsisText.Trim();
        }

        private void GetCast(IEnumerable<HtmlNode> divNodes, BaseItem item)
        {
            var castNode = divNodes.FirstOrDefault(x => x.HasAttributes && x.Attributes["class"] != null && x.Attributes["class"].Value == "Section Cast");

            if (castNode == null)
            {
                return;
            }

            var castList = castNode.Descendants("li").ToList();
            foreach (var cast in castList)
            {
                var person = new PersonInfo
                {
                    Type = PersonType.Actor
                };

                var name = cast.InnerText.Replace(" - (bio)", string.Empty).Trim();
                if (name.ToLower().Contains("director"))
                {
                    name = name.Replace("Director", string.Empty).Replace("director", string.Empty).Trim();
                    person.Type = PersonType.Director;
                }

                person.Name = name;

                item.AddPerson(person);
            }
        }

        private async Task<List<SearchItem>> GetSearchItems(string name, CancellationToken cancellationToken)
        {
            var url = string.Format(SearchUrl, name);

            string html;

            using (var stream = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = _resourcePool,
                CancellationToken = cancellationToken
            }).ConfigureAwait(false))
            {
                html = stream.ToStringFromStream();
            }

            if (string.IsNullOrEmpty(html))
            {
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return ParseSearchHtml(doc);
        }

        private List<SearchItem> ParseSearchHtml(HtmlDocument doc)
        {
            var listNode = doc.DocumentNode.Descendants("div").ToList();
            var listView = listNode.FirstOrDefault(x => x.HasAttributes && x.Attributes["id"] != null && x.Attributes["id"].Value == "ListView");
            if (listView == null)
            {
                return null;
            }

            var result = new List<SearchItem>();

            foreach (var node in listView.ChildNodes)
            {
                if (node.HasAttributes)
                {
                    // Ignore these items in the list, nothing to see here
                    if ((node.Attributes["class"] == null
                        || node.Attributes["class"].Value == "clear" )
                        || (node.Attributes["id"] == null
                        || node.Attributes["id"].Value == "ListFooter"))
                    {
                        continue;
                    }

                    try
                    {
                        var item = new SearchItem();

                        var id = node.Attributes["id"].Value;
                        item.Id = id.ToLower().Replace("item", string.Empty).Replace("_", string.Empty);

                        var name = node.SelectSingleNode("//p[@class='title']");
                        if (name != null)
                        {
                            item.Name = HttpUtility.HtmlDecode(name.InnerText);
                        }

                        var rank = node.SelectSingleNode("//span[@class='rank']");
                        if (rank != null)
                        {
                            item.Rank = int.Parse(rank.InnerText.Replace(".", string.Empty));
                        }

                        result.Add(item);
                    }
                    catch (Exception ex)
                    {
                        string s = "";
                    }
                }
            }

            return result;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        public override bool Supports(BaseItem item)
        {
            return item is AdultVideo;
        }

        public override bool RequiresInternet
        {
            get { return true; }
        }

        protected override string ProviderVersion
        {
            get { return "1"; }
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            return true;
        }

        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var askedAssembly = new AssemblyName(args.Name);

            Assembly assembly;
            var resourceName = string.Format("MediaBrowser.Plugins.ADEProvider.Assets.Assemblies.{0}.dll", askedAssembly.Name);
            var a = Assembly.GetExecutingAssembly();
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }
                var assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                assembly = Assembly.Load(assemblyData);
            }

            return assembly;
        }

        private class SearchItem
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public int Rank { get; set; }

            public string Url
            {
                get
                {
                    return BaseUrl + Id;
                }
            }
        }
    }
}
