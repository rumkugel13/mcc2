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
    public void TestCompileExtraCreditLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileExtraCreditMemberAccess()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/member_access").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditMemberAccess()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/member_access").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileExtraCreditOtherFeatures()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/other_features").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditOtherFeatures()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/other_features").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileExtraCreditSemanticAnalysis()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/semantic_analysis").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditSemanticAnalysis()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/semantic_analysis").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileExtraCreditSizeAndOffset()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/size_and_offset").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditSizeAndOffset()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/size_and_offset").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileExtraCreditUnionCopy()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/union_copy").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteExtraCreditUnionCopy()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit/union_copy").Where(a => a.EndsWith(".c"));
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
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("array_of_structs")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("global_struct")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("opaque_struct")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("param_struct")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("return_struct")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
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
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries/initializers")
            .Where(a => a.EndsWith(".c") && a.Contains("auto_struct") && !a.Contains("nested")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries/initializers")
            .Where(a => a.EndsWith(".c") && a.Contains("nested_auto_struct")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries/initializers")
            .Where(a => a.EndsWith(".c") && a.Contains("nested_static_struct")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/no_structure_parameters/libraries/initializers")
            .Where(a => a.EndsWith(".c") && a.Contains("static_struct") && !a.Contains("nested")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
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
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/parameters")
            .Where(a => a.EndsWith(".c") && !a.Contains("page_boundary"));
        TestUtils.TestExecuteValid(files);

        var special = TestUtils.TestsPath + chapter + "valid/parameters/pass_args_on_page_boundary.c";
        var extra = TestUtils.TestsPath + chapter + "valid/parameters/data_on_page_boundary_" + (OperatingSystem.IsMacOS() ? "osx.s" : "linux.s");
        TestUtils.TestExecuteValidSpecial(special, extra);
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
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/parameters/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("classify_params")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/parameters/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("modify_param")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/parameters/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("param_calling")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/parameters/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("pass_struct")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/parameters/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("struct_sizes")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
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
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/params_and_returns")
            .Where(a => a.EndsWith(".c") && !a.Contains("page_boundary") && !a.Contains("space") && !a.Contains("pointer"));
        TestUtils.TestExecuteValid(files);

        var special = TestUtils.TestsPath + chapter + "valid/params_and_returns/return_pointer_in_rax.c";
        var extra = TestUtils.TestsPath + chapter + "valid/params_and_returns/validate_return_pointer_" + (OperatingSystem.IsMacOS() ? "osx.s" : "linux.s");
        TestUtils.TestExecuteValidSpecial(special, extra);

        special = TestUtils.TestsPath + chapter + "valid/params_and_returns/return_space_overlap.c";
        extra = TestUtils.TestsPath + chapter + "valid/params_and_returns/return_space_address_overlap_" + (OperatingSystem.IsMacOS() ? "osx.s" : "linux.s");
        TestUtils.TestExecuteValidSpecial(special, extra);

        special = TestUtils.TestsPath + chapter + "valid/params_and_returns/return_struct_on_page_boundary.c";
        extra = TestUtils.TestsPath + chapter + "valid/params_and_returns/data_on_page_boundary_" + (OperatingSystem.IsMacOS() ? "osx.s" : "linux.s");
        TestUtils.TestExecuteValidSpecial(special, extra);

        special = TestUtils.TestsPath + chapter + "valid/params_and_returns/return_big_struct_on_page_boundary.c";
        extra = TestUtils.TestsPath + chapter + "valid/params_and_returns/big_data_on_page_boundary_" + (OperatingSystem.IsMacOS() ? "osx.s" : "linux.s");
        TestUtils.TestExecuteValidSpecial(special, extra);
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
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/params_and_returns/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("access_retval")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/params_and_returns/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("missing_retval")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/params_and_returns/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("return_calling")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/params_and_returns/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("retval_struct")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
    }
}