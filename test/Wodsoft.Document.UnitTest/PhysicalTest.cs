using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.FileProviders;
using Wodsoft.Document.Physical;
using System.Threading.Tasks;

namespace Wodsoft.Document.UnitTest
{
    [TestClass]
    public class PhysicalTest
    {
        [TestMethod]
        public async Task ProviderTest()
        {
            PhysicalFileProvider fileProvider = new PhysicalFileProvider("D:\\Docs\\ComBoost");
            DocumentProvider docProvider = new DocumentProvider(fileProvider, "");
            await docProvider.LoadAsync();
            Assert.AreEqual(4, docProvider.Pages.Count);
        }
    }
}
