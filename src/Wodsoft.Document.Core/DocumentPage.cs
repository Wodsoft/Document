using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;

namespace Wodsoft.Document
{
    public class DocumentPage : IDocumentPage
    {
        public DocumentPage(string name, IList<IDocumentContent> contents, IList<IDocumentPage> children)
        {
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (children == null)
                throw new ArgumentNullException(nameof(children));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Children = new ReadOnlyCollection<IDocumentPage>(children);
            Contents = new ReadOnlyCollection<IDocumentContent>(contents);
        }

        public string Name { get; }
        
        public IReadOnlyCollection<IDocumentPage> Children { get; }

        public IReadOnlyCollection<IDocumentContent> Contents { get; }

        public IDocumentContent GetContent(IDocumentLanguage lang)
        {
            return Contents.SingleOrDefault(t => t.Language == lang);
        }
    }
}
