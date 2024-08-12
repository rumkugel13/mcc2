using mcc2.AST;
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
        public bool Defined;
    }

    public void Analyze(ASTProgram program, Dictionary<string, SymbolEntry> symbolTable)
    {
        Dictionary<string, MapEntry> identifierMap = [];
        foreach (var decl in program.Declarations)
        {
            if (decl is FunctionDeclaration fun)
                ResolveFunctionDeclaration(fun, identifierMap);
        }

        foreach (var decl in program.Declarations)
        {
            if (decl is FunctionDeclaration fun)
                TypeCheckFunctionDeclaration(fun, symbolTable);
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

        if (symbolTable.TryGetValue(functionDeclaration.Identifier, out SymbolEntry prevEntry))
        {
            if (prevEntry.Type is not FunctionType funcA || funcA.ParameterCount != funType.ParameterCount)
                throw new Exception("Type Error: Incompatible function declarations");
            alreadyDefined = prevEntry.Defined;
            if (alreadyDefined && hasBody)
                throw new Exception("Type Error: Function is defined more than once");
        }

        symbolTable[functionDeclaration.Identifier] = new SymbolEntry() { Type = funType, Defined = alreadyDefined || hasBody };

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
                TypeCheckVariableDeclaration(declaration, symbolTable);
            }
            else if (item is FunctionDeclaration functionDeclaration)
            {
                TypeCheckFunctionDeclaration(functionDeclaration, symbolTable);
            }
            else if (item is Statement statement)
            {
                TypeCheckStatement(statement, symbolTable);
            }
        }
    }

    private void TypeCheckVariableDeclaration(VariableDeclaration variableDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        symbolTable.Add(variableDeclaration.Identifier, new SymbolEntry() { Type = new Int() });
        if (variableDeclaration.Initializer != null)
            TypeCheckExpression(variableDeclaration.Initializer, symbolTable);
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
                TypeCheckVariableDeclaration(initDeclaration.Declaration, symbolTable);
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
        if (identifierMap.TryGetValue(declaration.Identifier, out MapEntry newVariable) && newVariable.FromCurrentScope)
            throw new Exception("Semantic Error: Duplicate variable declaration");

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