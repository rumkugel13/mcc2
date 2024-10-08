namespace mcc2;

using System.Text;
using mcc2.AST;
using mcc2.TAC;

public class TackyEmitter
{
    private uint counter;

    private Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable;
    private Dictionary<string, SemanticAnalyzer.StructEntry> typeTable;

    public TackyEmitter(Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable, Dictionary<string, SemanticAnalyzer.StructEntry> typeTable)
    {
        this.symbolTable = symbolTable;
        this.typeTable = typeTable;
    }

    public TACProgam Emit(ASTProgram astProgram)
    {
        var program = EmitProgram(astProgram);
        program.Definitions.AddRange(ConvertSymbolsToTacky());
        return program;
    }

    private List<TopLevel> ConvertSymbolsToTacky()
    {
        List<TopLevel> instructions = [];
        foreach (var entry in symbolTable)
        {
            switch (entry.Value.IdentifierAttributes)
            {
                case IdentifierAttributes.Static stat:
                    switch (stat.InitialValue)
                    {
                        case InitialValue.Initial init:
                            instructions.Add(new TopLevel.StaticVariable(entry.Key, stat.Global, entry.Value.Type, init.Inits));
                            break;
                        case InitialValue.Tentative:
                            instructions.Add(new TopLevel.StaticVariable(entry.Key, stat.Global, entry.Value.Type, [new StaticInit.ZeroInit(TypeChecker.GetTypeSize(entry.Value.Type, typeTable))]));
                            break;
                        case InitialValue.NoInitializer:
                            break;
                    }
                    break;
                case IdentifierAttributes.Constant constant:
                    switch (constant.Init)
                    {
                        case StaticInit.StringInit stringInit:
                            instructions.Add(new TopLevel.StaticConstant(entry.Key, entry.Value.Type, stringInit));
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
        return instructions;
    }

    private TACProgam EmitProgram(ASTProgram astProgram)
    {
        List<TopLevel> definitions = [];
        foreach (var decl in astProgram.Declarations)
            if (decl is Declaration.FunctionDeclaration fun && fun.Body != null)
                definitions.Add(EmitFunction(fun));
        return new TACProgam(definitions);
    }

    private TopLevel.Function EmitFunction(Declaration.FunctionDeclaration functionDefinition)
    {
        List<Instruction> instructions = [];
        if (functionDefinition.Body != null)
        {
            foreach (var item in functionDefinition.Body.BlockItems)
                EmitInstruction(item, instructions);
            instructions.Add(new Instruction.Return(new Val.Constant(new Const.ConstInt(0))));
        }
        return new TopLevel.Function(functionDefinition.Identifier,
            ((IdentifierAttributes.Function)symbolTable[functionDefinition.Identifier].IdentifierAttributes).Global,
            functionDefinition.Parameters, instructions);
    }

    private void EmitVarDeclaration(Declaration.VariableDeclaration variableDeclaration, List<Instruction> instructions)
    {
        switch (variableDeclaration.Initializer)
        {
            case Initializer.SingleInitializer single:
                if (single.Expression is Expression.String stringExp)
                {
                    EmitCompoundInit(single, 0, variableDeclaration.Identifier, instructions);
                    break;
                }
                var result = EmitTackyAndConvert(single.Expression, instructions);
                instructions.Add(new Instruction.Copy(ToVal(result), new Val.Variable(variableDeclaration.Identifier)));
                break;
            case Initializer.CompoundInitializer compound:
                EmitCompoundInit(compound, 0, variableDeclaration.Identifier, instructions);
                break;
        }
    }

    private void EmitCompoundInit(Initializer initializer, long offset, string name, List<Instruction> instructions)
    {
        switch (initializer)
        {
            case Initializer.SingleInitializer single:
                if (single.Expression is Expression.String stringExp)
                {
                    var stringBytes = new List<byte>(Encoding.ASCII.GetBytes(stringExp.StringVal));
                    var paddingBytes = new List<byte>(new byte[TypeChecker.GetTypeSize(single.Type, typeTable) - stringExp.StringVal.Length]);
                    stringBytes.AddRange(paddingBytes);
                    EmitStringInit(name, offset, stringBytes, instructions);
                }
                else
                {
                    var result = EmitTackyAndConvert(single.Expression, instructions);
                    instructions.Add(new Instruction.CopyToOffset(ToVal(result), name, offset));
                }
                break;
            case Initializer.CompoundInitializer compound:
                if (compound.Type is Type.Structure structure)
                {
                    var members = typeTable[structure.Identifier].Members;
                    for (int i = 0; i < members.Count; i++)
                    {
                        var memInit = compound.Initializers[i];
                        var member = members[i];
                        var memOffset = offset + member.Offset;
                        EmitCompoundInit(memInit, memOffset, name, instructions);
                    }
                }
                else
                    for (int i = 0; i < compound.Initializers.Count; i++)
                    {
                        Initializer? init = compound.Initializers[i];
                        var newOffset = offset + (i * TypeChecker.GetTypeSize(((Type.Array)compound.Type).Element, typeTable));
                        EmitCompoundInit(init, newOffset, name, instructions);
                    }
                break;
        }
    }

    private void EmitStringInit(string dst, long offset, List<byte> bytes, List<Instruction> instructions)
    {
        if (bytes.Count >= 8)
        {
            var value = BitConverter.ToInt64(bytes.ToArray(), 0);
            instructions.Add(new Instruction.CopyToOffset(new Val.Constant(new Const.ConstLong(value)), dst, offset));
            var rest = bytes[8..];
            EmitStringInit(dst, offset + 8, rest, instructions);
        }
        else if (bytes.Count >= 4)
        {
            var value = BitConverter.ToInt32(bytes.ToArray(), 0);
            instructions.Add(new Instruction.CopyToOffset(new Val.Constant(new Const.ConstInt(value)), dst, offset));
            var rest = bytes[4..];
            EmitStringInit(dst, offset + 4, rest, instructions);
        }
        else if (bytes.Count >= 1)
        {
            var value = bytes[0];
            instructions.Add(new Instruction.CopyToOffset(new Val.Constant(new Const.ConstChar(value)), dst, offset));
            var rest = bytes[1..];
            EmitStringInit(dst, offset + 1, rest, instructions);
        }
    }

    private void EmitInstruction(BlockItem blockItem, List<Instruction> instructions)
    {
        switch (blockItem)
        {
            case Statement.ReturnStatement returnStatement:
                {
                    if (returnStatement.Expression != null)
                    {
                        var val = EmitTackyAndConvert(returnStatement.Expression, instructions);
                        instructions.Add(new Instruction.Return(ToVal(val)));
                    }
                    else
                        instructions.Add(new Instruction.Return(null));
                    break;
                }
            case Declaration.VariableDeclaration declaration:
                if (declaration.Initializer != null && declaration.StorageClass == null)
                {
                    EmitVarDeclaration(declaration, instructions);
                }
                break;
            case Statement.ExpressionStatement expressionStatement:
                EmitTackyAndConvert(expressionStatement.Expression, instructions);
                break;
            case Statement.NullStatement:
                break;
            case Statement.IfStatement ifStatement:
                {
                    var cond = EmitTackyAndConvert(ifStatement.Condition, instructions);
                    var endLabel = MakeLabel();
                    var elseLabel = MakeLabel();
                    if (ifStatement.Else == null)
                        instructions.Add(new Instruction.JumpIfZero(ToVal(cond), endLabel));
                    else
                        instructions.Add(new Instruction.JumpIfZero(ToVal(cond), elseLabel));
                    EmitInstruction(ifStatement.Then, instructions);
                    if (ifStatement.Else != null)
                    {
                        instructions.Add(new Instruction.Jump(endLabel));
                        instructions.Add(new Instruction.Label(elseLabel));
                        EmitInstruction(ifStatement.Else, instructions);
                    }
                    instructions.Add(new Instruction.Label(endLabel));
                    break;
                }
            case Statement.CompoundStatement compoundStatement:
                foreach (var item in compoundStatement.Block.BlockItems)
                    EmitInstruction(item, instructions);
                break;
            case Statement.BreakStatement breakStatement:
                instructions.Add(new Instruction.Jump(MakeBreakLabel(breakStatement.Label)));
                break;
            case Statement.ContinueStatement continueStatement:
                instructions.Add(new Instruction.Jump(MakeContinueLabel(continueStatement.Label)));
                break;
            case Statement.DoWhileStatement doWhileStatement:
                {
                    var start = MakeStartLabel(doWhileStatement.Label);
                    instructions.Add(new Instruction.Label(start));
                    EmitInstruction(doWhileStatement.Body, instructions);
                    instructions.Add(new Instruction.Label(MakeContinueLabel(doWhileStatement.Label)));
                    var val = EmitTackyAndConvert(doWhileStatement.Condition, instructions);
                    instructions.Add(new Instruction.JumpIfNotZero(ToVal(val), start));
                    instructions.Add(new Instruction.Label(MakeBreakLabel(doWhileStatement.Label)));
                    break;
                }
            case Statement.WhileStatement whileStatement:
                {
                    var continueLabel = MakeContinueLabel(whileStatement.Label);
                    instructions.Add(new Instruction.Label(continueLabel));
                    var val = EmitTackyAndConvert(whileStatement.Condition, instructions);
                    var breakLabel = MakeBreakLabel(whileStatement.Label);
                    instructions.Add(new Instruction.JumpIfZero(ToVal(val), breakLabel));
                    EmitInstruction(whileStatement.Body, instructions);
                    instructions.Add(new Instruction.Jump(continueLabel));
                    instructions.Add(new Instruction.Label(breakLabel));
                    break;
                }
            case Statement.ForStatement forStatement:
                {
                    if (forStatement.Init is ForInit.InitDeclaration initDeclaration)
                        EmitInstruction(initDeclaration.Declaration, instructions);
                    else if (forStatement.Init is ForInit.InitExpression initExpression && initExpression.Expression != null)
                        EmitTackyAndConvert(initExpression.Expression, instructions);
                    var start = MakeStartLabel(forStatement.Label);
                    instructions.Add(new Instruction.Label(start));
                    var breakLabel = MakeBreakLabel(forStatement.Label);
                    if (forStatement.Condition != null)
                    {
                        var val = EmitTackyAndConvert(forStatement.Condition, instructions);
                        instructions.Add(new Instruction.JumpIfZero(ToVal(val), breakLabel));
                    }
                    EmitInstruction(forStatement.Body, instructions);
                    var continueLabel = MakeContinueLabel(forStatement.Label);
                    instructions.Add(new Instruction.Label(continueLabel));
                    if (forStatement.Post != null)
                        EmitTackyAndConvert(forStatement.Post, instructions);
                    instructions.Add(new Instruction.Jump(start));
                    instructions.Add(new Instruction.Label(breakLabel));
                    break;
                }
            case Declaration.FunctionDeclaration functionDeclaration:
                EmitFunction(functionDeclaration);
                break;
            case Declaration.StructDeclaration structDecl:
                break;
            case Statement.GotoStatement go:
                instructions.Add(new Instruction.Jump(go.Label));
                break;
            case Statement.LabelStatement label:
                instructions.Add(new Instruction.Label(label.Label));
                EmitInstruction(label.Inner, instructions);
                break;
            case Statement.SwitchStatement sw:
                {
                    var val = EmitTackyAndConvert(sw.Expression, instructions);
                    foreach (var st in sw.Cases)
                    {
                        switch (st)
                        {
                            case Statement.CaseStatement ca:
                                {
                                    var constant = EmitTackyAndConvert(ca.Expression, instructions);
                                    var temp = MakeTackyVariable(GetType(sw.Expression));
                                    instructions.Add(new Instruction.Binary(Expression.BinaryOperator.Equal, ToVal(val), ToVal(constant), temp));
                                    instructions.Add(new Instruction.JumpIfNotZero(temp, MakeCaseLabel(ca.Label)));
                                    break;
                                }
                        }
                    }
                    var def = sw.Cases.Where(a => a is Statement.DefaultStatement).ToList();
                    if (def.Count > 0)
                    {
                        var defStmt = def[0] as Statement.DefaultStatement;
                        instructions.Add(new Instruction.Jump(MakeCaseLabel(defStmt!.Label)));
                    }

                    instructions.Add(new Instruction.Jump(MakeBreakLabel(sw.Label)));
                    EmitInstruction(sw.Inner, instructions);
                    instructions.Add(new Instruction.Label(MakeBreakLabel(sw.Label)));
                    break;
                }
            case Statement.CaseStatement ca:
                instructions.Add(new Instruction.Label(MakeCaseLabel(ca.Label)));
                EmitInstruction(ca.Inner, instructions);
                break;
            case Statement.DefaultStatement de:
                instructions.Add(new Instruction.Label(MakeCaseLabel(de.Label)));
                EmitInstruction(de.Inner, instructions);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private ExpResult EmitTacky(Expression expression, List<Instruction> instructions)
    {
        switch (expression)
        {
            case Expression.Constant constant:
                return new ExpResult.PlainOperand(new Val.Constant(constant.Value));
            case Expression.Unary unary:
                {
                    var src = EmitTackyAndConvert(unary.Expression, instructions);
                    var dst = MakeTackyVariable(unary.Type);
                    instructions.Add(new Instruction.Unary(unary.Operator, ToVal(src), dst));
                    return new ExpResult.PlainOperand(dst);
                }
            case Expression.Binary binary:
                {
                    if (binary.Operator == Expression.BinaryOperator.And ||
                        binary.Operator == Expression.BinaryOperator.Or)
                    {
                        var dstAndOr = MakeTackyVariable(binary.Type);

                        if (binary.Operator == Expression.BinaryOperator.And)
                        {
                            var v1 = EmitTackyAndConvert(binary.Left, instructions);
                            var falseLabel = MakeLabel();
                            instructions.Add(new Instruction.JumpIfZero(ToVal(v1), falseLabel));
                            var v2 = EmitTackyAndConvert(binary.Right, instructions);
                            instructions.Add(new Instruction.JumpIfZero(ToVal(v2), falseLabel));
                            instructions.Add(new Instruction.Copy(new Val.Constant(new Const.ConstInt(1)), dstAndOr));
                            var endLabel = MakeLabel();
                            instructions.Add(new Instruction.Jump(endLabel));
                            instructions.Add(new Instruction.Label(falseLabel));
                            instructions.Add(new Instruction.Copy(new Val.Constant(new Const.ConstInt(0)), dstAndOr));
                            instructions.Add(new Instruction.Label(endLabel));
                        }
                        else
                        {
                            var v1 = EmitTackyAndConvert(binary.Left, instructions);
                            var trueLabel = MakeLabel();
                            instructions.Add(new Instruction.JumpIfNotZero(ToVal(v1), trueLabel));
                            var v2 = EmitTackyAndConvert(binary.Right, instructions);
                            instructions.Add(new Instruction.JumpIfNotZero(ToVal(v2), trueLabel));
                            instructions.Add(new Instruction.Copy(new Val.Constant(new Const.ConstInt(0)), dstAndOr));
                            var endLabel = MakeLabel();
                            instructions.Add(new Instruction.Jump(endLabel));
                            instructions.Add(new Instruction.Label(trueLabel));
                            instructions.Add(new Instruction.Copy(new Val.Constant(new Const.ConstInt(1)), dstAndOr));
                            instructions.Add(new Instruction.Label(endLabel));
                        }
                        return new ExpResult.PlainOperand(dstAndOr);
                    }
                    else if (binary.Operator == Expression.BinaryOperator.Add && (GetType(binary.Left) is Type.Pointer || GetType(binary.Right) is Type.Pointer))
                    {
                        var pointer = GetType(binary.Left) is Type.Pointer ? binary.Left : binary.Right;
                        var integer = GetType(binary.Left) is Type.Pointer ? binary.Right : binary.Left;
                        var pointerVal = EmitTackyAndConvert(pointer, instructions);
                        var integerVal = EmitTackyAndConvert(integer, instructions);
                        var dst = MakeTackyVariable(binary.Type);
                        instructions.Add(new Instruction.AddPointer(ToVal(pointerVal), ToVal(integerVal), TypeChecker.GetTypeSize(((Type.Pointer)GetType(pointer)).Referenced, typeTable), dst));
                        return new ExpResult.PlainOperand(dst);
                    }
                    else if (binary.Operator == Expression.BinaryOperator.Subtract && (GetType(binary.Left) is Type.Pointer || GetType(binary.Right) is Type.Pointer))
                    {
                        if (GetType(binary.Left) is Type.Pointer && GetType(binary.Right) is Type.Pointer)
                        {
                            var p1 = EmitTackyAndConvert(binary.Left, instructions);
                            var p2 = EmitTackyAndConvert(binary.Right, instructions);
                            var diffDst = MakeTackyVariable(binary.Type);
                            instructions.Add(new Instruction.Binary(Expression.BinaryOperator.Subtract, ToVal(p1), ToVal(p2), diffDst));
                            var result = MakeTackyVariable(binary.Type);
                            instructions.Add(new Instruction.Binary(Expression.BinaryOperator.Divide, diffDst, new Val.Constant(new Const.ConstLong(TypeChecker.GetTypeSize(((Type.Pointer)GetType(binary.Left)).Referenced, typeTable))), result));
                            return new ExpResult.PlainOperand(result);
                        }

                        var pointer = GetType(binary.Left) is Type.Pointer ? binary.Left : binary.Right;
                        var integer = GetType(binary.Left) is Type.Pointer ? binary.Right : binary.Left;
                        var pointerVal = EmitTackyAndConvert(pointer, instructions);
                        var integerVal = EmitTackyAndConvert(integer, instructions);
                        var negatedDst = MakeTackyVariable(binary.Type);
                        instructions.Add(new Instruction.Unary(Expression.UnaryOperator.Negate, ToVal(integerVal), negatedDst));
                        var dst = MakeTackyVariable(binary.Type);
                        instructions.Add(new Instruction.AddPointer(ToVal(pointerVal), negatedDst, TypeChecker.GetTypeSize(((Type.Pointer)GetType(pointer)).Referenced, typeTable), dst));
                        return new ExpResult.PlainOperand(dst);
                    }
                    else
                    {
                        var v1 = EmitTackyAndConvert(binary.Left, instructions);
                        var v2 = EmitTackyAndConvert(binary.Right, instructions);
                        var dst = MakeTackyVariable(binary.Type);
                        instructions.Add(new Instruction.Binary(binary.Operator, ToVal(v1), ToVal(v2), dst));
                        return new ExpResult.PlainOperand(dst);
                    }
                }
            case Expression.Variable variable:
                return new ExpResult.PlainOperand(new Val.Variable(variable.Identifier));
            case Expression.Assignment assignment:
                {
                    var lval = EmitTacky(assignment.Left, instructions);
                    var rval = EmitTackyAndConvert(assignment.Right, instructions);
                    switch (lval)
                    {
                        case ExpResult.PlainOperand operand:
                            instructions.Add(new Instruction.Copy(ToVal(rval), (Val.Variable)operand.Val));
                            return lval;
                        case ExpResult.DereferencedPointer pointer:
                            instructions.Add(new Instruction.Store(ToVal(rval), pointer.Val));
                            return new ExpResult.PlainOperand(ToVal(rval));
                        case ExpResult.SubObject subObject:
                            instructions.Add(new Instruction.CopyToOffset(ToVal(rval), subObject.Base, subObject.Offset));
                            return new ExpResult.PlainOperand(ToVal(rval));
                        default:
                            throw new NotImplementedException();
                    }
                }
            case Expression.Conditional conditional:
                {
                    var cond = EmitTackyAndConvert(conditional.Condition, instructions);
                    var exp2Label = MakeLabel();
                    instructions.Add(new Instruction.JumpIfZero(ToVal(cond), exp2Label));
                    if (GetType(conditional) is Type.Void)
                    {
                        EmitTackyAndConvert(conditional.Then, instructions);
                        var endLabel = MakeLabel();
                        instructions.Add(new Instruction.Jump(endLabel));
                        instructions.Add(new Instruction.Label(exp2Label));
                        EmitTackyAndConvert(conditional.Else, instructions);
                        instructions.Add(new Instruction.Label(endLabel));
                        return new ExpResult.PlainOperand(new Val.Variable("DUMMY"));
                    }
                    else
                    {
                        var var1 = EmitTackyAndConvert(conditional.Then, instructions);
                        var dst = MakeTackyVariable(conditional.Type);
                        instructions.Add(new Instruction.Copy(ToVal(var1), dst));
                        var endLabel = MakeLabel();
                        instructions.Add(new Instruction.Jump(endLabel));
                        instructions.Add(new Instruction.Label(exp2Label));
                        var var2 = EmitTackyAndConvert(conditional.Else, instructions);
                        instructions.Add(new Instruction.Copy(ToVal(var2), dst));
                        instructions.Add(new Instruction.Label(endLabel));
                        return new ExpResult.PlainOperand(dst);
                    }
                }
            case Expression.FunctionCall functionCall:
                {
                    List<Val> arguments = [];
                    foreach (var arg in functionCall.Arguments)
                    {
                        var val = EmitTackyAndConvert(arg, instructions);
                        arguments.Add(ToVal(val));
                    }

                    if (functionCall.Type is not Type.Void)
                    {
                        var dst = MakeTackyVariable(functionCall.Type);
                        instructions.Add(new Instruction.FunctionCall(functionCall.Identifier, arguments, dst));
                        return new ExpResult.PlainOperand(dst);
                    }
                    else
                    {
                        instructions.Add(new Instruction.FunctionCall(functionCall.Identifier, arguments, null));
                        return new ExpResult.PlainOperand(new Val.Variable("DUMMY"));
                    }
                }
            case Expression.Cast cast:
                {
                    var result = EmitTackyAndConvert(cast.Expression, instructions);
                    var innerType = TypeChecker.GetType(cast.Expression);
                    if (cast.TargetType is Type.Void)
                        return new ExpResult.PlainOperand(new Val.Variable("DUMMY"));
                    if (cast.TargetType == innerType)
                        return result;
                    var dst = MakeTackyVariable(cast.TargetType);

                    var c = MakeCastInstruction(ToVal(result), dst, innerType, cast.TargetType, instructions);
                    if (c != null)
                        instructions.Add(c);

                    return new ExpResult.PlainOperand(dst);
                }
            case Expression.Dereference dereference:
                {
                    var result = EmitTackyAndConvert(dereference.Expression, instructions);
                    return new ExpResult.DereferencedPointer(ToVal(result));
                }
            case Expression.AddressOf addressOf:
                {
                    var val = EmitTacky(addressOf.Expression, instructions);
                    switch (val)
                    {
                        case ExpResult.PlainOperand operand:
                            var dst = MakeTackyVariable(GetType(expression));
                            instructions.Add(new Instruction.GetAddress(operand.Val, dst));
                            return new ExpResult.PlainOperand(dst);
                        case ExpResult.DereferencedPointer pointer:
                            return new ExpResult.PlainOperand(pointer.Val);
                        case ExpResult.SubObject subObject:
                            var objDst = MakeTackyVariable(GetType(expression));
                            instructions.Add(new Instruction.GetAddress(new Val.Variable(subObject.Base), objDst));
                            if (subObject.Offset > 0)
                                instructions.Add(new Instruction.AddPointer(objDst, new Val.Constant(new Const.ConstLong(subObject.Offset)), 1, objDst));
                            return new ExpResult.PlainOperand(objDst);
                        default:
                            throw new NotImplementedException();
                    }
                }
            case Expression.Subscript subscript:
                {
                    var result = EmitTackyAndConvert(new Expression.Binary(Expression.BinaryOperator.Add, subscript.Left, subscript.Right, new Type.Pointer(subscript.Type)), instructions);
                    return new ExpResult.DereferencedPointer(ToVal(result));
                }
            case Expression.String stringExp:
                {
                    var stringLabel = MakeStringTemporary();
                    symbolTable[stringLabel] = new SemanticAnalyzer.SymbolEntry()
                    {
                        IdentifierAttributes = new IdentifierAttributes.Constant(new StaticInit.StringInit(stringExp.StringVal, true)),
                        Type = new Type.Array(new Type.Char(), stringExp.StringVal.Length + 1)
                    };
                    return new ExpResult.PlainOperand(new Val.Variable(stringLabel));
                }
            case Expression.SizeOf sizeofExp:
                {
                    var type = GetType(sizeofExp.Expression);
                    var result = TypeChecker.GetTypeSize(type, typeTable);
                    return new ExpResult.PlainOperand(new Val.Constant(new Const.ConstULong((ulong)result)));
                }
            case Expression.SizeOfType sizeofType:
                {
                    var result = TypeChecker.GetTypeSize(sizeofType.TargetType, typeTable);
                    return new ExpResult.PlainOperand(new Val.Constant(new Const.ConstULong((ulong)result)));
                }
            case Expression.Dot dot:
                {
                    var structDef = typeTable[((Type.Structure)GetType(dot.Structure)).Identifier];
                    var memberOffset = structDef.Members.Find(a => a.MemberName == dot.Member).Offset;
                    var innerObject = EmitTacky(dot.Structure, instructions);
                    switch (innerObject)
                    {
                        case ExpResult.PlainOperand plain:
                            if (plain.Val is Val.Variable var)
                                return new ExpResult.SubObject(var.Name, memberOffset);
                            else
                                throw new NotImplementedException();
                        case ExpResult.SubObject subObject:
                            return new ExpResult.SubObject(subObject.Base, subObject.Offset + memberOffset);
                        case ExpResult.DereferencedPointer pointer:
                            if (memberOffset == 0)
                                return new ExpResult.DereferencedPointer(pointer.Val);
                            var dstPointer = MakeTackyVariable(new Type.Pointer(GetType(expression)));
                            var index = new Val.Constant(new Const.ConstLong(memberOffset));
                            instructions.Add(new Instruction.AddPointer(pointer.Val, index, 1, dstPointer));
                            return new ExpResult.DereferencedPointer(dstPointer);
                        default:
                            throw new NotImplementedException();
                    }
                }
            case Expression.Arrow arrow:
                {
                    var structDef = typeTable[((Type.Structure)((Type.Pointer)GetType(arrow.Pointer)).Referenced).Identifier];
                    var memberOffset = structDef.Members.Find(a => a.MemberName == arrow.Member).Offset;
                    var convertedPointer = EmitTackyAndConvert(arrow.Pointer, instructions);
                    if (memberOffset == 0)
                        return new ExpResult.DereferencedPointer(ToVal(convertedPointer));
                    var dstPointer = MakeTackyVariable(new Type.Pointer(GetType(expression)));
                    var index = new Val.Constant(new Const.ConstLong(memberOffset));
                    instructions.Add(new Instruction.AddPointer(ToVal(convertedPointer), index, 1, dstPointer));
                    return new ExpResult.DereferencedPointer(dstPointer);
                }
            case Expression.CompoundAssignment com:
                {
                    return EmitCompoundAssignment(com, instructions);
                }
            case Expression.PostfixIncrement inc:
                {
                    return MakePostfix(Expression.BinaryOperator.Add, inc.Expression, instructions);
                }
            case Expression.PostfixDecrement dec:
                {
                    return MakePostfix(Expression.BinaryOperator.Subtract, dec.Expression, instructions);
                }
            default:
                throw new NotImplementedException();
        }
    }

    private ExpResult MakePostfix(Expression.BinaryOperator op, Expression inner, List<Instruction> instructions)
    {
        var dst = MakeTackyVariable(GetType(inner));
        var lvalue = EmitTacky(inner, instructions);
        var doOp = (Expression.BinaryOperator op, Val src, Val.Variable dst, List<Instruction> instructions) =>
        {
            var index = op switch
            {
                Expression.BinaryOperator.Add => MakeConstFromType(1, new Type.Long()),
                Expression.BinaryOperator.Subtract => MakeConstFromType(-1, new Type.Long()),
                _ => throw new NotImplementedException()
            };
            if (GetType(inner) is Type.Pointer p)
                instructions.Add(new Instruction.AddPointer(src, new Val.Constant(index), TypeChecker.GetTypeSize(p.Referenced, typeTable), dst));
            else
            {
                var one = new Val.Constant(MakeConstFromType(1, GetType(inner)));
                instructions.Add(new Instruction.Binary(op, src, one, dst));
            }
        };

        switch (lvalue)
        {
            case ExpResult.PlainOperand plain:
                instructions.Add(new Instruction.Copy(plain.Val, dst));
                doOp(op, plain.Val, (Val.Variable)plain.Val, instructions);
                break;
            case ExpResult.DereferencedPointer pointer:
                var tmp = MakeTackyVariable(GetType(inner));
                instructions.Add(new Instruction.Load(ToVal(pointer), dst));
                doOp(op, dst, tmp, instructions);
                instructions.Add(new Instruction.Store(tmp, ToVal(pointer)));
                break;
            case ExpResult.SubObject sub:
                var tmp2 = MakeTackyVariable(GetType(inner));
                instructions.Add(new Instruction.CopyFromOffset(sub.Base, sub.Offset, tmp2));
                doOp(op, dst, tmp2, instructions);
                instructions.Add(new Instruction.CopyToOffset(tmp2, sub.Base, sub.Offset));
                break;
        }

        return new ExpResult.PlainOperand(dst);
    }

    private Instruction? MakeCastInstruction(Val val, Val dst, Type innerType, Type targetType, List<Instruction> instructions)
    {
        Instruction? result = null;
        if (innerType is Type.Double || targetType is Type.Double)
        {
            if (innerType is Type.Int or Type.Long or Type.Char or Type.SChar && targetType is Type.Double)
                result = new Instruction.IntToDouble(val, dst);
            else if (innerType is Type.UInt or Type.ULong or Type.UChar && targetType is Type.Double)
                result = new Instruction.UIntToDouble(val, dst);
            else if (innerType is Type.Double && targetType is Type.Int or Type.Long or Type.Char or Type.SChar)
                result = new Instruction.DoubleToInt(val, dst);
            else if (innerType is Type.Double && targetType is Type.UInt or Type.ULong or Type.UChar)
                result = new Instruction.DoubleToUInt(val, dst);
        }
        else
        {
            if (TypeChecker.GetTypeSize(targetType, typeTable) == TypeChecker.GetTypeSize(innerType, typeTable))
                result = new Instruction.Copy(val, dst);
            else if (TypeChecker.GetTypeSize(targetType, typeTable) < TypeChecker.GetTypeSize(innerType, typeTable))
                result = new Instruction.Truncate(val, dst);
            else if (TypeChecker.IsSignedType(innerType))
                result = new Instruction.SignExtend(val, dst);
            else
                result = new Instruction.ZeroExtend(val, dst);
        }
        return result;
    }

    private ExpResult EmitCompoundAssignment(Expression.CompoundAssignment com, List<Instruction> instructions)
    {
        var leftType = GetType(com.Left);
        var evaluatedLeft = EmitTacky(com.Left, instructions);
        var evaluatedRight = EmitTackyAndConvert(com.Right, instructions);

        Val.Variable? dst = null;
        Instruction? load = null, store = null;
        switch (evaluatedLeft)
        {
            case ExpResult.PlainOperand plain:
                dst = plain.Val as Val.Variable;
                break;
            case ExpResult.DereferencedPointer pointer:
                dst = MakeTackyVariable(leftType);
                load = new Instruction.Load(pointer.Val, dst);
                store = new Instruction.Store(dst, pointer.Val);
                break;
            case ExpResult.SubObject sub:
                dst = MakeTackyVariable(leftType);
                load = new Instruction.CopyFromOffset(sub.Base, sub.Offset, dst);
                store = new Instruction.CopyToOffset(dst, sub.Base, sub.Offset);
                break;
        }

        Val.Variable? resultVar = null;
        Instruction? castTo = null, castFrom = null;
        if (leftType == com.ResultType)
        {
            resultVar = dst;
        }
        else
        {
            var tmp = MakeTackyVariable(com.ResultType);
            resultVar = tmp;
            castTo = MakeCastInstruction(dst!, tmp, leftType, com.ResultType, instructions);
            castFrom = MakeCastInstruction(tmp, dst!, com.ResultType, leftType, instructions);
        }

        List<Instruction> operation = [];
        if (com.Type is Type.Pointer p)
        {
            var scale = TypeChecker.GetTypeSize(p.Referenced, typeTable);
            if (com.Operator is Expression.BinaryOperator.Add)
                operation.Add(new Instruction.AddPointer(resultVar!, ToVal(evaluatedRight), scale, resultVar!));
            else
            {
                var negatedIndex = MakeTackyVariable(new Type.Long());
                operation.Add(new Instruction.Unary(Expression.UnaryOperator.Negate, ToVal(evaluatedRight), negatedIndex));
                operation.Add(new Instruction.AddPointer(resultVar!, negatedIndex, scale, resultVar!));
            }
        }
        else
        {
            operation.Add(new Instruction.Binary(com.Operator, resultVar!, ToVal(evaluatedRight), resultVar!));
        }

        if (load != null)
            instructions.Add(load);
        if (castTo != null)
            instructions.Add(castTo);
        instructions.AddRange(operation);
        if (castFrom != null)
            instructions.Add(castFrom);
        if (store != null)
            instructions.Add(store);
        return new ExpResult.PlainOperand(dst!);
    }

    private Const MakeConstFromType(long val, Type type)
    {
        return type switch
        {
            Type.Char or Type.SChar => new Const.ConstChar((int)val),
            Type.UChar => new Const.ConstUChar((int)val),
            Type.Int => new Const.ConstInt((int)val),
            Type.UInt => new Const.ConstUInt((uint)val),
            Type.Long => new Const.ConstLong((long)val),
            Type.ULong => new Const.ConstULong((ulong)val),
            Type.Double => new Const.ConstDouble((double)val),
            _ => throw new NotImplementedException(),
        };
    }

    private ExpResult EmitTackyAndConvert(Expression expression, List<Instruction> instructions)
    {
        var result = EmitTacky(expression, instructions);
        switch (result)
        {
            case ExpResult.PlainOperand operand:
                return operand;
            case ExpResult.DereferencedPointer pointer:
                var dst = MakeTackyVariable(GetType(expression));
                instructions.Add(new Instruction.Load(pointer.Val, dst));
                return new ExpResult.PlainOperand(dst);
            case ExpResult.SubObject subObject:
                var objDst = MakeTackyVariable(GetType(expression));
                instructions.Add(new Instruction.CopyFromOffset(subObject.Base, subObject.Offset, objDst));
                return new ExpResult.PlainOperand(objDst);
            default:
                throw new NotImplementedException();
        }
    }

    private Val ToVal(ExpResult expResult)
    {
        return expResult switch
        {
            ExpResult.PlainOperand result => result.Val,
            ExpResult.DereferencedPointer result => result.Val,
            _ => throw new NotImplementedException()
        };
    }

    private Val.Variable MakeTackyVariable(Type varType)
    {
        var varName = MakeTemporary();
        symbolTable.Add(varName, new SemanticAnalyzer.SymbolEntry() { Type = varType, IdentifierAttributes = new IdentifierAttributes.Local() });
        return new Val.Variable(varName);
    }

    private Type GetType(Expression expression)
    {
        return TypeChecker.GetType(expression);
    }

    private string MakeStringTemporary()
    {
        return $"string.{counter++}";
    }

    private string MakeTemporary()
    {
        // todo: more descriptive names, e.g. function name
        return $"tmp.{counter++}";
    }

    private string MakeLabel()
    {
        return $"jmp.{counter++}";
    }

    private string MakeBreakLabel(string? loop)
    {
        return $"break_{loop}";
    }

    private string MakeContinueLabel(string? loop)
    {
        return $"continue_{loop}";
    }

    private string MakeStartLabel(string? loop)
    {
        return $"start_{loop}";
    }

    private string MakeCaseLabel(string? label)
    {
        return $"case_{label}";
    }
}