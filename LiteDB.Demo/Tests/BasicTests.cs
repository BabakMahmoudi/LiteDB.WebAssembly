using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiteDB.Demo.Tests
{
    public interface ITestFixture
    {
        Task<ILiteDatabase> GetDb(bool refersh = false, bool open = true, bool cache = false);
        void Log(string fmt, params object[] args);
    }
    public class Tests
    {
        public static async Task<bool> Test1(ITestFixture fixture)
        {
            var db = await fixture.GetDb();

            return true;
        }
    }
}
