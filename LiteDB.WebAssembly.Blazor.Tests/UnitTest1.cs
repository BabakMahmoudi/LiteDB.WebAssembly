using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace LiteDB.WebAssembly.Blazor.Tests
{
    public class Tests : PageTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test1()
        {
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync();

            int result = await Page.EvaluateAsync<int>("() => 7 + 3");
            Assert.AreEqual(10, result);
        }
    }
}