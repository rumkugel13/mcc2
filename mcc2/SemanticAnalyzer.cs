using mcc2.AST;
using mcc2.Attributes;
using mcc2.Types;

namespace mcc2;

public class SemanticAnalyzer
{
    private int varCounter;
    private int loopCounter;

    private struct MapEntry
    {
        public string NewName;
        public bool FromCurrentScope, HasLinkage;
    }

    public struct SymbolEntry
    {
        public Types.Type Type;
        public IdentifierAttributes IdentifierAttributes;
    }

    public void Analyze(ASTProgram program, Dictionary<string, SymbolEntry> symbolTable)
    {
        Dictionary<string, MapEntry> identifierMap = [];
        foreach (var decl in program.Declarations)
        {
            if (decl is FunctionDeclaration fun)
                ResolveFunctionDeclaration(fun, identifierMap);
            else if (decl is VariableDeclaration var)
                ResolveFileScopeVariableDeclaration(var, identifierMap);
        }

        foreach (var decl in program.Declarations)
        {
            if (decl is FunctionDeclaration fun)
                TypeCheckFunctionDeclaration(fun, symbolTable);
            else if (decl is VariableDeclaration var)
                TypeCheckFileScopeVariableDeclaration(var, symbolTable);
        }

        foreach (var decl in program.Declarations)
        {
            if (decl is FunctionDeclaration fun)
                LabelFunction(fun, null);
        }
    }

    private void TypeCheckFunctionDeclaration(FunctionDeclaration functionDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        FunctionType funType = new FunctionType(functionDeclaration.Parameters.Count);
        bool hasBody = functionDeclaration.Body != null;
        bool alreadyDefined = false;
        bool global = functionDeclaration.StorageClass != Declaration.StorageClasses.Static;

        if (symbolTable.TryGetValue(functionDeclaration.Identifier, out SymbolEntry prevEntry))
        {
            var attributes = (FunctionAttributes)prevEntry.IdentifierAttributes;
            // note: check correct type and number of parameters
            if (prevEntry.Type is not FunctionType funcA || funcA.ParameterCount != funType.ParameterCount)
                throw new Exception("Type Error: Incompatible function declarations");

            alreadyDefined = attributes.Defined;
            if (alreadyDefined && hasBody)
                throw new Exception("Type Error: Function is defined more than once");

            if (attributes.Global && functionDeclaration.StorageClass == Declaration.StorageClasses.Static)
                throw new Exception("Static function declaration follows non-static");

            global = attributes.Global;
        }

        symbolTable[functionDeclaration.Identifier] = new SymbolEntry() { Type = funType, IdentifierAttributes = new FunctionAttributes(alreadyDefined || hasBody, global) };

        if (functionDeclaration.Body != null)
        {
            foreach (var param in functionDeclaration.Parameters)
                symbolTable.Add(param, new SymbolEntry() { Type = new Int() });
            TypeCheckBlock(functionDeclaration.Body, symbolTable);
        }
    }

    private void TypeCheckBlock(Block block, Dictionary<string, SymbolEntry> symbolTable)
    {
        foreach (var item in block.BlockItems)
        {
            if (item is VariableDeclaration declaration)
            {
                TypeCheckLocalVariableDeclaration(declaration, symbolTable);
            }
            else if (item is FunctionDeclaration functionDeclaration)
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

    private void TypeCheckFileScopeVariableDeclaration(VariableDeclaration variableDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        InitialValue initialValue;
        if (variableDeclaration.Initializer is ConstantExpression constant)
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
            var attributes = (StaticAttributes)prevEntry.IdentifierAttributes;
            if (prevEntry.Type is not Int)
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

        symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = new Int(), IdentifierAttributes = new StaticAttributes(initialValue, global) };
    }

    private void TypeCheckLocalVariableDeclaration(VariableDeclaration variableDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        if (variableDeclaration.StorageClass == Declaration.StorageClasses.Extern)
        {
            if (variableDeclaration.Initializer != null)
                throw new Exception("Type Error: Initializer on local extern variable declaration");
            if (symbolTable.TryGetValue(variableDeclaration.Identifier, out SymbolEntry prevEntry))
            {
                if (prevEntry.Type is not Int)
                    throw new Exception("Type Error: Function redeclared as variable");
            }
            else
                symbolTable.Add(variableDeclaration.Identifier, new SymbolEntry() { Type = new Int(), IdentifierAttributes = new StaticAttributes(new InitialValue.NoInitializer(), true) });
        }
        else if (variableDeclaration.StorageClass == Declaration.StorageClasses.Static)
        {
            InitialValue initialValue;
            if (variableDeclaration.Initializer is ConstantExpression constant)
                initialValue = new InitialValue.Initial(constant.Value);
            else if (variableDeclaration.Initializer == null)
                initialValue = new InitialValue.Initial(0);
            else
                throw new Exception("Type Error: Non-constant initializer on local static variable");
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = new Int(), IdentifierAttributes = new StaticAttributes(initialValue, false) };
        }
        else
        {
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = new Int(), IdentifierAttributes = new LocalAttributes() };
            if (variableDeclaration.Initializer != null)
                TypeCheckExpression(variableDeclaration.Initializer, symbolTable);
        }
    }

    private void TypeCheckExpression(Expression expression, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (expression)
        {
            case AssignmentExpression assignmentExpression:
                TypeCheckExpression(assignmentExpression.ExpressionLeft, symbolTable);
                TypeCheckExpression(assignmentExpression.ExpressionRight, symbolTable);
                break;
            case VariableExpression variableExpression:
                if (symbolTable[variableExpression.Identifier].Type is not Int)
                    throw new Exception("Type Error: Function name used as variable");
                break;
            case UnaryExpression unaryExpression:
                TypeCheckExpression(unaryExpression.Expression, symbolTable);
                break;
            case BinaryExpression binaryExpression:
                TypeCheckExpression(binaryExpression.ExpressionLeft, symbolTable);
                TypeCheckExpression(binaryExpression.ExpressionRight, symbolTable);
                break;
            case ConstantExpression:
                break;
            case ConditionalExpression conditionalExpression:
                TypeCheckExpression(conditionalExpression.Condition, symbolTable);
                TypeCheckExpression(conditionalExpression.Then, symbolTable);
                TypeCheckExpression(conditionalExpression.Else, symbolTable);
                break;
            case FunctionCallExpression functionCallExpression:
                var funType = symbolTable[functionCallExpression.Identifier].Type;
                if (funType is Int)
                    throw new Exception("Type Error: Variable used as function name");

                if (funType is FunctionType functionType && functionType.ParameterCount != functionCallExpression.Arguments.Count)
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
            case ReturnStatement ret:
                TypeCheckExpression(ret.Expression, symbolTable);
                break;
            case ExpressionStatement expressionStatement:
                TypeCheckExpression(expressionStatement.Expression, symbolTable);
                break;
            case NullStatement:
                break;
            case IfStatement ifStatement:
                TypeCheckExpression(ifStatement.Condition, symbolTable);
                TypeCheckStatement(ifStatement.Then, symbolTable);
                if (ifStatement.Else != null)
                    TypeCheckStatement(ifStatement.Else, symbolTable);
                break;
            case CompoundStatement compoundStatement:
                TypeCheckBlock(compoundStatement.Block, symbolTable);
                break;
            case BreakStatement:
                break;
            case ContinueStatement:
                break;
            case WhileStatement whileStatement:
                TypeCheckExpression(whileStatement.Condition, symbolTable);
                TypeCheckStatement(whileStatement.Body, symbolTable);
                break;
            case DoWhileStatement doWhileStatement:
                TypeCheckStatement(doWhileStatement.Body, symbolTable);
                TypeCheckExpression(doWhileStatement.Condition, symbolTable);
                break;
            case ForStatement forStatement:
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
            case InitExpression initExpression:
                TypeCheckOptionalExpression(initExpression.Expression, symbolTable);
                break;
            case InitDeclaration initDeclaration:
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

    private void LabelFunction(FunctionDeclaration functionDeclaration, string? currentLabel)
    {
        if (functionDeclaration.Body != null)
        {
            LabelBlock(functionDeclaration.Body, null);
        }
    }

    private void LabelBlock(Block block, string? currentLabel)
    {
        foreach (var item in block.BlockItems)
        {
            if (item is Statement statement)
                LabelStatement(statement, currentLabel);
        }
    }

    private void LabelStatement(Statement statement, string? currentLabel)
    {
        switch (statement)
        {
            case ReturnStatement:
                break;
            case ExpressionStatement:
                break;
            case NullStatement:
                break;
            case IfStatement ifStatement:
                LabelStatement(ifStatement.Then, currentLabel);
                if (ifStatement.Else != null)
                    LabelStatement(ifStatement.Else, currentLabel);
                break;
            case CompoundStatement compoundStatement:
                LabelBlock(compoundStatement.Block, currentLabel);
                break;
            case BreakStatement breakStatement:
                if (currentLabel == null)
                    throw new Exception("Semantic Error: Break statement outside of loop");
                breakStatement.Label = currentLabel;
                break;
            case ContinueStatement continueStatement:
                if (currentLabel == null)
                    throw new Exception("Semantic Error: Continue statement outside of loop");
                continueStatement.Label = currentLabel;
                break;
            case WhileStatement whileStatement:
                {
                    var newLabel = MakeLabel();
                    LabelStatement(whileStatement.Body, newLabel);
                    whileStatement.Label = newLabel;
                    break;
                }
            case DoWhileStatement doWhileStatement:
                {
                    var newLabel = MakeLabel();
                    LabelStatement(doWhileStatement.Body, newLabel);
                    doWhileStatement.Label = newLabel;
                    break;
                }
            case ForStatement forStatement:
                {
                    var newLabel = MakeLabel();
                    LabelStatement(forStatement.Body, newLabel);
                    forStatement.Label = newLabel;
                    break;
                }
            default:
                throw new NotImplementedException();
        }
    }

    private string MakeLabel()
    {
        return $"loop.{loopCounter++}";
    }

    private void ResolveFunctionDeclaration(FunctionDeclaration functionDeclaration, Dictionary<string, MapEntry> identifierMap)
    {
        if (identifierMap.TryGetValue(functionDeclaration.Identifier, out MapEntry prevEntry))
        {
            if (prevEntry.FromCurrentScope && !prevEntry.HasLinkage)
                throw new Exception("Semantic Error: Duplicate declaration");
        }

        identifierMap[functionDeclaration.Identifier] = new MapEntry() { NewName = functionDeclaration.Identifier, FromCurrentScope = true, HasLinkage = true };

        var innerMap = CopyIdentifierMap(identifierMap);
        for (int i = 0; i < functionDeclaration.Parameters.Count; i++)
        {
            string? parameter = functionDeclaration.Parameters[i];
            ResolveParameter(ref parameter, innerMap);
            functionDeclaration.Parameters[i] = parameter;
        }

        if (functionDeclaration.Body != null)
        {
            ResolveBlock(functionDeclaration.Body, innerMap);
        }
    }

    private void ResolveParameter(ref string parameter, Dictionary<string, MapEntry> identifierMap)
    {
        if (identifierMap.TryGetValue(parameter, out MapEntry newVariable) && newVariable.FromCurrentScope)
            throw new Exception("Semantic Error: Duplicate parameter declaration");

        var uniqueName = MakeTemporary(parameter);
        identifierMap[parameter] = new MapEntry() { NewName = uniqueName, FromCurrentScope = true };
        parameter = uniqueName;
    }

    private void ResolveFileScopeVariableDeclaration(VariableDeclaration var, Dictionary<string, MapEntry> identifierMap)
    {
        identifierMap[var.Identifier] = new MapEntry() { NewName = var.Identifier, FromCurrentScope = true, HasLinkage = true };
    }

    private void ResolveBlock(Block block, Dictionary<string, MapEntry> identifierMap)
    {
        foreach (var item in block.BlockItems)
        {
            if (item is VariableDeclaration declaration)
            {
                ResolveVariableDeclaration(declaration, identifierMap);
            }
            else if (item is FunctionDeclaration functionDeclaration)
            {
                if (functionDeclaration.Body != null)
                    throw new Exception("Semantic Error: Local function definition");

                ResolveFunctionDeclaration(functionDeclaration, identifierMap);
            }
            else if (item is Statement statement)
            {
                ResolveStatement(statement, identifierMap);
            }
        }
    }

    private void ResolveStatement(Statement statement, Dictionary<string, MapEntry> identifierMap)
    {
        switch (statement)
        {
            case ReturnStatement ret:
                ResolveExpression(ret.Expression, identifierMap);
                break;
            case ExpressionStatement expressionStatement:
                ResolveExpression(expressionStatement.Expression, identifierMap);
                break;
            case NullStatement:
                break;
            case IfStatement ifStatement:
                ResolveExpression(ifStatement.Condition, identifierMap);
                ResolveStatement(ifStatement.Then, identifierMap);
                if (ifStatement.Else != null)
                    ResolveStatement(ifStatement.Else, identifierMap);
                break;
            case CompoundStatement compoundStatement:
                {
                    var newIdentifierMap = CopyIdentifierMap(identifierMap);
                    ResolveBlock(compoundStatement.Block, newIdentifierMap);
                    break;
                }
            case BreakStatement:
                break;
            case ContinueStatement:
                break;
            case WhileStatement whileStatement:
                ResolveExpression(whileStatement.Condition, identifierMap);
                ResolveStatement(whileStatement.Body, identifierMap);
                break;
            case DoWhileStatement doWhileStatement:
                ResolveStatement(doWhileStatement.Body, identifierMap);
                ResolveExpression(doWhileStatement.Condition, identifierMap);
                break;
            case ForStatement forStatement:
                {
                    var newVarMap = CopyIdentifierMap(identifierMap);
                    ResolveForInit(forStatement.Init, newVarMap);
                    ResolveOptionalExpression(forStatement.Condition, newVarMap);
                    ResolveOptionalExpression(forStatement.Post, newVarMap);
                    ResolveStatement(forStatement.Body, newVarMap);
                    break;
                }
            default:
                throw new NotImplementedException();
        }
    }

    private void ResolveOptionalExpression(Expression? expression, Dictionary<string, MapEntry> identifierMap)
    {
        if (expression != null)
            ResolveExpression(expression, identifierMap);
    }

    private void ResolveForInit(ForInit init, Dictionary<string, MapEntry> identifierMap)
    {
        switch (init)
        {
            case InitExpression initExpression:
                ResolveOptionalExpression(initExpression.Expression, identifierMap);
                break;
            case InitDeclaration initDeclaration:
                ResolveVariableDeclaration(initDeclaration.Declaration, identifierMap);
                break;
        }
    }

    private Dictionary<string, MapEntry> CopyIdentifierMap(Dictionary<string, MapEntry> identifierMap)
    {
        Dictionary<string, MapEntry> newMap = [];
        foreach (var item in identifierMap)
        {
            newMap.Add(item.Key, new MapEntry() { NewName = item.Value.NewName, FromCurrentScope = false });
        }
        return newMap;
    }

    private void ResolveExpression(Expression expression, Dictionary<string, MapEntry> identifierMap)
    {
        switch (expression)
        {
            case AssignmentExpression assignmentExpression:
                if (assignmentExpression.ExpressionLeft is not VariableExpression)
                    throw new Exception("Semantic Error: Invalid lvalue");

                ResolveExpression(assignmentExpression.ExpressionLeft, identifierMap);
                ResolveExpression(assignmentExpression.ExpressionRight, identifierMap);
                break;
            case VariableExpression variableExpression:
                if (identifierMap.TryGetValue(variableExpression.Identifier, out MapEntry newVariable))
                    variableExpression.Identifier = newVariable.NewName;
                else
                    throw new Exception("Semantic Error: Undeclared variable");
                break;
            case UnaryExpression unaryExpression:
                ResolveExpression(unaryExpression.Expression, identifierMap);
                break;
            case BinaryExpression binaryExpression:
                ResolveExpression(binaryExpression.ExpressionLeft, identifierMap);
                ResolveExpression(binaryExpression.ExpressionRight, identifierMap);
                break;
            case ConstantExpression:
                break;
            case ConditionalExpression conditionalExpression:
                ResolveExpression(conditionalExpression.Condition, identifierMap);
                ResolveExpression(conditionalExpression.Then, identifierMap);
                ResolveExpression(conditionalExpression.Else, identifierMap);
                break;
            case FunctionCallExpression functionCallExpression:
                if (identifierMap.TryGetValue(functionCallExpression.Identifier, out MapEntry entry))
                {
                    functionCallExpression.Identifier = entry.NewName;
                    foreach (var arg in functionCallExpression.Arguments)
                        ResolveExpression(arg, identifierMap);
                }
                else
                    throw new Exception("Semantic Error: Undeclared function");
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void ResolveVariableDeclaration(VariableDeclaration declaration, Dictionary<string, MapEntry> identifierMap)
    {
        if (identifierMap.TryGetValue(declaration.Identifier, out MapEntry prevEntry))
        {
            if (prevEntry.FromCurrentScope)
                if (!(prevEntry.HasLinkage && declaration.StorageClass == Declaration.StorageClasses.Extern))
                    throw new Exception("Semantic Error: Conflicting local declarations");
        }

        if (declaration.StorageClass == Declaration.StorageClasses.Extern)
        {
            identifierMap[declaration.Identifier] = new MapEntry() { NewName = declaration.Identifier, FromCurrentScope = true, HasLinkage = true };
            return;
        }

        var uniqueName = MakeTemporary(declaration.Identifier);
        identifierMap[declaration.Identifier] = new MapEntry() { NewName = uniqueName, FromCurrentScope = true, HasLinkage = false };
        if (declaration.Initializer != null)
        {
            ResolveExpression(declaration.Initializer, identifierMap);
        }
        declaration.Identifier = uniqueName;
    }

    private string MakeTemporary(string varName)
    {
        return $"var.{varName}.{varCounter++}";
    }
}