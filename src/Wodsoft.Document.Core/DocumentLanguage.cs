using System;
using System.Collections.Generic;
using System.Text;

namespace Wodsoft.Document
{
    public class DocumentLanguage : IDocumentLanguage
    {
        public DocumentLanguage(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Name { get; }

        public string Value { get; }
    }
}
