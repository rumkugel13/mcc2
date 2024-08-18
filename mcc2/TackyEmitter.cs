namespace mcc2;

using mcc2.AST;
using mcc2.TAC;

public class TackyEmitter
{
    private uint counter;

    private Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable;

    public TackyEmitter(Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable)
    {
        this.symbolTable = symbolTable;
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
                            instructions.Add(new TopLevel.StaticVariable(entry.Key, stat.Global, entry.Value.Type, init.Inits[0]));
                            break;
                        case InitialValue.Tentative:
                            instructions.Add(new TopLevel.StaticVariable(entry.Key, stat.Global, entry.Value.Type, TypeChecker.ConvertConstantToInit(entry.Value.Type, new Const.ConstInt(0))));
                            break;
                        case InitialValue.NoInitializer:
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

    private void EmitInstruction(BlockItem blockItem, List<Instruction> instructions)
    {
        switch (blockItem)
        {
            case Statement.ReturnStatement returnStatement:
                {
                    var val = EmitTackyAndConvert(returnStatement.Expression, instructions);
                    instructions.Add(new Instruction.Return(ToVal(val)));
                    break;
                }
            case Declaration.VariableDeclaration declaration:
                if (declaration.Initializer != null && declaration.StorageClass == null)
                {
                    // var result = EmitTackyAndConvert(declaration.Initializer, instructions);
                    // instructions.Add(new Instruction.Copy(ToVal(result), new Val.Variable(declaration.Identifier)));
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
                        default:
                            throw new NotImplementedException();
                    }
                }
            case Expression.Conditional conditional:
                {
                    var cond = EmitTackyAndConvert(conditional.Condition, instructions);
                    var exp2Label = MakeLabel();
                    instructions.Add(new Instruction.JumpIfZero(ToVal(cond), exp2Label));
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
            case Expression.FunctionCall functionCall:
                {
                    List<Val> arguments = [];
                    foreach (var arg in functionCall.Arguments)
                    {
                        var val = EmitTackyAndConvert(arg, instructions);
                        arguments.Add(ToVal(val));
                    }

                    var dst = MakeTackyVariable(functionCall.Type);
                    instructions.Add(new Instruction.FunctionCall(functionCall.Identifier, arguments, dst));
                    return new ExpResult.PlainOperand(dst);
                }
            case Expression.Cast cast:
                {
                    var result = EmitTackyAndConvert(cast.Expression, instructions);
                    var innerType = TypeChecker.GetType(cast.Expression);
                    if (cast.TargetType == innerType)
                        return result;
                    var dst = MakeTackyVariable(cast.TargetType);

                    if (innerType is Type.Double || cast.TargetType is Type.Double)
                    {
                        if (innerType is Type.Int or Type.Long && cast.TargetType is Type.Double)
                            instructions.Add(new Instruction.IntToDouble(ToVal(result), dst));
                        else if (innerType is Type.UInt or Type.ULong && cast.TargetType is Type.Double)
                            instructions.Add(new Instruction.UIntToDouble(ToVal(result), dst));
                        else if (innerType is Type.Double && cast.TargetType is Type.Int or Type.Long)
                            instructions.Add(new Instruction.DoubleToInt(ToVal(result), dst));
                        else if (innerType is Type.Double && cast.TargetType is Type.UInt or Type.ULong)
                            instructions.Add(new Instruction.DoubleToUInt(ToVal(result), dst));
                    }
                    else
                    {
                        if (TypeChecker.GetTypeSize(cast.TargetType) == TypeChecker.GetTypeSize(innerType))
                            instructions.Add(new Instruction.Copy(ToVal(result), dst));
                        else if (TypeChecker.GetTypeSize(cast.TargetType) < TypeChecker.GetTypeSize(innerType))
                            instructions.Add(new Instruction.Truncate(ToVal(result), dst));
                        else if (TypeChecker.IsSignedType(innerType))
                            instructions.Add(new Instruction.SignExtend(ToVal(result), dst));
                        else
                            instructions.Add(new Instruction.ZeroExtend(ToVal(result), dst));
                    }

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
                        default:
                            throw new NotImplementedException();
                    }
                }
            default:
                throw new NotImplementedException();
        }
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
}