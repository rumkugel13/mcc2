using mcc2.AST;
using static mcc2.SemanticAnalyzer;

namespace mcc2;

public class TypeChecker
{
    public void Check(ASTProgram program, Dictionary<string, SymbolEntry> symbolTable)
    {
        foreach (var decl in program.Declarations)
        {
            if (decl is Declaration.FunctionDeclaration fun)
                TypeCheckFunctionDeclaration(fun, symbolTable);
            else if (decl is Declaration.VariableDeclaration var)
                TypeCheckFileScopeVariableDeclaration(var, symbolTable);
        }
    }

    private void TypeCheckFunctionDeclaration(Declaration.FunctionDeclaration functionDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        Type.FunctionType funType = new Type.FunctionType(functionDeclaration.Parameters.Count);
        bool hasBody = functionDeclaration.Body != null;
        bool alreadyDefined = false;
        bool global = functionDeclaration.StorageClass != Declaration.StorageClasses.Static;

        if (symbolTable.TryGetValue(functionDeclaration.Identifier, out SymbolEntry prevEntry))
        {
            var attributes = (IdentifierAttributes.Function)prevEntry.IdentifierAttributes;
            // note: check correct type and number of parameters
            if (prevEntry.Type is not Type.FunctionType funcA || funcA.ParameterCount != funType.ParameterCount)
                throw new Exception("Type Error: Incompatible function declarations");

            alreadyDefined = attributes.Defined;
            if (alreadyDefined && hasBody)
                throw new Exception("Type Error: Function is defined more than once");

            if (attributes.Global && functionDeclaration.StorageClass == Declaration.StorageClasses.Static)
                throw new Exception("Static function declaration follows non-static");

            global = attributes.Global;
        }

        symbolTable[functionDeclaration.Identifier] = new SymbolEntry() { Type = funType, IdentifierAttributes = new IdentifierAttributes.Function(alreadyDefined || hasBody, global) };

        if (functionDeclaration.Body != null)
        {
            foreach (var param in functionDeclaration.Parameters)
                symbolTable.Add(param, new SymbolEntry() { Type = new Type.Int() });
            TypeCheckBlock(functionDeclaration.Body, symbolTable);
        }
    }

    private void TypeCheckBlock(Block block, Dictionary<string, SymbolEntry> symbolTable)
    {
        foreach (var item in block.BlockItems)
        {
            if (item is Declaration.VariableDeclaration declaration)
            {
                TypeCheckLocalVariableDeclaration(declaration, symbolTable);
            }
            else if (item is Declaration.FunctionDeclaration functionDeclaration)
            {
                if (functionDeclaration.StorageClass != Declaration.StorageClasses.Static)
                    TypeCheckFunctionDeclaration(functionDeclaration, symbolTable);
                else
                    throw new Exception("Type Error: StorageClass static used in block function declaration");
            }
            else if (item is Statement statement)
            {
                TypeCheckStatement(statement, symbolTable);
            }
        }
    }

    private void TypeCheckFileScopeVariableDeclaration(Declaration.VariableDeclaration variableDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        InitialValue initialValue;
        if (variableDeclaration.Initializer is Expression.ConstantExpression constant)
            initialValue = new InitialValue.Initial(constant.Value);
        else if (variableDeclaration.Initializer == null)
        {
            if (variableDeclaration.StorageClass == Declaration.StorageClasses.Extern)
                initialValue = new InitialValue.NoInitializer();
            else
                initialValue = new InitialValue.Tentative();
        }
        else
            throw new Exception("Type Error: Non-Constant initializer");

        var global = variableDeclaration.StorageClass != Declaration.StorageClasses.Static;

        if (symbolTable.TryGetValue(variableDeclaration.Identifier, out SymbolEntry prevEntry))
        {
            var attributes = (IdentifierAttributes.Static)prevEntry.IdentifierAttributes;
            if (prevEntry.Type is not Type.Int)
                throw new Exception("Type Error: Function redeclared as variable");
            if (variableDeclaration.StorageClass == Declaration.StorageClasses.Extern)
                global = attributes.Global;
            else if (attributes.Global != global)
                throw new Exception("Type Error: Conflicting variable linkage");

            if (attributes.InitialValue is InitialValue.Initial)
                if (initialValue is InitialValue.Initial)
                    throw new Exception("Type Error: Conflicting file scope variable definitions");
                else
                    initialValue = attributes.InitialValue;
            else if (initialValue is not InitialValue.Initial && attributes.InitialValue is InitialValue.Tentative)
                initialValue = new InitialValue.Tentative();
        }

        symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = new Type.Int(), IdentifierAttributes = new IdentifierAttributes.Static(initialValue, global) };
    }

    private void TypeCheckLocalVariableDeclaration(Declaration.VariableDeclaration variableDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        if (variableDeclaration.StorageClass == Declaration.StorageClasses.Extern)
        {
            if (variableDeclaration.Initializer != null)
                throw new Exception("Type Error: Initializer on local extern variable declaration");
            if (symbolTable.TryGetValue(variableDeclaration.Identifier, out SymbolEntry prevEntry))
            {
                if (prevEntry.Type is not Type.Int)
                    throw new Exception("Type Error: Function redeclared as variable");
            }
            else
                symbolTable.Add(variableDeclaration.Identifier, new SymbolEntry() { Type = new Type.Int(), IdentifierAttributes = new IdentifierAttributes.Static(new InitialValue.NoInitializer(), true) });
        }
        else if (variableDeclaration.StorageClass == Declaration.StorageClasses.Static)
        {
            InitialValue initialValue;
            if (variableDeclaration.Initializer is Expression.ConstantExpression constant)
                initialValue = new InitialValue.Initial(constant.Value);
            else if (variableDeclaration.Initializer == null)
                initialValue = new InitialValue.Initial(0);
            else
                throw new Exception("Type Error: Non-constant initializer on local static variable");
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = new Type.Int(), IdentifierAttributes = new IdentifierAttributes.Static(initialValue, false) };
        }
        else
        {
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = new Type.Int(), IdentifierAttributes = new IdentifierAttributes.Local() };
            if (variableDeclaration.Initializer != null)
                TypeCheckExpression(variableDeclaration.Initializer, symbolTable);
        }
    }

    private void TypeCheckExpression(Expression expression, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (expression)
        {
            case Expression.AssignmentExpression assignmentExpression:
                TypeCheckExpression(assignmentExpression.ExpressionLeft, symbolTable);
                TypeCheckExpression(assignmentExpression.ExpressionRight, symbolTable);
                break;
            case Expression.VariableExpression variableExpression:
                if (symbolTable[variableExpression.Identifier].Type is not Type.Int)
                    throw new Exception("Type Error: Function name used as variable");
                break;
            case Expression.UnaryExpression unaryExpression:
                TypeCheckExpression(unaryExpression.Expression, symbolTable);
                break;
            case Expression.BinaryExpression binaryExpression:
                TypeCheckExpression(binaryExpression.ExpressionLeft, symbolTable);
                TypeCheckExpression(binaryExpression.ExpressionRight, symbolTable);
                break;
            case Expression.ConstantExpression:
                break;
            case Expression.ConditionalExpression conditionalExpression:
                TypeCheckExpression(conditionalExpression.Condition, symbolTable);
                TypeCheckExpression(conditionalExpression.Then, symbolTable);
                TypeCheckExpression(conditionalExpression.Else, symbolTable);
                break;
            case Expression.FunctionCallExpression functionCallExpression:
                var funType = symbolTable[functionCallExpression.Identifier].Type;
                if (funType is Type.Int)
                    throw new Exception("Type Error: Variable used as function name");

                if (funType is Type.FunctionType functionType && functionType.ParameterCount != functionCallExpression.Arguments.Count)
                    throw new Exception("Type Error: Function called with the wrong number of arguments");

                foreach (var arg in functionCallExpression.Arguments)
                    TypeCheckExpression(arg, symbolTable);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void TypeCheckStatement(Statement statement, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (statement)
        {
            case Statement.ReturnStatement ret:
                TypeCheckExpression(ret.Expression, symbolTable);
                break;
            case Statement.ExpressionStatement expressionStatement:
                TypeCheckExpression(expressionStatement.Expression, symbolTable);
                break;
            case Statement.NullStatement:
                break;
            case Statement.IfStatement ifStatement:
                TypeCheckExpression(ifStatement.Condition, symbolTable);
                TypeCheckStatement(ifStatement.Then, symbolTable);
                if (ifStatement.Else != null)
                    TypeCheckStatement(ifStatement.Else, symbolTable);
                break;
            case Statement.CompoundStatement compoundStatement:
                TypeCheckBlock(compoundStatement.Block, symbolTable);
                break;
            case Statement.BreakStatement:
                break;
            case Statement.ContinueStatement:
                break;
            case Statement.WhileStatement whileStatement:
                TypeCheckExpression(whileStatement.Condition, symbolTable);
                TypeCheckStatement(whileStatement.Body, symbolTable);
                break;
            case Statement.DoWhileStatement doWhileStatement:
                TypeCheckStatement(doWhileStatement.Body, symbolTable);
                TypeCheckExpression(doWhileStatement.Condition, symbolTable);
                break;
            case Statement.ForStatement forStatement:
                {
                    TypeCheckForInit(forStatement.Init, symbolTable);
                    TypeCheckOptionalExpression(forStatement.Condition, symbolTable);
                    TypeCheckOptionalExpression(forStatement.Post, symbolTable);
                    TypeCheckStatement(forStatement.Body, symbolTable);
                    break;
                }
            default:
                throw new NotImplementedException();
        }
    }

    private void TypeCheckForInit(ForInit init, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (init)
        {
            case ForInit.InitExpression initExpression:
                TypeCheckOptionalExpression(initExpression.Expression, symbolTable);
                break;
            case ForInit.InitDeclaration initDeclaration:
                if (initDeclaration.Declaration.StorageClass == null)
                    TypeCheckLocalVariableDeclaration(initDeclaration.Declaration, symbolTable);
                else
                    throw new Exception("Type Error: StorageClass used in for loop init");
                break;
        }
    }

    private void TypeCheckOptionalExpression(Expression? expression, Dictionary<string, SymbolEntry> symbolTable)
    {
        if (expression != null)
            TypeCheckExpression(expression, symbolTable);
    }
}