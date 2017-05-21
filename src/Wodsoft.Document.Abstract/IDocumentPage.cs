using System;
using System.Collections.Generic;
using System.Text;

namespace Wodsoft.Document
{
    public interface IDocumentPage
    {
        string Name { get; }
        
        IDocumentContent GetContent(IDocumentLanguage language);

        IReadOnlyCollection<IDocumentPage> Children { get; }
    }
}
