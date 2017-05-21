using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Wodsoft.Document
{
    public interface IDocumentProvider
    {
        string Title { get; }

        IReadOnlyCollection<IDocumentPage> Pages { get; }

        IReadOnlyCollection<IDocumentLanguage> Languages { get; }

        Task LoadAsync();

        bool IsLoaded { get; }
    }
}
