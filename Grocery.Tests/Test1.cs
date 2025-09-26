using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grocery.Tests
{
    [TestClass]
    public class SimpleTests
    {
        [TestMethod]
        public void AlwaysPasses()
        {
            Assert.AreEqual(4, 2 + 2);
        }
    }
}
