using System;
using System.Collections.Generic;
using System.Text;

namespace Wodsoft.Document
{
    public class DocumentAuthor : IDocumentAuthor
    {
        public DocumentAuthor(string id, string name, string avatar, string link)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Avatar = avatar;
            Link = link;
        }

        public string Id { get; }

        public string Name { get; }

        public string Avatar { get; }

        public string Link { get; }
    }
}
