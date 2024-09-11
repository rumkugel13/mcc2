namespace mcc2.Tests;

[TestClass]
public class TestExternal
{
    [TestMethod]
    public void TestChapter01()
    {
        TestUtils.TestExternal(1);
    }  

    [TestMethod]
    public void TestChapter02()
    {
        TestUtils.TestExternal(2);
    }

    [TestMethod]
    public void TestChapter03()
    {
        TestUtils.TestExternal(3);
    }

    [TestMethod]
    public void TestChapter03ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(3, "--bitwise");
    }

    [TestMethod]
    public void TestChapter04()
    {
        TestUtils.TestExternal(4);
    }

    [TestMethod]
    public void TestChapter04ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(4, "--bitwise");
    }

    [TestMethod]
    public void TestChapter05()
    {
        TestUtils.TestExternal(5);
    }

    [TestMethod]
    public void TestChapter05ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(5, "--bitwise --compound --increment");
    }

    [TestMethod]
    public void TestChapter06()
    {
        TestUtils.TestExternal(6);
    }

    [TestMethod]
    public void TestChapter06ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(6, "--bitwise --compound --increment --goto");
    }

    [TestMethod]
    public void TestChapter07()
    {
        TestUtils.TestExternal(7);
    }

    [TestMethod]
    public void TestChapter07ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(7, "--bitwise --compound --increment --goto");
    }

    [TestMethod]
    public void TestChapter08()
    {
        TestUtils.TestExternal(8);
    }

    [TestMethod]
    public void TestChapter08ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(8, "--bitwise --compound --increment --goto --switch");
    }

    [TestMethod]
    public void TestChapter09()
    {
        TestUtils.TestExternal(9);
    }

    [TestMethod]
    public void TestChapter09ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(9, "--bitwise --compound --increment --goto --switch");
    }
}