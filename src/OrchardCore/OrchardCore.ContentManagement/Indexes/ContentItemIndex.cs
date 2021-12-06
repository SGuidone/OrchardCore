using System;
using YesSql.Indexes;

namespace OrchardCore.ContentManagement.Records
{
    [Obsolete("ContentItemIndex is deprecated, please use OrchardCore.ContentManagement.Indexes.ContentItemIndex instead.", true)]
    public class ContentItemIndex : MapIndex
    {
        public const int MaxContentTypeSize = 255;
        public const int MaxContentPartSize = 255;
        public const int MaxContentFieldSize = 255;
        public const int MaxOwnerSize = 255;
        public const int MaxAuthorSize = 255;
        public const int MaxDisplayTextSize = 255;

        public int DocumentId { get; set; }
        public string ContentItemId { get; set; }
        public string ContentItemVersionId { get; set; }
        public bool Published { get; set; }
        public bool Latest { get; set; }
        public string ContentType { get; set; }
        public DateTime? ModifiedUtc { get; set; }
        public DateTime? PublishedUtc { get; set; }
        public DateTime? CreatedUtc { get; set; }
        public string Owner { get; set; }
        public string Author { get; set; }
        public string DisplayText { get; set; }
    }

    [Obsolete("ContentItemIndexProvider is deprecated, please use OrchardCore.ContentManagement.Indexes.ContentItemIndexProvider instead.", true)]
    public class ContentItemIndexProvider : IndexProvider<ContentItem>
    {
        public override void Describe(DescribeContext<ContentItem> context)
        {
            context.For<ContentItemIndex>()
                .Map(contentItem =>
                {
                    var contentItemIndex = new ContentItemIndex
                    {
                        Latest = contentItem.Latest,
                        Published = contentItem.Published,
                        ContentType = contentItem.ContentType,
                        ContentItemId = contentItem.ContentItemId,
                        ContentItemVersionId = contentItem.ContentItemVersionId,
                        ModifiedUtc = contentItem.ModifiedUtc,
                        PublishedUtc = contentItem.PublishedUtc,
                        CreatedUtc = contentItem.CreatedUtc,
                        Owner = contentItem.Owner,
                        Author = contentItem.Author,
                        DisplayText = contentItem.DisplayText
                    };

                    if (contentItemIndex.ContentType?.Length > ContentItemIndex.MaxContentTypeSize)
                    {
                        contentItemIndex.ContentType = contentItem.ContentType.Substring(0, ContentItemIndex.MaxContentTypeSize);
                    }

                    if (contentItemIndex.Owner?.Length > ContentItemIndex.MaxOwnerSize)
                    {
                        contentItemIndex.Owner = contentItem.Owner.Substring(0, ContentItemIndex.MaxOwnerSize);
                    }

                    if (contentItemIndex.Author?.Length > ContentItemIndex.MaxAuthorSize)
                    {
                        contentItemIndex.Author = contentItem.Author.Substring(0, ContentItemIndex.MaxAuthorSize);
                    }

                    if (contentItemIndex.DisplayText?.Length > ContentItemIndex.MaxDisplayTextSize)
                    {
                        contentItemIndex.DisplayText = contentItem.DisplayText.Substring(0, ContentItemIndex.MaxDisplayTextSize);
                    }

                    return contentItemIndex;
                });
        }
    }
}

namespace OrchardCore.ContentManagement.Indexes
{
    public class ContentItemIndex : MapIndex
    {
        public const int MaxContentTypeSize = 255;
        public const int MaxContentPartSize = 255;
        public const int MaxContentFieldSize = 255;
        public const int MaxOwnerSize = 255;
        public const int MaxAuthorSize = 255;
        public const int MaxDisplayTextSize = 255;

        public int DocumentId { get; set; }
        public string ContentItemId { get; set; }
        public string ContentItemVersionId { get; set; }
        public bool Published { get; set; }
        public bool Latest { get; set; }
        public string ContentType { get; set; }
        public DateTime? ModifiedUtc { get; set; }
        public DateTime? PublishedUtc { get; set; }
        public DateTime? CreatedUtc { get; set; }
        public string Owner { get; set; }
        public string Author { get; set; }
        public string DisplayText { get; set; }
    }

    public class ContentItemIndexProvider : IndexProvider<ContentItem>
    {
        public override void Describe(DescribeContext<ContentItem> context)
        {
            context.For<ContentItemIndex>()
                .Map(contentItem =>
                {
                    var contentItemIndex = new ContentItemIndex
                    {
                        Latest = contentItem.Latest,
                        Published = contentItem.Published,
                        ContentType = contentItem.ContentType,
                        ContentItemId = contentItem.ContentItemId,
                        ContentItemVersionId = contentItem.ContentItemVersionId,
                        ModifiedUtc = contentItem.ModifiedUtc,
                        PublishedUtc = contentItem.PublishedUtc,
                        CreatedUtc = contentItem.CreatedUtc,
                        Owner = contentItem.Owner,
                        Author = contentItem.Author,
                        DisplayText = contentItem.DisplayText
                    };

                    if (contentItemIndex.ContentType?.Length > ContentItemIndex.MaxContentTypeSize)
                    {
                        contentItemIndex.ContentType = contentItem.ContentType.Substring(0, ContentItemIndex.MaxContentTypeSize);
                    }

                    if (contentItemIndex.Owner?.Length > ContentItemIndex.MaxOwnerSize)
                    {
                        contentItemIndex.Owner = contentItem.Owner.Substring(0, ContentItemIndex.MaxOwnerSize);
                    }

                    if (contentItemIndex.Author?.Length > ContentItemIndex.MaxAuthorSize)
                    {
                        contentItemIndex.Author = contentItem.Author.Substring(0, ContentItemIndex.MaxAuthorSize);
                    }

                    if (contentItemIndex.DisplayText?.Length > ContentItemIndex.MaxDisplayTextSize)
                    {
                        contentItemIndex.DisplayText = contentItem.DisplayText.Substring(0, ContentItemIndex.MaxDisplayTextSize);
                    }

                    return contentItemIndex;
                });
        }
    }
}
