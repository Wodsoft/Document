using System;
using System.Collections.Generic;
using System.Text;

namespace Wodsoft.Document
{
    public interface IDocumentContent
    {
        string Title { get; }

        IReadOnlyCollection<IDocumentAuthor> Authors { get; }

        IReadOnlyCollection<string> Keywords { get; }

        DateTime CreateDate { get; }

        DateTime EditDate { get; }

        string Content { get; }

        IDocumentLanguage Language { get; }
    }
}
