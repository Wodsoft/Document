using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wodsoft.Document
{
    public class DocumentManager
    {
        public DocumentManager(IDocumentProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            Provider = provider;
            PreferredLanguage = new List<IDocumentLanguage>();
            _Pages = new Dictionary<string, IDocumentPage>();
        }

        private Dictionary<string, IDocumentPage> _Pages;

        public IDocumentProvider Provider { get; }

        public IList<IDocumentLanguage> PreferredLanguage { get; }

        public bool IsLoaded { get; private set; }

        public async Task InitializeAsync()
        {
            if (IsLoaded)
                return;
            await Provider.LoadAsync();
            InsertPage(Provider.Pages, "");
            IsLoaded = true;
        }

        private void InsertPage(IReadOnlyCollection<IDocumentPage> pages, string parent)
        {
            foreach (var page in pages)
            {
                _Pages.Add(parent + page.Name.ToLower(), page);
                InsertPage(page.Children, page.Name.ToLower() + "/");
            }
        }

        public IDocumentContent GetContent(string path, IDocumentLanguage lang)
        {
            if (!IsLoaded)
                throw new InvalidOperationException("未加载内容。");
            if (lang == null)
                throw new ArgumentNullException(nameof(lang));
            IDocumentPage page;
            if (!string.IsNullOrEmpty(path))
            {
                path = path.ToLower();
                if (!_Pages.TryGetValue(path, out page))
                    return null;
            }
            else
                page = Provider.Pages.First();
            var content = page.GetContent(lang);
            if (content == null)
                foreach (var prefer in PreferredLanguage)
                {
                    content = page.GetContent(prefer);
                    if (content != null)
                        break;
                }
            return content;
        }
    }
}
