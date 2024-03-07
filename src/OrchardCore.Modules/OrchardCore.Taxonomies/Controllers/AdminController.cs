using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Settings;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.Admin;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Taxonomies.Models;
using YesSql;

namespace OrchardCore.Taxonomies.Controllers
{
    [Admin]
    public class AdminController : Controller, IUpdateModel
    {
        private readonly IContentManager _contentManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IContentItemDisplayManager _contentItemDisplayManager;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ISession _session;
        private readonly INotifier _notifier;

        protected readonly IHtmlLocalizer H;

        public AdminController(
            ISession session,
            IContentManager contentManager,
            IAuthorizationService authorizationService,
            IContentItemDisplayManager contentItemDisplayManager,
            IContentDefinitionManager contentDefinitionManager,
            INotifier notifier,
            IHtmlLocalizer<AdminController> localizer)
        {
            _contentManager = contentManager;
            _authorizationService = authorizationService;
            _contentItemDisplayManager = contentItemDisplayManager;
            _contentDefinitionManager = contentDefinitionManager;
            _session = session;
            _notifier = notifier;
            H = localizer;
        }

        [Admin("Taxonomies/Create/{id}", "Taxonomies.Create")]
        public async Task<IActionResult> Create(string id, string taxonomyContentItemId, string taxonomyItemId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            if (await _contentDefinitionManager.GetTypeDefinitionAsync(id) == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies))
            {
                return Forbid();
            }

            var contentItem = await _contentManager.NewAsync(id);
            contentItem.Weld<TermPart>();
            contentItem.Alter<TermPart>(t => t.TaxonomyContentItemId = taxonomyContentItemId);

            dynamic model = await _contentItemDisplayManager.BuildEditorAsync(contentItem, this, true);

            model.TaxonomyContentItemId = taxonomyContentItemId;
            model.TaxonomyItemId = taxonomyItemId;

            return View(model);
        }

        [HttpPost]
        [ActionName("Create")]
        public async Task<IActionResult> CreatePost(string id, string taxonomyContentItemId, string taxonomyItemId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            if (await _contentDefinitionManager.GetTypeDefinitionAsync(id) == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies))
            {
                return Forbid();
            }

            ContentItem taxonomy;

            var contentTypeDefinition = await _contentDefinitionManager.GetTypeDefinitionAsync("Taxonomy");

            if (!contentTypeDefinition.IsDraftable())
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.Latest);
            }
            else
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.DraftRequired);
            }

            if (taxonomy == null)
            {
                return NotFound();
            }

            var contentItem = await _contentManager.NewAsync(id);
            contentItem.Weld<TermPart>();
            contentItem.Alter<TermPart>(t => t.TaxonomyContentItemId = taxonomyContentItemId);

            dynamic model = await _contentItemDisplayManager.UpdateEditorAsync(contentItem, this, true);

            if (!ModelState.IsValid)
            {
                model.TaxonomyContentItemId = taxonomyContentItemId;
                model.TaxonomyItemId = taxonomyItemId;

                return View(model);
            }

            if (taxonomyItemId == null)
            {
                // Use the taxonomy as the parent if no target is specified.
                taxonomy.Alter<TaxonomyPart>(part => part.Terms.Add(contentItem));
            }
            else
            {
                // Look for the target taxonomy item in the hierarchy.
                var parentTaxonomyItem = FindTaxonomyItem((JsonObject)taxonomy.As<TaxonomyPart>().Content, taxonomyItemId);

                // Couldn't find targeted taxonomy item.
                if (parentTaxonomyItem == null)
                {
                    return NotFound();
                }

                var taxonomyItems = (JsonArray)parentTaxonomyItem?["Terms"];

                if (taxonomyItems == null)
                {
                    parentTaxonomyItem["Terms"] = taxonomyItems = [];
                }

                taxonomyItems.Add(JObject.FromObject(contentItem));
            }

            await _session.SaveAsync(taxonomy);

            return RedirectToAction(nameof(Edit), "Admin", new { area = "OrchardCore.Contents", contentItemId = taxonomyContentItemId });
        }

        [Admin("Taxonomies/Edit/{taxonomyContentItemId}/{taxonomyItemId}", "Taxonomies.Create")]
        public async Task<IActionResult> Edit(string taxonomyContentItemId, string taxonomyItemId)
        {
            if (string.IsNullOrWhiteSpace(taxonomyContentItemId) || string.IsNullOrWhiteSpace(taxonomyItemId))
            {
                return NotFound();
            }

            var taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.Latest);

            if (taxonomy == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies, taxonomy))
            {
                return Forbid();
            }

            // Look for the target taxonomy item in the hierarchy.
            var taxonomyItem = FindTaxonomyItem((JsonObject)taxonomy.As<TaxonomyPart>().Content, taxonomyItemId);

            // Couldn't find targeted taxonomy item.
            if (taxonomyItem == null)
            {
                return NotFound();
            }

            var contentItem = taxonomyItem.ToObject<ContentItem>();
            contentItem.Weld<TermPart>();
            contentItem.Alter<TermPart>(t => t.TaxonomyContentItemId = taxonomyContentItemId);

            dynamic model = await _contentItemDisplayManager.BuildEditorAsync(contentItem, this, false);

            model.TaxonomyContentItemId = taxonomyContentItemId;
            model.TaxonomyItemId = taxonomyItemId;

            return View(model);
        }

        [HttpPost]
        [ActionName("Edit")]
        public async Task<IActionResult> EditPost(string taxonomyContentItemId, string taxonomyItemId)
        {
            if (string.IsNullOrWhiteSpace(taxonomyContentItemId) || string.IsNullOrWhiteSpace(taxonomyItemId))
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies))
            {
                return Forbid();
            }

            ContentItem taxonomy;

            var contentTypeDefinition = await _contentDefinitionManager.GetTypeDefinitionAsync("Taxonomy");

            if (!contentTypeDefinition.IsDraftable())
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.Latest);
            }
            else
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.DraftRequired);
            }

            if (taxonomy == null)
            {
                return NotFound();
            }

            // Look for the target taxonomy item in the hierarchy.
            var taxonomyItem = FindTaxonomyItem((JsonObject)taxonomy.As<TaxonomyPart>().Content, taxonomyItemId);

            // Couldn't find targeted taxonomy item.
            if (taxonomyItem == null)
            {
                return NotFound();
            }

            var existing = taxonomyItem.ToObject<ContentItem>();

            // Create a new item to take into account the current type definition.
            var contentItem = await _contentManager.NewAsync(existing.ContentType);

            contentItem.ContentItemId = existing.ContentItemId;
            contentItem.Merge(existing);
            contentItem.Weld<TermPart>();
            contentItem.Alter<TermPart>(t => t.TaxonomyContentItemId = taxonomyContentItemId);

            dynamic model = await _contentItemDisplayManager.UpdateEditorAsync(contentItem, this, false);

            if (!ModelState.IsValid)
            {
                model.TaxonomyContentItemId = taxonomyContentItemId;
                model.TaxonomyItemId = taxonomyItemId;

                return View(model);
            }

            taxonomyItem.Merge((JsonObject)contentItem.Content, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                MergeNullValueHandling = MergeNullValueHandling.Merge
            });

            // Merge doesn't copy the properties.
            taxonomyItem[nameof(ContentItem.DisplayText)] = contentItem.DisplayText;

            await _session.SaveAsync(taxonomy);

            return RedirectToAction(nameof(Edit), "Admin", new { area = "OrchardCore.Contents", contentItemId = taxonomyContentItemId });
        }

        [HttpPost]
        [Admin("Taxonomies/Delete/{taxonomyContentItemId}/{taxonomyItemId}", "Taxonomies.Delete")]
        public async Task<IActionResult> Delete(string taxonomyContentItemId, string taxonomyItemId)
        {
            if (string.IsNullOrWhiteSpace(taxonomyContentItemId) || string.IsNullOrWhiteSpace(taxonomyItemId))
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies))
            {
                return Forbid();
            }

            ContentItem taxonomy;

            var contentTypeDefinition = await _contentDefinitionManager.GetTypeDefinitionAsync("Taxonomy");

            if (!contentTypeDefinition.IsDraftable())
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.Latest);
            }
            else
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.DraftRequired);
            }

            if (taxonomy == null)
            {
                return NotFound();
            }

            // Look for the target taxonomy item in the hierarchy.
            var taxonomyItem = FindTaxonomyItem((JsonObject)taxonomy.As<TaxonomyPart>().Content, taxonomyItemId);

            // Couldn't find targeted taxonomy item.
            if (taxonomyItem == null)
            {
                return NotFound();
            }

            taxonomy.As<TaxonomyPart>().Content.Remove(taxonomyItemId);

            await _session.SaveAsync(taxonomy);

            await _notifier.SuccessAsync(H["Taxonomy item deleted successfully."]);

            return RedirectToAction(nameof(Edit), "Admin", new { area = "OrchardCore.Contents", contentItemId = taxonomyContentItemId });
        }

        private static JsonObject FindTaxonomyItem(JsonObject contentItem, string taxonomyItemId)
        {
            if (contentItem["ContentItemId"]?.Value<string>() == taxonomyItemId)
            {
                return contentItem;
            }

            if (!contentItem.TryGetPropertyValue("Terms", out var terms) || terms is not JsonArray taxonomyItems)
            {
                return null;
            }

            JsonObject result;
            foreach (var taxonomyItem in taxonomyItems.Cast<JsonObject>())
            {
                // Search in inner taxonomy items.
                result = FindTaxonomyItem(taxonomyItem, taxonomyItemId);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
