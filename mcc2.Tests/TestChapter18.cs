namespace mcc2.Tests;

[TestClass]
public class TestChapter18
{
    private const string chapter = "chapter_18/";

    [TestMethod]
    public void TestInvalidLex()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_lex").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidLex(files);
    }

    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidParseExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_parse/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsStructTags()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_struct_tags").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsStructTagsExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_struct_tags/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCreditBadUnionMemberAccess()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit/bad_union_member_access").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCreditIncompatibleUnionTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit/incompatible_union_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCreditIncompleteUnions()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit/incomplete_unions").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCreditInvalidUnionLvalues()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit/invalid_union_lvalues").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCreditOtherFeatures()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit/other_features").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCreditScalarRequired()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit/scalar_required").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCreditUnionInitializers()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit/union_initializers").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCreditUnionStructConflicts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit/union_struct_conflicts").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCreditUnionTagResolution()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit/union_tag_resolution").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCreditUnionTypeDeclarations()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit/union_type_declarations").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesIncompatibleTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/incompatible_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsInitializers()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/initializers").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsIncompleteStructs()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/invalid_incomplete_structs").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsInvalidLvalues()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/invalid_lvalues").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsInvalidMemberOperators()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/invalid_member_operators").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsInvalidStructDeclaration()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/invalid_struct_declaration").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsScalarRequired()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/scalar_required").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTagResolution()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/tag_resolution").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestCompileExtraCreditsLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditsLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileExtraCreditsMemberAccess()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/member_access").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditsMemberAccess()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/member_access").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileExtraCreditsOtherFeatures()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/other_features").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditsOtherFeatures()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/other_features").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileExtraCreditsSemanticAnalysis()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/semantic_analysis").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditsSemanticAnalysis()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/semantic_analysis").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileExtraCreditsSizeAndOffset()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/size_and_offset").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditsSizeAndOffset()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/size_and_offset").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileExtraCreditsUnionCopy()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/union_copy").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditsUnionCopy()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credits/union_copy").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileNoStructureParametersLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteNoStructureParametersLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileNoStructureParametersLibrariesInitializers()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries/initializers").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteNoStructureParametersLibrariesInitializers()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries/initializers").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileNoStructureParametersParseAndLex()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/parse_and_lex").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteNoStructureParametersParseAndLex()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/parse_and_lex").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileNoStructureParametersScalarMemberAccess()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/scalar_member_access").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteNoStructureParametersScalarMemberAccess()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/scalar_member_access").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileNoStructureParametersSemanticAnalysis()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/semantic_analysis").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteNoStructureParametersSemanticAnalysis()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/semantic_analysis").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileNoStructureParametersSizeAndOffsetCalculations()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/size_and_offset_calculations").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteNoStructureParametersSizeAndOffsetCalculations()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/size_and_offset_calculations").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileNoStructureParametersSmokeTests()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/smoke_tests").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteNoStructureParametersSmokeTests()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/smoke_tests").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileNoStructureParametersStructCopy()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/struct_copy").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteNoStructureParametersStructCopy()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/struct_copy").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileParameters()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/parameters").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteParameters()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/parameters").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileParametersLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/parameters/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteParametersLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/parameters/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileParamsAndReturns()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/params_and_returns").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteParamsAndReturns()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/params_and_returns").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileParamsAndReturnsLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/params_and_returns/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteParamsAndReturnsLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/params_and_returns/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }
}