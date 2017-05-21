using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Wodsoft.Document.Github
{
    public class DocumentProvider : IDocumentProvider
    {
        private Dictionary<string, IDocumentAuthor> _Authors;
        private HttpClient _Client;
        public DocumentProvider(string owner, string repository)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _Authors = new Dictionary<string, IDocumentAuthor>();
        }

        public string Owner { get; private set; }

        public string Repository { get; private set; }

        public string Title { get; private set; }

        public IReadOnlyCollection<IDocumentPage> Pages { get; private set; }

        public IReadOnlyCollection<IDocumentLanguage> Languages { get; private set; }

        public IDocumentLanguage DefaultLanguage { get; private set; }

        public async Task LoadAsync()
        {
            _Client = new HttpClient();
            _Client.BaseAddress = new Uri("https://api.github.com");

            var message = await _Client.GetAsync("/repos/" + Owner + "/" + Repository + "/contents/" + "topic.md");
            if (message.StatusCode != System.Net.HttpStatusCode.OK)
                throw new NotSupportedException("找不到topic.md文件。");
            var stream = await message.Content.ReadAsStreamAsync();
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
            message.Dispose();
        }

        private async Task<List<IDocumentContent>> GetContents(string path)
        {
            List<IDocumentContent> contents = new List<IDocumentContent>();
            {
                var message = await _Client.GetAsync(path);
                if (message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var stream = await message.Content.ReadAsStreamAsync();
                    var content = await GetContent(stream, DefaultLanguage);
                    if (content != null)
                        contents.Add(content);
                }
                message.Dispose();
            }
            foreach (var lang in Languages)
            {
                if (lang == DefaultLanguage && contents.Count > 0)
                    continue;
                var file = FileProvider.GetFileInfo(BaseUri + "/" + path.Insert(path.Length - 3, "." + lang.Value));
                if (!file.Exists)
                    continue;
                var content = await GetContent(file, lang);
                if (content != null)
                    contents.Add(content);
            }
            return contents;
        }

        private async Task<DocumentContent> GetContent(Stream stream, IDocumentLanguage lang)
        {
            var reader = new StreamReader(stream);
            if (await reader.ReadLineAsync() != "---")
                throw new FormatException("文件格式错误，属性解释失败。");
            Dictionary<string, string> attribute = new Dictionary<string, string>();
            while (!reader.EndOfStream)
            {
                var value = await reader.ReadLineAsync();
                if (value == "---")
                    break;
                var data = value.Split(':');
                if (data.Length != 2)
                    throw new FormatException("文件格式错误，属性解释失败。");
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
