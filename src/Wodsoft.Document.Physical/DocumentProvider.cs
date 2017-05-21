using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wodsoft.Document.Physical
{
    public class DocumentProvider : IDocumentProvider
    {
        private Dictionary<string, IDocumentAuthor> _Authors;
        public DocumentProvider(IFileProvider fileProvider, string path)
        {
            if (fileProvider == null)
                throw new ArgumentNullException(nameof(fileProvider));
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            FileProvider = fileProvider;
            Path = path;
            _Authors = new Dictionary<string, IDocumentAuthor>();
        }

        public IFileProvider FileProvider { get; private set; }

        public string Path { get; private set; }

        public string Title { get; private set; }

        public IReadOnlyCollection<IDocumentPage> Pages { get; private set; }

        public IReadOnlyCollection<IDocumentLanguage> Languages { get; private set; }

        public IDocumentLanguage DefaultLanguage { get; private set; }

        public bool IsLoaded { get; private set; }

        public async Task LoadAsync()
        {
            if (IsLoaded)
                return;
            var file = FileProvider.GetFileInfo(Path + "/topic.md");
            if (!file.Exists)
                throw new NotSupportedException("找不到topic.md文件。");

            var stream = file.CreateReadStream();
            var reader = new StreamReader(stream);
            Title = await reader.ReadLineAsync();
            if (!Title.StartsWith("# "))
                throw new FormatException("topic.md文件格式错误，标题解释失败。");

            List<IDocumentLanguage> languages = new List<IDocumentLanguage>();
            while (true)
            {
                var value = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(value))
                    break;
                var data = value.Split(':');
                if (data.Length != 2)
                    throw new FormatException("topic.md文件格式错误，语言解释失败。");
                data[0] = data[0].Trim();
                data[1] = data[1].Trim();
                if (!data[0].StartsWith("[") || !data[0].EndsWith("]"))
                    throw new FormatException("topic.md文件格式错误，语言解释失败。");
                languages.Add(new DocumentLanguage(data[1], data[0].Substring(1, data[0].Length - 2)));
            }
            Languages = new ReadOnlyCollection<IDocumentLanguage>(languages);
            DefaultLanguage = languages[0];

            int current = 0;
            int space, start, end;
            Stack<List<IDocumentPage>> childrenStack = new Stack<List<IDocumentPage>>();
            var root = new List<IDocumentPage>();
            Pages = new ReadOnlyCollection<IDocumentPage>(root);
            childrenStack.Push(root);
            while (!reader.EndOfStream)
            {
                var value = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(value))
                    continue;
                if (!value.StartsWith("#"))
                    throw new FormatException("topic.md文件格式错误，目录解释失败。");
                space = value.IndexOf(' ');
                if (space == -1 || space > ++current)
                    throw new FormatException("topic.md文件格式错误，目录解释失败。");
                if (space != current)
                {
                    var skip = current - space;
                    for (int i = 0; i < skip; i++)
                    {
                        childrenStack.Pop();
                        current--;
                    }
                }
                start = value.IndexOf('[', space + 1);
                if (start == -1)
                    throw new FormatException("topic.md文件格式错误，目录解释失败。");
                end = value.IndexOf(']', start + 1);
                if (end == -1)
                    throw new FormatException("topic.md文件格式错误，目录解释失败。");
                string name = value.Substring(start + 1, end - start - 1);
                start = value.IndexOf('(', end + 1);
                if (start == -1)
                    throw new FormatException("topic.md文件格式错误，目录解释失败。");
                end = value.IndexOf(')', start + 1);
                if (end == -1)
                    throw new FormatException("topic.md文件格式错误，目录解释失败。");
                string path = value.Substring(start + 1, end - start - 1);
                if (!path.EndsWith(".md"))
                    throw new FormatException("topic.md文件格式错误，目录解释失败。");

                List<IDocumentContent> contents = await GetContents(path);
                List<IDocumentPage> children = new List<IDocumentPage>();
                DocumentPage page = new DocumentPage(name, contents, children);
                childrenStack.Peek().Add(page);
                childrenStack.Push(children);
            }
            IsLoaded = true;
        }

        private async Task<List<IDocumentContent>> GetContents(string path)
        {
            List<IDocumentContent> contents = new List<IDocumentContent>();
            {
                var file = FileProvider.GetFileInfo(Path + "/" + path);
                if (file.Exists)
                {
                    var content = await GetContent(file, DefaultLanguage);
                    if (content != null)
                        contents.Add(content);
                }
            }
            foreach (var lang in Languages)
            {
                if (lang == DefaultLanguage && contents.Count > 0)
                    continue;
                var file = FileProvider.GetFileInfo(Path + "/" + path.Insert(path.Length - 3, "." + lang.Value));
                if (!file.Exists)
                    continue;
                var content = await GetContent(file, lang);
                if (content != null)
                    contents.Add(content);
            }
            return contents;
        }

        private async Task<DocumentContent> GetContent(IFileInfo fileInfo, IDocumentLanguage lang)
        {
            var stream = fileInfo.CreateReadStream();
            var reader = new StreamReader(stream);
            if (await reader.ReadLineAsync() != "---")
                throw new FormatException(fileInfo.Name + "文件格式错误，属性解释失败。");
            Dictionary<string, string> attribute = new Dictionary<string, string>();
            while (!reader.EndOfStream)
            {
                var value = await reader.ReadLineAsync();
                if (value == "---")
                    break;
                var data = value.Split(':');
                if (data.Length != 2)
                    throw new FormatException(fileInfo.Name + "文件格式错误，属性解释失败。");
                attribute.Add(data[0].Trim(), data[1].Trim());
            }
            var title = await reader.ReadLineAsync();
            if (title.StartsWith("#"))
            {
                var i = title.IndexOf(' ');
                title = title.Substring(i + 1);
            }
            var content = await reader.ReadToEndAsync();
            return new DocumentContent(
                title,
                attribute.ContainsKey("keywords") ? attribute["keywords"].Split(',').ToList() : new List<string>(),
                attribute.ContainsKey("authors") ? attribute["authors"].Split(',').Select(t => GetAuthor(t)).ToList() : new List<IDocumentAuthor>(),
                fileInfo.LastModified.DateTime,
                fileInfo.LastModified.DateTime,
                content,
                lang);
        }

        private IDocumentAuthor GetAuthor(string value)
        {
            int start, end;
            start = value.IndexOf('[');
            if (start == -1)
                throw new FormatException("属性格式错误，作者解释失败。");
            end = value.IndexOf(']', start + 1);
            if (end == -1)
                throw new FormatException("属性格式错误，作者解释失败。");
            string name = value.Substring(start + 1, end - start - 1);
            start = value.IndexOf('(', end + 1);
            if (start == -1)
                throw new FormatException("属性格式错误，作者解释失败。");
            end = value.IndexOf(')', start + 1);
            if (end == -1)
                throw new FormatException("属性格式错误，作者解释失败。");
            string expression = value.Substring(start + 1, end - start - 1);
            start = expression.IndexOf(" \"");
            string id, link;
            if (start != -1)
            {
                id = expression.Substring(0, start);
                link = expression.Substring(start + 2, expression.Length - start - 3);
            }
            else
            {
                id = expression;
                link = null;
            }
            if (_Authors.ContainsKey(id))
                return _Authors[id];
            var author = new DocumentAuthor(id, name, null, link);
            _Authors.Add(id, author);
            return author;
        }
    }
}
