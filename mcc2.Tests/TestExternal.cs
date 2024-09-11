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
    
    [TestMethod]
    public void TestChapter10()
    {
        TestUtils.TestExternal(10);
    }

    [TestMethod]
    public void TestChapter10ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(10, "--bitwise --compound --increment --goto --switch");
    }

    [TestMethod]
    public void TestChapter11()
    {
        TestUtils.TestExternal(11);
    }

    [TestMethod]
    public void TestChapter11ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(11, "--bitwise --compound --increment --goto --switch");
    }

    [TestMethod]
    public void TestChapter12()
    {
        TestUtils.TestExternal(12);
    }

    [TestMethod]
    public void TestChapter12ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(12, "--bitwise --compound --increment --goto --switch");
    }

    [TestMethod]
    public void TestChapter13()
    {
        TestUtils.TestExternal(13);
    }

    [TestMethod]
    public void TestChapter13ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(13, "--bitwise --compound --increment --goto --switch --nan");
    }

    [TestMethod]
    public void TestChapter14()
    {
        TestUtils.TestExternal(14);
    }

    [TestMethod]
    public void TestChapter14ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(14, "--bitwise --compound --increment --goto --switch --nan");
    }

    [TestMethod]
    public void TestChapter15()
    {
        TestUtils.TestExternal(15);
    }

    [TestMethod]
    public void TestChapter15ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(15, "--bitwise --compound --increment --goto --switch --nan");
    }

    [TestMethod]
    public void TestChapter16()
    {
        TestUtils.TestExternal(16);
    }

    [TestMethod]
    public void TestChapter16ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(16, "--bitwise --compound --increment --goto --switch --nan");
    }

    [TestMethod]
    public void TestChapter17()
    {
        TestUtils.TestExternal(17);
    }

    [TestMethod]
    public void TestChapter17ExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(17, "--bitwise --compound --increment --goto --switch --nan");
    }
}