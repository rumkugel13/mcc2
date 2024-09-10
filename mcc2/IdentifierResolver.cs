using mcc2.AST;
using static mcc2.SemanticAnalyzer;

namespace mcc2;

public class IdentifierResolver
{
    private int varCounter;
    
    private struct MapEntry
    {
        public string NewName;
        public bool FromCurrentScope, HasLinkage;
    }

    private struct StructMapEntry
    {
        public string NewName;
        public bool FromCurrentScope;
    }

    public void Resolve(ASTProgram program)
    {
        Dictionary<string, MapEntry> identifierMap = [];
        Dictionary<string, StructMapEntry> structMap = [];
        for (int i = 0; i < program.Declarations.Count; i++)
        {
            Declaration? decl = program.Declarations[i];
            if (decl is Declaration.FunctionDeclaration fun)
                program.Declarations[i] = ResolveFunctionDeclaration(fun, identifierMap, structMap);
            else if (decl is Declaration.VariableDeclaration var)
                program.Declarations[i] = ResolveFileScopeVariableDeclaration(var, identifierMap, structMap);
            else if (decl is Declaration.StructDeclaration structDecl)
                program.Declarations[i] = ResolveStructureDeclaration(structDecl, structMap);
        }
    }

    private Declaration.FunctionDeclaration ResolveFunctionDeclaration(Declaration.FunctionDeclaration functionDeclaration, Dictionary<string, MapEntry> identifierMap, Dictionary<string, StructMapEntry> structMap)
    {
        if (identifierMap.TryGetValue(functionDeclaration.Identifier, out MapEntry prevEntry))
        {
            if (prevEntry.FromCurrentScope && !prevEntry.HasLinkage)
                throw SemanticError("Duplicate declaration");
        }

        var newType = ResolveType(functionDeclaration.FunctionType, structMap);

        identifierMap[functionDeclaration.Identifier] = new MapEntry() { NewName = functionDeclaration.Identifier, FromCurrentScope = true, HasLinkage = true };

        var innerMap = CopyIdentifierMap(identifierMap);
        var innerStructMap = CopyStructMap(structMap);
        List<string> newParams = [];
        foreach (var param in functionDeclaration.Parameters)
        {
            newParams.Add(ResolveParameter(param, innerMap, innerStructMap));
        }

        Block? newBody = null;
        if (functionDeclaration.Body != null)
        {
            newBody = ResolveBlock(functionDeclaration.Body, innerMap, innerStructMap);
        }
        return new Declaration.FunctionDeclaration(functionDeclaration.Identifier, newParams, newBody, newType, functionDeclaration.StorageClass);
    }

    private string ResolveParameter(string parameter, Dictionary<string, MapEntry> identifierMap, Dictionary<string, StructMapEntry> structMap)
    {
        if (identifierMap.TryGetValue(parameter, out MapEntry newVariable) && newVariable.FromCurrentScope)
            throw SemanticError("Duplicate parameter declaration");

        var uniqueName = MakeTemporary(parameter);
        identifierMap[parameter] = new MapEntry() { NewName = uniqueName, FromCurrentScope = true };
        return uniqueName;
    }

    private Declaration.VariableDeclaration ResolveFileScopeVariableDeclaration(Declaration.VariableDeclaration var, Dictionary<string, MapEntry> identifierMap, Dictionary<string, StructMapEntry> structMap)
    {
        var newType = ResolveType(var.VariableType, structMap);
        identifierMap[var.Identifier] = new MapEntry() { NewName = var.Identifier, FromCurrentScope = true, HasLinkage = true };
        return new Declaration.VariableDeclaration(var.Identifier, var.Initializer, newType, var.StorageClass);
    }

    private Declaration.StructDeclaration ResolveStructureDeclaration(Declaration.StructDeclaration structDeclaration, Dictionary<string, StructMapEntry> structMap)
    {
        string uniqueTag;
        if (!structMap.TryGetValue(structDeclaration.Identifier, out var prevEntry) || !prevEntry.FromCurrentScope)
        {
            uniqueTag = MakeTemporary(structDeclaration.Identifier);
            structMap[structDeclaration.Identifier] = new StructMapEntry() {NewName = uniqueTag, FromCurrentScope = true};
        }
        else
            uniqueTag = prevEntry.NewName;

        List<MemberDeclaration> processedMembers = [];
        foreach (var member in structDeclaration.Members)
        {
            var processedType = ResolveType(member.MemberType, structMap);
            var processedMember = new MemberDeclaration(member.MemberName, processedType);
            processedMembers.Add(processedMember);
        }
        return new Declaration.StructDeclaration(uniqueTag, processedMembers);
    }

    private Block ResolveBlock(Block block, Dictionary<string, MapEntry> identifierMap, Dictionary<string, StructMapEntry> structMap)
    {
        List<BlockItem> newItems = [];
        foreach (var item in block.BlockItems)
        {
            if (item is Declaration.VariableDeclaration declaration)
            {
                newItems.Add(ResolveVariableDeclaration(declaration, identifierMap, structMap));
            }
            else if (item is Declaration.FunctionDeclaration functionDeclaration)
            {
                if (functionDeclaration.Body != null)
                    throw SemanticError("Local function definition");

                newItems.Add(ResolveFunctionDeclaration(functionDeclaration, identifierMap, structMap));
            }
            else if (item is Declaration.StructDeclaration structDecl)
            {
                newItems.Add(ResolveStructureDeclaration(structDecl, structMap));
            }
            else if (item is Statement statement)
            {
                newItems.Add(ResolveStatement(statement, identifierMap, structMap));
            }
        }
        return new Block(newItems);
    }

    private Statement ResolveStatement(Statement statement, Dictionary<string, MapEntry> identifierMap, Dictionary<string, StructMapEntry> structMap)
    {
        switch (statement)
        {
            case Statement.ReturnStatement ret:
                if (ret.Expression == null)
                    return ret;
                else
                    return new Statement.ReturnStatement(ResolveExpression(ret.Expression, identifierMap, structMap));
            case Statement.ExpressionStatement expressionStatement:
                return new Statement.ExpressionStatement(ResolveExpression(expressionStatement.Expression, identifierMap, structMap));
            case Statement.NullStatement nullStatement:
                return nullStatement;
            case Statement.IfStatement ifStatement:
                {
                    var cond = ResolveExpression(ifStatement.Condition, identifierMap, structMap);
                    var then = ResolveStatement(ifStatement.Then, identifierMap, structMap);
                    Statement? el = ifStatement.Else;
                    if (el != null)
                        el = ResolveStatement(el, identifierMap, structMap);
                    return new Statement.IfStatement(cond, then, el);
                }
            case Statement.CompoundStatement compoundStatement:
                {
                    var newIdentifierMap = CopyIdentifierMap(identifierMap);
                    var innerStructMap = CopyStructMap(structMap);
                    return new Statement.CompoundStatement(ResolveBlock(compoundStatement.Block, newIdentifierMap, innerStructMap));
                }
            case Statement.BreakStatement breakStatement:
                return breakStatement;
            case Statement.ContinueStatement continueStatement:
                return continueStatement;
            case Statement.WhileStatement whileStatement:
                {
                    var cond = ResolveExpression(whileStatement.Condition, identifierMap, structMap);
                    var body = ResolveStatement(whileStatement.Body, identifierMap, structMap);
                    return new Statement.WhileStatement(cond, body, whileStatement.Label);
                }
            case Statement.DoWhileStatement doWhileStatement:
                {
                    var body = ResolveStatement(doWhileStatement.Body, identifierMap, structMap);
                    var cond = ResolveExpression(doWhileStatement.Condition, identifierMap, structMap);
                    return new Statement.DoWhileStatement(body, cond, doWhileStatement.Label);
                }
            case Statement.ForStatement forStatement:
                {
                    var newVarMap = CopyIdentifierMap(identifierMap);
                    var innerStructMap = CopyStructMap(structMap);
                    var init = ResolveForInit(forStatement.Init, newVarMap, innerStructMap);
                    var cond = ResolveOptionalExpression(forStatement.Condition, newVarMap, innerStructMap);
                    var post = ResolveOptionalExpression(forStatement.Post, newVarMap, innerStructMap);
                    var body = ResolveStatement(forStatement.Body, newVarMap, innerStructMap);
                    return new Statement.ForStatement(init, cond, post, body, forStatement.Label);
                }
            default:
                throw new NotImplementedException();
        }
    }

    private Expression? ResolveOptionalExpression(Expression? expression, Dictionary<string, MapEntry> identifierMap, Dictionary<string, StructMapEntry> structMap)
    {
        if (expression != null)
            return ResolveExpression(expression, identifierMap, structMap);
        else
            return null;
    }

    private ForInit ResolveForInit(ForInit init, Dictionary<string, MapEntry> identifierMap, Dictionary<string, StructMapEntry> structMap)
    {
        switch (init)
        {
            case ForInit.InitExpression initExpression:
                return new ForInit.InitExpression(ResolveOptionalExpression(initExpression.Expression, identifierMap, structMap));
            case ForInit.InitDeclaration initDeclaration:
                return new ForInit.InitDeclaration(ResolveVariableDeclaration(initDeclaration.Declaration, identifierMap, structMap));
            default:
                throw new NotImplementedException();
        }
    }

    private Expression ResolveExpression(Expression expression, Dictionary<string, MapEntry> identifierMap, Dictionary<string, StructMapEntry> structMap)
    {
        switch (expression)
        {
            case Expression.Assignment assignment:
                {
                    var left = ResolveExpression(assignment.Left, identifierMap, structMap);
                    var right = ResolveExpression(assignment.Right, identifierMap, structMap);
                    return new Expression.Assignment(left, right, assignment.Type);
                }
            case Expression.Variable variable:
                if (identifierMap.TryGetValue(variable.Identifier, out MapEntry newVariable))
                    return new Expression.Variable(newVariable.NewName, variable.Type);
                else
                    throw SemanticError("Undeclared variable");
            case Expression.Unary unary:
                {
                    var exp = ResolveExpression(unary.Expression, identifierMap, structMap);
                    return new Expression.Unary(unary.Operator, exp, unary.Type);
                }
            case Expression.Binary binary:
                {
                    var left = ResolveExpression(binary.Left, identifierMap, structMap);
                    var right = ResolveExpression(binary.Right, identifierMap, structMap);
                    return new Expression.Binary(binary.Operator, left, right, binary.Type);
                }
            case Expression.Constant constant:
                return constant;
            case Expression.Conditional conditional:
                {
                    var cond = ResolveExpression(conditional.Condition, identifierMap, structMap);
                    var then = ResolveExpression(conditional.Then, identifierMap, structMap);
                    var el = ResolveExpression(conditional.Else, identifierMap, structMap);
                    return new Expression.Conditional(cond, then, el, conditional.Type);
                }
            case Expression.FunctionCall functionCall:
                if (identifierMap.TryGetValue(functionCall.Identifier, out MapEntry entry))
                {
                    var newFunName = entry.NewName;
                    List<Expression> newArgs = [];
                    foreach (var arg in functionCall.Arguments)
                        newArgs.Add(ResolveExpression(arg, identifierMap, structMap));
                    return new Expression.FunctionCall(newFunName, newArgs, functionCall.Type);
                }
                else
                    throw SemanticError("Undeclared function");
            case Expression.Cast cast:
                {
                    var newType = ResolveType(cast.TargetType, structMap);
                    return new Expression.Cast(newType, ResolveExpression(cast.Expression, identifierMap, structMap), cast.Type);
                }
            case Expression.Dereference dereference:
                {
                    var exp = ResolveExpression(dereference.Expression, identifierMap, structMap);
                    return new Expression.Dereference(exp, dereference.Type);
                }
            case Expression.AddressOf addressOf:
                {
                    var exp = ResolveExpression(addressOf.Expression, identifierMap, structMap);
                    return new Expression.AddressOf(exp, addressOf.Type);
                }
            case Expression.Subscript subscript:
                {
                    var left = ResolveExpression(subscript.Left, identifierMap, structMap);
                    var right = ResolveExpression(subscript.Right, identifierMap, structMap);
                    return new Expression.Subscript(left, right, subscript.Type);
                }
            case Expression.String stringExp:
                return stringExp;
            case Expression.SizeOf sizeofExp:
                {
                    var exp = ResolveExpression(sizeofExp.Expression, identifierMap, structMap);
                    return new Expression.SizeOf(exp, sizeofExp.Type);
                }
            case Expression.SizeOfType sizeofType:
                {
                    var newType = ResolveType(sizeofType.TargetType, structMap);
                    return new Expression.SizeOfType(newType, sizeofType.Type);
                }
            case Expression.Dot dot:
                {
                    var exp = ResolveExpression(dot.Structure, identifierMap, structMap);
                    return new Expression.Dot(exp, dot.Member, dot.Type);
                }
            case Expression.Arrow arrow:
                {
                    var exp = ResolveExpression(arrow.Pointer, identifierMap, structMap);
                    return new Expression.Arrow(exp, arrow.Member, arrow.Type);
                }
            default:
                throw new NotImplementedException();
        }
    }

    private Declaration.VariableDeclaration ResolveVariableDeclaration(Declaration.VariableDeclaration declaration, Dictionary<string, MapEntry> identifierMap, Dictionary<string, StructMapEntry> structMap)
    {
        if (identifierMap.TryGetValue(declaration.Identifier, out MapEntry prevEntry))
        {
            if (prevEntry.FromCurrentScope)
                if (!(prevEntry.HasLinkage && declaration.StorageClass == Declaration.StorageClasses.Extern))
                    throw SemanticError("Conflicting local declarations");
        }

        var newType = ResolveType(declaration.VariableType, structMap);

        if (declaration.StorageClass == Declaration.StorageClasses.Extern)
        {
            identifierMap[declaration.Identifier] = new MapEntry() { NewName = declaration.Identifier, FromCurrentScope = true, HasLinkage = true };
            return new Declaration.VariableDeclaration(declaration.Identifier, declaration.Initializer, newType, declaration.StorageClass);
        }

        var uniqueName = MakeTemporary(declaration.Identifier);
        identifierMap[declaration.Identifier] = new MapEntry() { NewName = uniqueName, FromCurrentScope = true, HasLinkage = false };
        var init = declaration.Initializer;
        if (init != null)
            init = ResolveInitializer(init, identifierMap, structMap);
        return new Declaration.VariableDeclaration(uniqueName, init, newType, declaration.StorageClass);
    }

    private Initializer ResolveInitializer(Initializer initializer, Dictionary<string, MapEntry> identifierMap, Dictionary<string, StructMapEntry> structMap)
    {
        switch (initializer)
        {
            case Initializer.SingleInitializer single:
                return new Initializer.SingleInitializer(ResolveExpression(single.Expression, identifierMap, structMap), single.Type);
            case Initializer.CompoundInitializer compound:
                List<Initializer> initializers = [];
                foreach (var init in compound.Initializers)
                    initializers.Add(ResolveInitializer(init, identifierMap, structMap));
                return new Initializer.CompoundInitializer(initializers, compound.Type);
            default:
                throw new NotImplementedException();
        }
    }

    private Type ResolveType(Type type, Dictionary<string, StructMapEntry> structMap)
    {
        switch (type)
        {
            case Type.Structure structure:
                if (structMap.TryGetValue(structure.Identifier, out var structEntry))
                {
                    var uniqueTag = structEntry.NewName;
                    return new Type.Structure(uniqueTag);
                }
                else
                    throw SemanticError("Specified an undeclared structure type");
            case Type.Pointer pointer:
                var resolvedType = ResolveType(pointer.Referenced, structMap);
                return new Type.Pointer(resolvedType);
            case Type.Array array:
                var resolvedArrayType = ResolveType(array.Element, structMap);
                return new Type.Array(resolvedArrayType, array.Size);
            case Type.FunctionType funcType:
                List<Type> newParams = [];
                foreach (var paramType in funcType.Parameters)
                {
                    var resolved = ResolveType(paramType, structMap);
                    newParams.Add(resolved);
                }
                var resolvedReturn = ResolveType(funcType.Return, structMap);
                return new Type.FunctionType(newParams, resolvedReturn);
            default:
                return type;
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

    private Dictionary<string, StructMapEntry> CopyStructMap(Dictionary<string, StructMapEntry> structMap)
    {
        Dictionary<string, StructMapEntry> newMap = [];
        foreach (var item in structMap)
        {
            newMap.Add(item.Key, new StructMapEntry() { NewName = item.Value.NewName, FromCurrentScope = false });
        }
        return newMap;
    }

    private string MakeTemporary(string varName)
    {
        return $"var.{varName}.{varCounter++}";
    }

    private Exception SemanticError(string message)
    {
        return new Exception("Semantic Error: " + message);
    }
}