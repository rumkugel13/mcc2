using mcc2.AST;
using static mcc2.SemanticAnalyzer;

namespace mcc2;

public class TypeChecker
{
    private Type functionReturnType = new Type.Int();
    private int counter;

    public void Check(ASTProgram program, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        for (int i = 0; i < program.Declarations.Count; i++)
        {
            Declaration? decl = program.Declarations[i];
            if (decl is Declaration.FunctionDeclaration fun)
                program.Declarations[i] = TypeCheckFunctionDeclaration(fun, symbolTable, typeTable);
            else if (decl is Declaration.VariableDeclaration var)
                program.Declarations[i] = TypeCheckFileScopeVariableDeclaration(var, symbolTable, typeTable);
            else if (decl is Declaration.StructDeclaration structDecl)
                program.Declarations[i] = TypeCheckStructDeclaration(structDecl, symbolTable, typeTable);
        }
    }

    private Declaration.FunctionDeclaration TypeCheckFunctionDeclaration(Declaration.FunctionDeclaration functionDeclaration, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        Type.FunctionType funType = (Type.FunctionType)functionDeclaration.FunctionType;
        if (funType.Return is Type.Array)
            throw TypeError("A function cannot return an array");
        ValidateTypeSpecifier(funType, typeTable);

        List<Type> adjustedParams = [];
        foreach (var t in funType.Parameters)
        {
            switch (t)
            {
                case Type.Array array:
                    adjustedParams.Add(new Type.Pointer(array.Element));
                    break;
                case Type.Void:
                    throw TypeError("Parameters can't be of type void");
                default:
                    adjustedParams.Add(t);
                    break;
            }
        }
        funType.Parameters.Clear();
        funType.Parameters.AddRange(adjustedParams);

        bool hasBody = functionDeclaration.Body != null;
        bool alreadyDefined = false;
        bool global = functionDeclaration.StorageClass != Declaration.StorageClasses.Static;

        if (symbolTable.TryGetValue(functionDeclaration.Identifier, out SymbolEntry prevEntry))
        {
            var attributes = (IdentifierAttributes.Function)prevEntry.IdentifierAttributes;
            // note: check correct type and number of parameters
            var prevFunType = (Type.FunctionType)prevEntry.Type;
            if (prevFunType is not Type.FunctionType funcA || funcA.Parameters.Count != funType.Parameters.Count || funcA.Return != funType.Return)
                throw TypeError("Incompatible function declarations");

            for (int i = 0; i < prevFunType.Parameters.Count; i++)
            {
                if (prevFunType.Parameters[i] != funType.Parameters[i])
                    throw TypeError("Incompatible function declarations");
            }

            alreadyDefined = attributes.Defined;
            if (alreadyDefined && hasBody)
                throw TypeError("Function is defined more than once");

            if (attributes.Global && functionDeclaration.StorageClass == Declaration.StorageClasses.Static)
                throw TypeError("Static function declaration follows non-static");

            global = attributes.Global;
        }

        symbolTable[functionDeclaration.Identifier] = new SymbolEntry() { Type = funType, IdentifierAttributes = new IdentifierAttributes.Function(alreadyDefined || hasBody, global) };

        if (functionDeclaration.Body != null)
        {
            if (funType.Return is not Type.Void && !IsComplete(funType.Return, typeTable))
                throw TypeError("Return type of function definition must be complete");

            foreach (var paramType in funType.Parameters)
                if (!IsComplete(paramType, typeTable))
                    throw TypeError("Function parameter must be complete");

            functionReturnType = funType.Return;

            for (int i = 0; i < functionDeclaration.Parameters.Count; i++)
            {
                string? param = functionDeclaration.Parameters[i];

                symbolTable.Add(param, new SymbolEntry() { Type = funType.Parameters[i] });
            }

            return new Declaration.FunctionDeclaration(functionDeclaration.Identifier, functionDeclaration.Parameters, TypeCheckBlock(functionDeclaration.Body, symbolTable, typeTable), funType, functionDeclaration.StorageClass);
        }

        return functionDeclaration;
    }

    private Declaration.StructDeclaration TypeCheckStructDeclaration(Declaration.StructDeclaration structDecl, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        if (structDecl.Members.Count == 0)
            return structDecl;
        ValidateStructDefinition(structDecl, symbolTable, typeTable);

        List<MemberEntry> memberEntries = [];
        long structSize = 0;
        long structAlignment = 1;
        foreach (var member in structDecl.Members)
        {
            var memberAlignment = GetTypeAlignment(member.MemberType, typeTable);
            var memberOffset = AssemblyGenerator.AlignTo(structSize, memberAlignment);
            var entry = new MemberEntry() { MemberName = member.MemberName, MemberType = member.MemberType, Offset = memberOffset };
            memberEntries.Add(entry);
            structAlignment = Math.Max(structAlignment, memberAlignment);
            structSize = memberOffset + GetTypeSize(member.MemberType, typeTable);
        }

        structSize = AssemblyGenerator.AlignTo(structSize, structAlignment);
        typeTable[structDecl.Identifier] = new StructEntry() { Alignment = structAlignment, Size = structSize, Members = memberEntries };
        return structDecl;
    }

    private long GetTypeAlignment(Type type, Dictionary<string, StructEntry> typeTable)
    {
        return type switch
        {
            Type.Structure structure => typeTable[structure.Identifier].Alignment,
            Type.Array array => GetTypeAlignment(array.Element, typeTable),
            Type.Int or Type.UInt => 4,
            Type.Long or Type.ULong or Type.Double or Type.Pointer => 8,
            Type.Char or Type.SChar or Type.UChar => 1,
            _ => throw new NotImplementedException()
        };
    }

    private void ValidateStructDefinition(Declaration.StructDeclaration structDecl, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        if (typeTable.ContainsKey(structDecl.Identifier))
            throw TypeError("Trying to redefine struct");

        if (structDecl.Members.Select(a => a.MemberName).Distinct().Count() != structDecl.Members.Count)
            throw TypeError("Struct members can't share the same identifier");

        structDecl.Members.ForEach(a => ValidateTypeSpecifier(a.MemberType, typeTable));

        if (structDecl.Members.Any(a => !IsComplete(a.MemberType, typeTable)))
            throw TypeError("Struct members can't be incomplete");
    }

    private Block TypeCheckBlock(Block block, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        List<BlockItem> newItems = [];
        for (int i = 0; i < block.BlockItems.Count; i++)
        {
            BlockItem? item = block.BlockItems[i];
            if (item is Declaration.VariableDeclaration declaration)
            {
                newItems.Add(TypeCheckLocalVariableDeclaration(declaration, symbolTable, typeTable));
            }
            else if (item is Declaration.FunctionDeclaration functionDeclaration)
            {
                if (functionDeclaration.StorageClass != Declaration.StorageClasses.Static)
                    newItems.Add(TypeCheckFunctionDeclaration(functionDeclaration, symbolTable, typeTable));
                else
                    throw TypeError("StorageClass static used in block function declaration");
            }
            else if (item is Declaration.StructDeclaration structDecl)
            {
                newItems.Add(TypeCheckStructDeclaration(structDecl, symbolTable, typeTable));
            }
            else if (item is Statement statement)
            {
                newItems.Add(TypeCheckStatement(statement, symbolTable, typeTable));
            }
        }
        return new Block(newItems);
    }

    private Declaration.VariableDeclaration TypeCheckFileScopeVariableDeclaration(Declaration.VariableDeclaration variableDeclaration, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        InitialValue initialValue;
        ValidateTypeSpecifier(variableDeclaration.VariableType, typeTable);
        if (variableDeclaration.VariableType is Type.Void)
            throw TypeError("Variable declaration can't be of type void");
        if (variableDeclaration.Initializer != null)
        {
            if (!IsComplete(variableDeclaration.VariableType, typeTable))
                throw TypeError("Type of variable definition must be complete");
            initialValue = new InitialValue.Initial(CreateStaticInitList(variableDeclaration.VariableType, variableDeclaration.Initializer, symbolTable, typeTable));
        }
        else if (variableDeclaration.Initializer == null)
        {
            if (variableDeclaration.StorageClass == Declaration.StorageClasses.Extern)
                initialValue = new InitialValue.NoInitializer();
            else if (!IsComplete(variableDeclaration.VariableType, typeTable))
                throw TypeError("Type of variable definition must be complete");
            else
                initialValue = new InitialValue.Tentative();
        }
        else
            throw TypeError("Non-Constant initializer");

        var global = variableDeclaration.StorageClass != Declaration.StorageClasses.Static;

        if (symbolTable.TryGetValue(variableDeclaration.Identifier, out SymbolEntry prevEntry))
        {
            var attributes = (IdentifierAttributes.Static)prevEntry.IdentifierAttributes;
            if (prevEntry.Type != variableDeclaration.VariableType)
                throw TypeError("Function redeclared as variable");
            if (variableDeclaration.StorageClass == Declaration.StorageClasses.Extern)
                global = attributes.Global;
            else if (attributes.Global != global)
                throw TypeError("Conflicting variable linkage");

            if (attributes.InitialValue is InitialValue.Initial)
                if (initialValue is InitialValue.Initial)
                    throw TypeError("Conflicting file scope variable definitions");
                else
                    initialValue = attributes.InitialValue;
            else if (initialValue is not InitialValue.Initial && attributes.InitialValue is InitialValue.Tentative)
                initialValue = new InitialValue.Tentative();
        }

        symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Static(initialValue, global) };
        return variableDeclaration;
    }

    private Declaration.VariableDeclaration TypeCheckLocalVariableDeclaration(Declaration.VariableDeclaration variableDeclaration, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        ValidateTypeSpecifier(variableDeclaration.VariableType, typeTable);
        if (variableDeclaration.VariableType is Type.Void)
            throw TypeError("Variable declaration can't be of type void");
        if (variableDeclaration.StorageClass == Declaration.StorageClasses.Extern)
        {
            if (variableDeclaration.Initializer != null)
                throw TypeError("Initializer on local extern variable declaration");
            if (symbolTable.TryGetValue(variableDeclaration.Identifier, out SymbolEntry prevEntry))
            {
                if (prevEntry.Type != variableDeclaration.VariableType)
                    throw TypeError("Function redeclared as variable");
            }
            else
                symbolTable.Add(variableDeclaration.Identifier, new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Static(new InitialValue.NoInitializer(), true) });
        }
        else if (variableDeclaration.StorageClass == Declaration.StorageClasses.Static)
        {
            if (!IsComplete(variableDeclaration.VariableType, typeTable))
                throw TypeError("Type of variable definition must be complete");
            InitialValue initialValue;
            if (variableDeclaration.Initializer != null)
            {
                initialValue = new InitialValue.Initial(CreateStaticInitList(variableDeclaration.VariableType, variableDeclaration.Initializer, symbolTable, typeTable));
            }
            else if (variableDeclaration.Initializer == null)
                initialValue = new InitialValue.Initial([new StaticInit.ZeroInit(GetTypeSize(variableDeclaration.VariableType, typeTable))]);
            else
                throw TypeError("Non-constant initializer on local static variable");
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Static(initialValue, false) };
        }
        else
        {
            if (!IsComplete(variableDeclaration.VariableType, typeTable))
                throw TypeError("Type of variable definition must be complete");
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Local() };
            if (variableDeclaration.Initializer != null)
            {
                return new Declaration.VariableDeclaration(variableDeclaration.Identifier, TypeCheckInitializer(variableDeclaration.VariableType, variableDeclaration.Initializer, symbolTable, typeTable), variableDeclaration.VariableType, variableDeclaration.StorageClass);
            }
        }
        return variableDeclaration;
    }

    private List<StaticInit> CreateStaticInitList(Type targetType, Initializer initializer, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        switch (targetType, initializer)
        {
            case (Type.Structure structure, Initializer.CompoundInitializer init):
                {
                    var structDef = typeTable[structure.Identifier];
                    if (init.Initializers.Count > structDef.Members.Count)
                        throw TypeError("Too many elements in structure initializer");

                    long currentOffset = 0;
                    List<StaticInit> staticInits = [];
                    var i = 0;
                    foreach (var initElem in init.Initializers)
                    {
                        var member = structDef.Members[i];
                        if (member.Offset != currentOffset)
                            staticInits.Add(new StaticInit.ZeroInit(member.Offset - currentOffset));
                        var moreStaticInit = CreateStaticInitList(member.MemberType, initElem, symbolTable, typeTable);
                        staticInits.AddRange(moreStaticInit);
                        currentOffset = member.Offset + GetTypeSize(member.MemberType, typeTable);
                        i++;
                    }
                    if (structDef.Size != currentOffset)
                        staticInits.Add(new StaticInit.ZeroInit(structDef.Size - currentOffset));
                    return staticInits;
                }
            case (Type.Structure structure, Initializer.SingleInitializer init):
                throw TypeError("Cannot initialize static structure with scalar expression");
            case (Type.Array array, Initializer.CompoundInitializer compound):
                {
                    if (compound.Initializers.Count > array.Size)
                        throw TypeError("Wrong number of values in initializer");

                    List<StaticInit> staticInits = [];
                    List<StaticInit> typeCheckedInits = [];
                    foreach (var init in compound.Initializers)
                    {
                        var typecheckedElem = CreateStaticInitList(array.Element, init, symbolTable, typeTable);
                        typeCheckedInits.AddRange(typecheckedElem);
                    }
                    if ((array.Size - compound.Initializers.Count) > 0)
                        typeCheckedInits.Add(new StaticInit.ZeroInit(GetTypeSize(array.Element, typeTable) * (array.Size - compound.Initializers.Count)));
                    staticInits.AddRange(typeCheckedInits);
                    return staticInits;
                }
            case (Type.Array array, Initializer.SingleInitializer init):
                {
                    if (IsCharacterType(array.Element) && init.Expression is Expression.String stringExp)
                    {
                        if (stringExp.StringVal.Length > array.Size)
                            throw TypeError("Too many characters in string literal");

                        List<StaticInit> staticInits = [];
                        staticInits.Add(new StaticInit.StringInit(stringExp.StringVal, stringExp.StringVal.Length < array.Size));
                        var diff = array.Size - stringExp.StringVal.Length;
                        if (diff > 1)
                            staticInits.Add(new StaticInit.ZeroInit(diff - 1));
                        return staticInits;
                    }
                    else
                        throw TypeError("Can't initialize array from scalar value");
                }
            case (Type.Pointer pointer, Initializer.SingleInitializer init):
                if ((pointer.Referenced is Type.Char) && init.Expression is Expression.String strExp)
                {
                    var stringLabel = $".Lstring_init_{counter++}";
                    var type = new Type.Array(new Type.Char(), strExp.StringVal.Length + 1);
                    var attr = new IdentifierAttributes.Constant(new StaticInit.StringInit(strExp.StringVal, true));
                    symbolTable[stringLabel] = new SymbolEntry() { Type = type, IdentifierAttributes = attr };

                    return [new StaticInit.PointerInit(stringLabel)];
                }
                else if (init.Expression is Expression.Constant constantInit && IsZeroInteger(constantInit.Value))
                    return [ConvertConstantToInit(targetType, constantInit.Value, typeTable)];
                else if (pointer.Referenced is not Type.Void || init.Expression is not Expression.String)
                    throw TypeError("Invalid pointer initialization");
                else return [];
            case (_, Initializer.SingleInitializer single):
                if (single.Expression is Expression.Constant constant)
                {
                    // if (IsZeroInteger(constant.Value))
                    //     staticInits.Add(ConvertConstantToInit(constant.Type, new Const.ConstULong(0)));
                    // else
                    return [ConvertConstantToInit(targetType, constant.Value, typeTable)];
                }
                else
                    throw TypeError("Non-Static initializer");
            default:
                throw TypeError("Can't initialize a scalar object with a compound initializer");
        }
    }

    private Initializer TypeCheckInitializer(Type targetType, Initializer initializer, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        switch (targetType, initializer)
        {
            case (Type.Structure structure, Initializer.CompoundInitializer init):
                {
                    var structDef = typeTable[structure.Identifier];
                    if (init.Initializers.Count > structDef.Members.Count)
                        throw TypeError("Too many elements in structure initializer");

                    int i = 0;
                    List<Initializer> typeCheckedInits = [];
                    foreach (var initElem in init.Initializers)
                    {
                        var type = structDef.Members[i].MemberType;
                        var typeCheckedElem = TypeCheckInitializer(type, initElem, symbolTable, typeTable);
                        typeCheckedInits.Add(typeCheckedElem);
                        i++;
                    }
                    while (i < structDef.Members.Count)
                    {
                        var type = structDef.Members[i].MemberType;
                        typeCheckedInits.Add(ZeroInitializer(type, typeTable));
                        i++;
                    }
                    return new Initializer.CompoundInitializer(typeCheckedInits, targetType);
                }
            case (Type.Array array, Initializer.SingleInitializer init):
                {
                    if (init.Expression is Expression.String stringExp)
                    {
                        if (!IsCharacterType(array.Element))
                            throw TypeError("Can't initialize a non-character type with a string literal");
                        if (stringExp.StringVal.Length > array.Size)
                            throw TypeError("Too many characters in string literal");
                        return new Initializer.SingleInitializer(stringExp, array);
                    }
                    else
                        throw TypeError("Cannot initialize array with scalar initializer");
                }
            case (_, Initializer.SingleInitializer single):
                {
                    var typecheckedExp = TypeCheckAndConvertExpression(single.Expression, symbolTable, typeTable);
                    var castExp = ConvertByAssignment(typecheckedExp, targetType);
                    return new Initializer.SingleInitializer(castExp, targetType);
                }
            case (Type.Array array, Initializer.CompoundInitializer compound):
                if (compound.Initializers.Count > array.Size)
                    throw TypeError("Wrong number of values in initializer");

                List<Initializer> typecheckedInits = [];
                foreach (var init in compound.Initializers)
                {
                    var typecheckedElem = TypeCheckInitializer(array.Element, init, symbolTable, typeTable);
                    typecheckedInits.Add(typecheckedElem);
                }
                while (typecheckedInits.Count < array.Size)
                    typecheckedInits.Add(ZeroInitializer(array.Element, typeTable));
                return new Initializer.CompoundInitializer(typecheckedInits, targetType);
            default:
                throw TypeError("Can't initialize a scalar object with a compound initializer");
        }
    }

    private Initializer ZeroInitializer(Type element, Dictionary<string, StructEntry> typeTable)
    {
        switch (element)
        {
            case Type.Int:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstInt(0), element), element);
            case Type.UInt:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstUInt(0), element), element);
            case Type.Long:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstLong(0), element), element);
            case Type.ULong:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstULong(0), element), element);
            case Type.Double or Type.Pointer:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstDouble(0), element), element);
            case Type.Array array:
                List<Initializer> inits = [];
                for (int i = 0; i < array.Size; i++)
                {
                    inits.Add(ZeroInitializer(array.Element, typeTable));
                }
                return new Initializer.CompoundInitializer(inits, element);
            case Type.Char or Type.SChar:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstChar(0), element), element);
            case Type.UChar:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstUChar(0), element), element);
            case Type.Structure structure:
                var structDef = typeTable[structure.Identifier];
                List<Initializer> structInits = [];
                for (int i = 0; i < structDef.Members.Count; i++)
                {
                    structInits.Add(ZeroInitializer(structDef.Members[i].MemberType, typeTable));
                }
                return new Initializer.CompoundInitializer(structInits, element);
            default:
                throw TypeError($"Can't zero initialize with type {element}");
        }
    }

    private Expression TypeCheckExpression(Expression expression, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        switch (expression)
        {
            case Expression.Assignment assignment:
                {
                    var typedLeft = TypeCheckAndConvertExpression(assignment.Left, symbolTable, typeTable);
                    if (!IsLvalue(typedLeft))
                        throw TypeError("Tried to assign to non-lvalue");
                    var typedRight = TypeCheckAndConvertExpression(assignment.Right, symbolTable, typeTable);
                    var leftType = GetType(typedLeft);
                    var convertedRight = ConvertByAssignment(typedRight, leftType);
                    return new Expression.Assignment(typedLeft, convertedRight, leftType);
                }
            case Expression.Variable variable:
                {
                    var varType = symbolTable[variable.Identifier].Type;
                    if (varType is Type.FunctionType)
                        throw TypeError("Function name used as variable");
                    return new Expression.Variable(variable.Identifier, varType);
                }
            case Expression.Unary unary:
                {
                    var unaryInner = TypeCheckAndConvertExpression(unary.Expression, symbolTable, typeTable);
                    if (unary.Operator is Expression.UnaryOperator.Negate && !IsArithmetic(GetType(unaryInner)))
                        throw TypeError("Can only negate arithmetic types");
                    if (unary.Operator is Expression.UnaryOperator.Negate && IsCharacterType(GetType(unaryInner)))
                        unaryInner = ConvertTo(unaryInner, new Type.Int());
                    if (unary.Operator is Expression.UnaryOperator.Complement && !IsArithmetic(GetType(unaryInner)))
                        throw TypeError("Bitwise complement only valid for integer types");
                    if (unary.Operator is Expression.UnaryOperator.Complement && IsCharacterType(GetType(unaryInner)))
                        unaryInner = ConvertTo(unaryInner, new Type.Int());
                    if (unary.Operator == Expression.UnaryOperator.Complement && GetType(unaryInner) is Type.Double)
                        throw TypeError("Can't take the bitwise complement of a double");
                    if (unary.Operator == Expression.UnaryOperator.Not && !IsScalar(GetType(unaryInner)))
                        throw TypeError("Logical operators only apply to scalar expressions");
                    return new Expression.Unary(unary.Operator, unaryInner, unary.Operator switch
                    {
                        Expression.UnaryOperator.Not => new Type.Int(),
                        _ => GetType(unaryInner)
                    });
                }
            case Expression.Binary binary:
                {
                    var typedE1 = TypeCheckAndConvertExpression(binary.Left, symbolTable, typeTable);
                    var typedE2 = TypeCheckAndConvertExpression(binary.Right, symbolTable, typeTable);
                    if (binary.Operator is Expression.BinaryOperator.And or Expression.BinaryOperator.Or)
                    {
                        if (!IsScalar(GetType(typedE1)) || !IsScalar(GetType(typedE2)))
                            throw TypeError("Logical operators only apply to scalar expressions");
                        return new Expression.Binary(binary.Operator, typedE1, typedE2, new Type.Int());
                    }
                    var t1 = GetType(typedE1);
                    var t2 = GetType(typedE2);
                    if (binary.Operator == Expression.BinaryOperator.Add)
                    {
                        if (IsArithmetic(t1) && IsArithmetic(t2))
                        {
                            var commonAdd = GetCommonType(t1, t2, typeTable);
                            var convertedLeft = ConvertTo(typedE1, commonAdd);
                            var convertedRight = ConvertTo(typedE2, commonAdd);
                            return new Expression.Binary(binary.Operator, convertedLeft, convertedRight, commonAdd);
                        }
                        else if (IsPointerToComplete(t1, typeTable) && IsInteger(t2))
                        {
                            var convertedRight = ConvertTo(typedE2, new Type.Long());
                            return new Expression.Binary(binary.Operator, typedE1, convertedRight, t1);
                        }
                        else if (IsPointerToComplete(t2, typeTable) && IsInteger(t1))
                        {
                            var convertedLeft = ConvertTo(typedE1, new Type.Long());
                            return new Expression.Binary(binary.Operator, convertedLeft, typedE2, t2);
                        }
                        else
                            throw TypeError("Invalid operands for addition");
                    }
                    if (binary.Operator == Expression.BinaryOperator.Subtract)
                    {
                        if (IsArithmetic(t1) && IsArithmetic(t2))
                        {
                            var commonSub = GetCommonType(t1, t2, typeTable);
                            var convertedLeft = ConvertTo(typedE1, commonSub);
                            var convertedRight = ConvertTo(typedE2, commonSub);
                            return new Expression.Binary(binary.Operator, convertedLeft, convertedRight, commonSub);
                        }
                        else if (IsPointerToComplete(t1, typeTable) && IsInteger(t2))
                        {
                            var convertedRight = ConvertTo(typedE2, new Type.Long());
                            return new Expression.Binary(binary.Operator, typedE1, convertedRight, t1);
                        }
                        else if (IsPointerToComplete(t2, typeTable) && t1 == t2)
                        {
                            return new Expression.Binary(binary.Operator, typedE1, typedE2, new Type.Long());
                        }
                        else
                            throw TypeError("Invalid operands for subtraction");
                    }
                    if (binary.Operator is Expression.BinaryOperator.LessThan or Expression.BinaryOperator.LessOrEqual or
                        Expression.BinaryOperator.GreaterThan or Expression.BinaryOperator.GreaterOrEqual)
                    {
                        var commonComp = (IsArithmetic(t1) && IsArithmetic(t2))
                            ? GetCommonType(t1, t2, typeTable)
                            : (t1 is Type.Pointer && t1 == t2) ?
                            t1 : throw TypeError("Invalid types for comparison");
                        var convertedLeft = ConvertTo(typedE1, commonComp);
                        var convertedRight = ConvertTo(typedE2, commonComp);
                        return new Expression.Binary(binary.Operator, convertedLeft, convertedRight, new Type.Int());
                    }
                    if (binary.Operator is Expression.BinaryOperator.Equal or Expression.BinaryOperator.NotEqual)
                    {
                        var commonEquality = (t1 is Type.Pointer || t2 is Type.Pointer)
                            ? GetCommonPointerType(typedE1, typedE2)
                            : (IsArithmetic(t1) && IsArithmetic(t2)) ?
                            GetCommonType(t1, t2, typeTable)
                            : throw TypeError("Invalid operands for equality");
                        var convertedLeft = ConvertTo(typedE1, commonEquality);
                        var convertedRight = ConvertTo(typedE2, commonEquality);
                        return new Expression.Binary(binary.Operator, convertedLeft, convertedRight, new Type.Int());
                    }

                    var commonType = (binary.Operator is Expression.BinaryOperator.Equal or Expression.BinaryOperator.NotEqual &&
                        (t1 is Type.Pointer || t2 is Type.Pointer))
                        ? GetCommonPointerType(typedE1, typedE2)
                        : (IsArithmetic(t1) && IsArithmetic(t2)) ?
                        GetCommonType(t1, t2, typeTable)
                        : throw TypeError("Invalid operands for equality");
                    if (binary.Operator == Expression.BinaryOperator.Remainder && commonType is Type.Double)
                        throw TypeError("Can't apply remainder to double");
                    if (binary.Operator is Expression.BinaryOperator.Multiply or Expression.BinaryOperator.Divide or Expression.BinaryOperator.Remainder &&
                        !IsArithmetic(commonType))
                        throw TypeError("Can only multiply arithmetic types");
                    var convertedE1 = ConvertTo(typedE1, commonType);
                    var convertedE2 = ConvertTo(typedE2, commonType);
                    return new Expression.Binary(binary.Operator, convertedE1, convertedE2, commonType);
                }
            case Expression.Constant constant:
                return new Expression.Constant(constant.Value, constant.Value switch
                {
                    Const.ConstInt => new Type.Int(),
                    Const.ConstLong => new Type.Long(),
                    Const.ConstUInt => new Type.UInt(),
                    Const.ConstULong => new Type.ULong(),
                    Const.ConstDouble => new Type.Double(),
                    _ => throw new NotImplementedException()
                });
            case Expression.Conditional conditional:
                var typedCond = TypeCheckAndConvertExpression(conditional.Condition, symbolTable, typeTable);
                var typedThen = TypeCheckAndConvertExpression(conditional.Then, symbolTable, typeTable);
                var typedElse = TypeCheckAndConvertExpression(conditional.Else, symbolTable, typeTable);
                if (!IsScalar(GetType(typedCond)))
                    throw TypeError("Conditional expression only applies to scalar expressions");
                var typeThen = GetType(typedThen);
                var typeElse = GetType(typedElse);




                Type? common;
                if (typeThen is Type.Void && typeElse is Type.Void)
                    common = new Type.Void();
                else if (IsArithmetic(typeThen) && IsArithmetic(typeElse))
                    common = GetCommonType(typeThen, typeElse, typeTable);
                else if (typeThen is Type.Structure s1 && typeElse is Type.Structure s2)
                    if (s1.Identifier != s2.Identifier)
                        throw TypeError("Conditional branches must be the same struct type");
                    else
                        common = s1;
                else if (typeThen is Type.Structure || typeElse is Type.Structure)
                    throw TypeError("Conditional branches must both be a struct type or not");
                else if (typeThen is Type.Pointer || typeElse is Type.Pointer)
                    common = GetCommonPointerType(typedThen, typedElse);
                else
                    throw TypeError("Cannot convert branches of conditional to a common type");
                var convertedThen = ConvertTo(typedThen, common);
                var convertedElse = ConvertTo(typedElse, common);
                return new Expression.Conditional(typedCond, convertedThen, convertedElse, common);
            case Expression.FunctionCall functionCall:
                var funType = symbolTable[functionCall.Identifier].Type;
                if (funType is not Type.FunctionType)
                    throw TypeError("Variable used as function name");

                if (funType is Type.FunctionType functionType && functionType.Parameters.Count != functionCall.Arguments.Count)
                    throw TypeError("Function called with the wrong number of arguments");

                List<Expression> convertedArgs = [];
                for (int i = 0; i < functionCall.Arguments.Count; i++)
                {
                    var typedArg = TypeCheckAndConvertExpression(functionCall.Arguments[i], symbolTable, typeTable);
                    convertedArgs.Add(ConvertByAssignment(typedArg, ((Type.FunctionType)funType).Parameters[i]));
                }
                return new Expression.FunctionCall(functionCall.Identifier, convertedArgs, ((Type.FunctionType)funType).Return);
            case Expression.Cast cast:
                {
                    ValidateTypeSpecifier(cast.TargetType, typeTable);
                    var typedInner = TypeCheckAndConvertExpression(cast.Expression, symbolTable, typeTable);
                    if (cast.TargetType is Type.Double && GetType(typedInner) is Type.Pointer ||
                        cast.TargetType is Type.Pointer && GetType(typedInner) is Type.Double)
                        throw TypeError("Cannot cast between pointer and double");
                    if (GetType(typedInner) is Type.Array)
                        throw TypeError("Cannot cast expression to array");
                    if (cast.TargetType is Type.Void)
                        return new Expression.Cast(cast.TargetType, typedInner, new Type.Void());
                    else if (!IsScalar(cast.TargetType))
                        throw TypeError("Can only cast to scalar type or void");
                    else if (!IsScalar(GetType(typedInner)))
                        throw TypeError("Cannot cast non-scalar expression to scalar type");
                    else
                        return new Expression.Cast(cast.TargetType, typedInner, cast.TargetType);
                }
            case Expression.Dereference dereference:
                {
                    var typedInner = TypeCheckAndConvertExpression(dereference.Expression, symbolTable, typeTable);
                    return GetType(typedInner) switch
                    {
                        Type.Pointer pointer => pointer.Referenced is not Type.Void ?
                            new Expression.Dereference(typedInner, pointer.Referenced) :
                            throw TypeError("Can't dereference pointer to void"),
                        _ => throw TypeError("Cannot dereference non-pointer"),
                    };
                }
            case Expression.AddressOf addressOf:
                {
                    if (IsLvalue(addressOf.Expression))
                    {
                        var typedInner = TypeCheckExpression(addressOf.Expression, symbolTable, typeTable);
                        var referencedType = GetType(typedInner);
                        return new Expression.AddressOf(typedInner, new Type.Pointer(referencedType));
                    }
                    else
                        throw TypeError("Can't take the address of a non-lvalue");
                }
            case Expression.Subscript subscript:
                {
                    var typedE1 = TypeCheckAndConvertExpression(subscript.Left, symbolTable, typeTable);
                    var typedE2 = TypeCheckAndConvertExpression(subscript.Right, symbolTable, typeTable);
                    var t1 = GetType(typedE1);
                    var t2 = GetType(typedE2);
                    Type? pointerType;
                    if (IsPointerToComplete(t1, typeTable) && IsInteger(t2))
                    {
                        pointerType = t1;
                        typedE2 = ConvertTo(typedE2, new Type.Long());
                    }
                    else if (IsInteger(t1) && IsPointerToComplete(t2, typeTable))
                    {
                        pointerType = t2;
                        typedE1 = ConvertTo(typedE1, new Type.Long());
                    }
                    else
                        throw TypeError("Subscript must have integer and pointer operands");
                    return new Expression.Subscript(typedE1, typedE2, ((Type.Pointer)pointerType).Referenced);
                }
            case Expression.String stringExp:
                {
                    return new Expression.String(stringExp.StringVal, new Type.Array(new Type.Char(), stringExp.StringVal.Length + 1));
                }
            case Expression.SizeOf sizeofExp:
                {
                    var typedInner = TypeCheckExpression(sizeofExp.Expression, symbolTable, typeTable);
                    if (!IsComplete(GetType(typedInner), typeTable))
                        throw TypeError("Can't get the size of an incomplete type");
                    return new Expression.SizeOf(typedInner, new Type.ULong());
                }
            case Expression.SizeOfType sizeofType:
                {
                    ValidateTypeSpecifier(sizeofType.TargetType, typeTable);
                    if (!IsComplete(sizeofType.TargetType, typeTable))
                        throw TypeError("Can't get the size of an incomplete type");
                    return new Expression.SizeOfType(sizeofType.TargetType, new Type.ULong());
                }
            case Expression.Dot dot:
                {
                    var typedStructure = TypeCheckAndConvertExpression(dot.Structure, symbolTable, typeTable);
                    switch (GetType(typedStructure))
                    {
                        case Type.Structure structure:
                            var structDef = typeTable[structure.Identifier];
                            foreach (var memberDef in structDef.Members)
                            {
                                if (memberDef.MemberName == dot.Member)
                                    return new Expression.Dot(typedStructure, dot.Member, memberDef.MemberType);
                            }
                            throw TypeError("Could not find struct member");
                        default:
                            throw TypeError("Tried to get member of non-structure");
                    }
                }
            case Expression.Arrow arrow:
                {
                    var typedStructure = TypeCheckAndConvertExpression(arrow.Pointer, symbolTable, typeTable);
                    if (GetType(typedStructure) is Type.Pointer pointer)
                        switch (pointer.Referenced)
                        {
                            case Type.Structure structure:
                                var structDef = typeTable[structure.Identifier];
                                foreach (var memberDef in structDef.Members)
                                {
                                    if (memberDef.MemberName == arrow.Member)
                                        return new Expression.Arrow(typedStructure, arrow.Member, memberDef.MemberType);
                                }
                                throw TypeError("Could not find struct member");
                            default:
                                throw TypeError("Tried to get member of non-structure");
                        }
                    else
                        throw TypeError("Tried to get member of non-pointer to structure");
                }
            default:
                throw new NotImplementedException();
        }
    }

    private Expression TypeCheckAndConvertExpression(Expression expression, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        var typedExp = TypeCheckExpression(expression, symbolTable, typeTable);
        return GetType(typedExp) switch
        {
            Type.Array array => new Expression.AddressOf(typedExp, new Type.Pointer(array.Element)),
            Type.Structure structure => typeTable.ContainsKey(structure.Identifier) ? typedExp :
                throw TypeError("Invalid use of incomplete structure type"),
            _ => typedExp,
        };
    }

    private Expression ConvertByAssignment(Expression exp, Type targetType)
    {
        if (GetType(exp) == targetType)
            return exp;
        else if (IsArithmetic(GetType(exp)) && IsArithmetic(targetType))
            return ConvertTo(exp, targetType);
        else if (IsNullPointerConstant(exp) && targetType is Type.Pointer)
            return ConvertTo(exp, targetType);
        else if (targetType is Type.Pointer pointer && pointer.Referenced is Type.Void && GetType(exp) is Type.Pointer)
            return ConvertTo(exp, targetType);
        else if (GetType(exp) is Type.Pointer pointer2 && pointer2.Referenced is Type.Void && targetType is Type.Pointer)
            return ConvertTo(exp, targetType);
        else
            throw TypeError("Cannot convert type for assignment");
    }

    private bool IsInteger(Type type)
    {
        return type switch
        {
            Type.Int or Type.Long or Type.UInt or Type.ULong or Type.Char or Type.SChar or Type.UChar => true,
            _ => false
        };
    }

    private bool IsArithmetic(Type type)
    {
        return type switch
        {
            Type.Int or Type.Long or Type.UInt or Type.ULong or Type.Double or Type.Char or Type.SChar or Type.UChar => true,
            _ => false
        };
    }

    private bool IsLvalue(Expression exp)
    {
        return exp is Expression.Variable or Expression.Dereference or Expression.Subscript or Expression.String or Expression.Arrow ||
            (exp is Expression.Dot dot && IsLvalue(dot.Structure));
    }

    private bool IsZeroInteger(Const constant)
    {
        return constant switch
        {
            Const.ConstInt c => c.Value == 0,
            Const.ConstUInt c => c.Value == 0,
            Const.ConstLong c => c.Value == 0,
            Const.ConstULong c => c.Value == 0,
            _ => false
        };
    }

    private bool IsNullPointerConstant(Expression exp)
    {
        return exp switch
        {
            Expression.Constant constant => IsZeroInteger(constant.Value),
            _ => false
        };
    }

    private Type GetCommonPointerType(Expression exp1, Expression exp2)
    {
        var type1 = GetType(exp1);
        var type2 = GetType(exp2);
        if (type1 == type2)
            return type1;
        else if (IsNullPointerConstant(exp1))
            return type2;
        else if (IsNullPointerConstant(exp2))
            return type1;
        else if (type1 is Type.Pointer pointer && pointer.Referenced is Type.Void && type2 is Type.Pointer)
            return new Type.Pointer(new Type.Void());
        else if (type2 is Type.Pointer pointer2 && pointer2.Referenced is Type.Void && type1 is Type.Pointer)
            return new Type.Pointer(new Type.Void());
        else
            throw TypeError("Expressions have incompatible types");
    }

    private Statement TypeCheckStatement(Statement statement, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        switch (statement)
        {
            case Statement.ReturnStatement ret:
                if (ret.Expression == null && functionReturnType is Type.Void)
                {
                    return new Statement.ReturnStatement(null);
                }
                else if (ret.Expression != null && functionReturnType is not Type.Void)
                {
                    var typedReturn = TypeCheckAndConvertExpression(ret.Expression, symbolTable, typeTable);
                    var converted = ConvertByAssignment(typedReturn, functionReturnType);
                    return new Statement.ReturnStatement(converted);
                }
                else
                    throw TypeError("Incompatible return type");
            case Statement.ExpressionStatement expressionStatement:
                return new Statement.ExpressionStatement(TypeCheckAndConvertExpression(expressionStatement.Expression, symbolTable, typeTable));
            case Statement.NullStatement nullStatement:
                return nullStatement;
            case Statement.IfStatement ifStatement:
                var ifCond = TypeCheckAndConvertExpression(ifStatement.Condition, symbolTable, typeTable);
                if (!IsScalar(GetType(ifCond)))
                    throw TypeError("Conditional expression only applies to scalar expressions");
                var ifThen = TypeCheckStatement(ifStatement.Then, symbolTable, typeTable);
                var ifElse = ifStatement.Else;
                if (ifElse != null)
                    ifElse = TypeCheckStatement(ifElse, symbolTable, typeTable);
                return new Statement.IfStatement(ifCond, ifThen, ifElse);
            case Statement.CompoundStatement compoundStatement:
                return new Statement.CompoundStatement(TypeCheckBlock(compoundStatement.Block, symbolTable, typeTable));
            case Statement.BreakStatement breakStatement:
                return breakStatement;
            case Statement.ContinueStatement continueStatement:
                return continueStatement;
            case Statement.WhileStatement whileStatement:
                var whileCond = TypeCheckAndConvertExpression(whileStatement.Condition, symbolTable, typeTable);
                if (!IsScalar(GetType(whileCond)))
                    throw TypeError("Conditional expression only applies to scalar expressions");
                var whileBody = TypeCheckStatement(whileStatement.Body, symbolTable, typeTable);
                return new Statement.WhileStatement(whileCond, whileBody, whileStatement.Label);
            case Statement.DoWhileStatement doWhileStatement:
                var doBody = TypeCheckStatement(doWhileStatement.Body, symbolTable, typeTable);
                var doCond = TypeCheckAndConvertExpression(doWhileStatement.Condition, symbolTable, typeTable);
                if (!IsScalar(GetType(doCond)))
                    throw TypeError("Conditional expression only applies to scalar expressions");
                return new Statement.DoWhileStatement(doBody, doCond, doWhileStatement.Label);
            case Statement.ForStatement forStatement:
                {
                    var forInit = TypeCheckForInit(forStatement.Init, symbolTable, typeTable);
                    var cond = TypeCheckOptionalExpression(forStatement.Condition, symbolTable, typeTable);
                    if (cond != null && !IsScalar(GetType(cond)))
                        throw TypeError("Conditional expression only applies to scalar expressions");
                    var post = TypeCheckOptionalExpression(forStatement.Post, symbolTable, typeTable);
                    var body = TypeCheckStatement(forStatement.Body, symbolTable, typeTable);
                    return new Statement.ForStatement(forInit, cond, post, body, forStatement.Label);
                }
            default:
                throw new NotImplementedException();
        }
    }

    private ForInit TypeCheckForInit(ForInit init, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        switch (init)
        {
            case ForInit.InitExpression initExpression:
                return new ForInit.InitExpression(TypeCheckOptionalExpression(initExpression.Expression, symbolTable, typeTable));
            case ForInit.InitDeclaration initDeclaration:
                if (initDeclaration.Declaration.StorageClass == null)
                    return new ForInit.InitDeclaration(TypeCheckLocalVariableDeclaration(initDeclaration.Declaration, symbolTable, typeTable));
                else
                    throw TypeError("StorageClass used in for loop init");
            default:
                throw new NotImplementedException();
        }
    }

    private Expression? TypeCheckOptionalExpression(Expression? expression, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        if (expression != null)
            return TypeCheckAndConvertExpression(expression, symbolTable, typeTable);
        else
            return null;
    }

    private StaticInit ConvertConstantToInit(Type target, Const constant, Dictionary<string, StructEntry> typeTable)
    {
        if (constant is Const.ConstDouble cd && target is Type.Double)
            return new StaticInit.DoubleInit(cd.Value);

        ulong value = constant switch
        {
            Const.ConstInt constInt => (ulong)constInt.Value,
            Const.ConstLong constLong => (ulong)constLong.Value,
            Const.ConstUInt constUInt => (ulong)constUInt.Value,
            Const.ConstULong constULong => (ulong)constULong.Value,
            Const.ConstDouble constDouble => (ulong)constDouble.Value,
            Const.ConstChar constChar => (ulong)constChar.Value,
            Const.ConstUChar constUChar => (ulong)constUChar.Value,
            _ => throw new NotImplementedException()
        };

        if (target is Type.Pointer && value != 0)
            throw TypeError("Invalid static initializer for pointer");

        return target switch
        {
            Type.Int => new StaticInit.IntInit((int)value),
            Type.Long => new StaticInit.LongInit((long)value),
            Type.UInt => new StaticInit.UIntInit((uint)value),
            Type.ULong => new StaticInit.ULongInit((ulong)value),
            Type.Double => new StaticInit.DoubleInit((double)value),
            Type.Pointer => new StaticInit.ULongInit(value),
            Type.Array array => new StaticInit.ZeroInit(array.Size * GetTypeSize(array.Element, typeTable)),
            Type.Char or Type.SChar => new StaticInit.CharInit((char)value),
            Type.UChar => new StaticInit.UCharInit((byte)value),
            _ => throw new NotImplementedException()
        };
    }

    private Expression ConvertTo(Expression expression, Type type)
    {
        if (GetType(expression) == type)
            return expression;

        return new Expression.Cast(type, expression, type);
    }

    private Type GetCommonType(Type type1, Type type2, Dictionary<string, StructEntry> typeTable)
    {
        if (IsCharacterType(type1))
            type1 = new Type.Int();
        if (IsCharacterType(type2))
            type2 = new Type.Int();
        if (type1 == type2)
            return type1;
        if (type1 is Type.Double || type2 is Type.Double)
            return new Type.Double();
        if (GetTypeSize(type1, typeTable) == GetTypeSize(type2, typeTable))
        {
            if (IsSignedType(type1))
                return type2;
            else
                return type1;
        }
        if (GetTypeSize(type1, typeTable) > GetTypeSize(type2, typeTable))
            return type1;
        else
            return type2;
    }

    private void ValidateTypeSpecifier(Type type, Dictionary<string, StructEntry> typeTable)
    {
        switch (type)
        {
            case Type.Array array:
                if (!IsComplete(array.Element, typeTable))
                    throw TypeError("Illegal array of incomplete type");
                ValidateTypeSpecifier(array.Element, typeTable);
                break;
            case Type.Pointer pointer:
                ValidateTypeSpecifier(pointer.Referenced, typeTable);
                break;
            case Type.FunctionType funcType:
                foreach (var param in funcType.Parameters)
                    ValidateTypeSpecifier(param, typeTable);
                ValidateTypeSpecifier(funcType.Return, typeTable);
                break;
            default:
                break;
        }
    }

    private bool IsCharacterType(Type type)
    {
        return type is Type.Char or Type.UChar or Type.SChar;
    }

    public static bool IsScalar(Type type)
    {
        return type switch
        {
            Type.Void => false,
            Type.Array => false,
            Type.FunctionType => false,
            Type.Structure => false,
            _ => true,
        };
    }

    public static bool IsComplete(Type type, Dictionary<string, StructEntry> typeTable)
    {
        return type switch
        {
            Type.Void => false,
            Type.Structure structure => typeTable.ContainsKey(structure.Identifier),
            _ => true,
        };
    }

    private bool IsPointerToComplete(Type type, Dictionary<string, StructEntry> typeTable)
    {
        return type switch
        {
            Type.Pointer pointer => IsComplete(pointer.Referenced, typeTable),
            _ => false,
        };
    }

    public static long GetTypeSize(Type type, Dictionary<string, StructEntry> typeTable)
    {
        return type switch
        {
            Type.Char or Type.SChar or Type.UChar => 1,
            Type.Int or Type.UInt => 4,
            Type.Long or Type.ULong or Type.Pointer or Type.Double => 8,
            Type.Array array => GetTypeSize(array.Element, typeTable) * array.Size,
            Type.Structure structure => typeTable[structure.Identifier].Size,
            _ => throw new NotImplementedException()
        };
    }

    public static bool IsSignedType(Type type)
    {
        return type switch
        {
            Type.Int or Type.Long or Type.Char or Type.SChar => true,
            Type.UInt or Type.ULong or Type.UChar or Type.Pointer => false,
            _ => throw new NotImplementedException()
        };
    }

    public static Type GetType(Expression expression)
    {
        return expression switch
        {
            Expression.Constant exp => exp.Type,
            Expression.Variable exp => exp.Type,
            Expression.Unary exp => exp.Type,
            Expression.Binary exp => exp.Type,
            Expression.Assignment exp => exp.Type,
            Expression.Conditional exp => exp.Type,
            Expression.FunctionCall exp => exp.Type,
            Expression.Cast exp => exp.Type,
            Expression.Dereference exp => exp.Type,
            Expression.AddressOf exp => exp.Type,
            Expression.Subscript exp => exp.Type,
            Expression.String exp => exp.Type,
            Expression.SizeOf exp => exp.Type,
            Expression.SizeOfType exp => exp.Type,
            Expression.Dot exp => exp.Type,
            Expression.Arrow exp => exp.Type,
            _ => throw new NotImplementedException()
        };
    }

    private Exception TypeError(string message)
    {
        return new Exception("Type Error: " + message);
    }
}