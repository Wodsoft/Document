using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Wodsoft.Document
{
    public class DocumentContent : IDocumentContent
    {
        public DocumentContent(string title, IList<string> keywords, IList<IDocumentAuthor> authors, DateTime createDate, DateTime editDate, string content, IDocumentLanguage language)
        {
            if (keywords == null)
                throw new ArgumentNullException(nameof(keywords));
            if (authors == null)
                throw new ArgumentNullException(nameof(authors));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Keywords = new ReadOnlyCollection<string>(keywords);
            Authors = new ReadOnlyCollection<IDocumentAuthor>(authors);
            CreateDate = createDate;
            EditDate = editDate;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Language = language ?? throw new ArgumentNullException(nameof(language));
        }

        public string Title { get; }

        public IReadOnlyCollection<IDocumentAuthor> Authors { get; }

        public IReadOnlyCollection<string> Keywords { get; }

        public DateTime CreateDate { get; }

        public DateTime EditDate { get; }

        public string Content { get; }

        public IDocumentLanguage Language { get; }
    }
}
