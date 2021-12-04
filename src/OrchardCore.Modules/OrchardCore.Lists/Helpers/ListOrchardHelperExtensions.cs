using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Indexes;
using OrchardCore.Lists.Helpers;
using YesSql;

public static class ListOrchardHelperExtensions
{
    /// <summary>
    /// Returns list count.
    /// </summary>
    /// <param name="orchardHelper">The <see cref="IOrchardHelper"/>.</param>
    /// <param name="listContentItemId">The list content item id.</param>
    /// <param name="itemPredicate">The optional predicate applied to each item. By defult published items only.</param>
    /// <returns>A number of list items satisfying given predicate.</returns>
    public static Task<int> QueryListItemsCountAsync(this IOrchardHelper orchardHelper, string listContentItemId, Expression<Func<ContentItemIndex, bool>> itemPredicate = null)
    {
        var session = orchardHelper.HttpContext.RequestServices.GetService<ISession>();

        return ListQueryHelpers.QueryListItemsCountAsync(session, listContentItemId, itemPredicate);
    }

    /// <summary>
    /// Returns list items.
    /// </summary>
    /// <param name="orchardHelper">The <see cref="IOrchardHelper"/>.</param>
    /// <param name="listContentItemId">The list content item id.</param>
    /// <param name="itemPredicate">The optional predicate applied to each item. By defult published items only.</param>
    /// <returns>An enumerable of list items satisfying given predicate.</returns>
    public static Task<IEnumerable<ContentItem>> QueryListItemsAsync(this IOrchardHelper orchardHelper, string listContentItemId, Expression<Func<ContentItemIndex, bool>> itemPredicate = null)
    {
        var session = orchardHelper.HttpContext.RequestServices.GetService<ISession>();

        return ListQueryHelpers.QueryListItemsAsync(session, listContentItemId, itemPredicate);
    }
}
