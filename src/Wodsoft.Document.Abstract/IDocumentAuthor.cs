using System;
using System.Collections.Generic;
using System.Text;

namespace Wodsoft.Document
{
    public interface IDocumentAuthor
    {
        string Id { get; }

        string Name { get; }
        
        string Avatar { get; }

        string Link { get; }
    }
}
