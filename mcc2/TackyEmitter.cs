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
                            instructions.Add(new TopLevel.StaticVariable(entry.Key, stat.Global, init.Init));
                            break;
                        case InitialValue.Tentative:
                            instructions.Add(new TopLevel.StaticVariable(entry.Key, stat.Global, 0));
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
            if (decl is FunctionDeclaration fun && fun.Body != null)
                definitions.Add(EmitFunction(fun));
        return new TACProgam(definitions);
    }

    private TopLevel.Function EmitFunction(FunctionDeclaration functionDefinition)
    {
        List<Instruction> instructions = [];
        if (functionDefinition.Body != null)
        {
            foreach (var item in functionDefinition.Body.BlockItems)
                EmitInstruction(item, instructions);
            instructions.Add(new Instruction.Return(new Val.Constant(0)));
        }
        return new TopLevel.Function(functionDefinition.Identifier,
            ((IdentifierAttributes.Function)symbolTable[functionDefinition.Identifier].IdentifierAttributes).Global,
            functionDefinition.Parameters, instructions);
    }

    private void EmitInstruction(BlockItem blockItem, List<Instruction> instructions)
    {
        switch (blockItem)
        {
            case ReturnStatement returnStatement:
                {
                    var val = EmitInstruction(returnStatement.Expression, instructions);
                    instructions.Add(new Instruction.Return(val));
                    break;
                }
            case VariableDeclaration declaration:
                if (declaration.Initializer != null && declaration.StorageClass == null)
                {
                    var result = EmitInstruction(declaration.Initializer, instructions);
                    instructions.Add(new Instruction.Copy(result, new Val.Variable(declaration.Identifier)));
                }
                break;
            case ExpressionStatement expressionStatement:
                EmitInstruction(expressionStatement.Expression, instructions);
                break;
            case NullStatement:
                break;
            case IfStatement ifStatement:
                {
                    var cond = EmitInstruction(ifStatement.Condition, instructions);
                    var endLabel = MakeLabel();
                    var elseLabel = MakeLabel();
                    if (ifStatement.Else == null)
                        instructions.Add(new Instruction.JumpIfZero(cond, endLabel));
                    else
                        instructions.Add(new Instruction.JumpIfZero(cond, elseLabel));
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
            case CompoundStatement compoundStatement:
                foreach (var item in compoundStatement.Block.BlockItems)
                    EmitInstruction(item, instructions);
                break;
            case BreakStatement breakStatement:
                instructions.Add(new Instruction.Jump(MakeBreakLabel(breakStatement.Label)));
                break;
            case ContinueStatement continueStatement:
                instructions.Add(new Instruction.Jump(MakeContinueLabel(continueStatement.Label)));
                break;
            case DoWhileStatement doWhileStatement:
                {
                    var start = MakeStartLabel(doWhileStatement.Label);
                    instructions.Add(new Instruction.Label(start));
                    EmitInstruction(doWhileStatement.Body, instructions);
                    instructions.Add(new Instruction.Label(MakeContinueLabel(doWhileStatement.Label)));
                    var val = EmitInstruction(doWhileStatement.Condition, instructions);
                    instructions.Add(new Instruction.JumpIfNotZero(val, start));
                    instructions.Add(new Instruction.Label(MakeBreakLabel(doWhileStatement.Label)));
                    break;
                }
            case WhileStatement whileStatement:
                {
                    var continueLabel = MakeContinueLabel(whileStatement.Label);
                    instructions.Add(new Instruction.Label(continueLabel));
                    var val = EmitInstruction(whileStatement.Condition, instructions);
                    var breakLabel = MakeBreakLabel(whileStatement.Label);
                    instructions.Add(new Instruction.JumpIfZero(val, breakLabel));
                    EmitInstruction(whileStatement.Body, instructions);
                    instructions.Add(new Instruction.Jump(continueLabel));
                    instructions.Add(new Instruction.Label(breakLabel));
                    break;
                }
            case ForStatement forStatement:
                {
                    EmitInstruction(forStatement.Init, instructions);
                    var start = MakeStartLabel(forStatement.Label);
                    instructions.Add(new Instruction.Label(start));
                    var breakLabel = MakeBreakLabel(forStatement.Label);
                    if (forStatement.Condition != null)
                    {
                        var val = EmitInstruction(forStatement.Condition, instructions);
                        instructions.Add(new Instruction.JumpIfZero(val, breakLabel));
                    }
                    EmitInstruction(forStatement.Body, instructions);
                    var continueLabel = MakeContinueLabel(forStatement.Label);
                    instructions.Add(new Instruction.Label(continueLabel));
                    if (forStatement.Post != null)
                        EmitInstruction(forStatement.Post, instructions);
                    instructions.Add(new Instruction.Jump(start));
                    instructions.Add(new Instruction.Label(breakLabel));
                    break;
                }
            case FunctionDeclaration functionDeclaration:
                EmitFunction(functionDeclaration);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void EmitInstruction(ForInit init, List<Instruction> instructions)
    {
        if (init is InitDeclaration initDeclaration)
        {
            EmitInstruction(initDeclaration.Declaration, instructions);
        }
        else if (init is InitExpression initExpression && initExpression.Expression != null)
        {
            EmitInstruction(initExpression.Expression, instructions);
        }
    }

    private Val EmitInstruction(Expression expression, List<Instruction> instructions)
    {
        switch (expression)
        {
            case Expression.ConstantExpression constant:
                return new Val.Constant(constant.Value);
            case Expression.UnaryExpression unary:
                {
                    var src = EmitInstruction(unary.Expression, instructions);
                    var dstName = MakeTemporary();
                    var dst = new Val.Variable(dstName);
                    instructions.Add(new Instruction.Unary(unary.Operator, src, dst));
                    return dst;
                }
            case Expression.BinaryExpression binary:
                {
                    if (binary.Operator == Expression.BinaryOperator.And ||
                        binary.Operator == Expression.BinaryOperator.Or)
                    {
                        return EmitShortCurcuit(binary, instructions);
                    }

                    var v1 = EmitInstruction(binary.ExpressionLeft, instructions);
                    var v2 = EmitInstruction(binary.ExpressionRight, instructions);
                    var dstName = MakeTemporary();
                    var dst = new Val.Variable(dstName);
                    instructions.Add(new Instruction.Binary(binary.Operator, v1, v2, dst));
                    return dst;
                }
            case Expression.VariableExpression variableExpression:
                return new Val.Variable(variableExpression.Identifier);
            case Expression.AssignmentExpression assignmentExpression:
                {
                    var result = EmitInstruction(assignmentExpression.ExpressionRight, instructions);
                    var dst = new Val.Variable(((Expression.VariableExpression)assignmentExpression.ExpressionLeft).Identifier);
                    instructions.Add(new Instruction.Copy(result, dst));
                    return dst;
                }
            case Expression.ConditionalExpression conditionalExpression:
                {
                    var cond = EmitInstruction(conditionalExpression.Condition, instructions);
                    var exp2Label = MakeLabel();
                    instructions.Add(new Instruction.JumpIfZero(cond, exp2Label));
                    var var1 = EmitInstruction(conditionalExpression.Then, instructions);
                    var dstName = MakeTemporary();
                    var dst = new Val.Variable(dstName);
                    instructions.Add(new Instruction.Copy(var1, dst));
                    var endLabel = MakeLabel();
                    instructions.Add(new Instruction.Jump(endLabel));
                    instructions.Add(new Instruction.Label(exp2Label));
                    var var2 = EmitInstruction(conditionalExpression.Else, instructions);
                    instructions.Add(new Instruction.Copy(var2, dst));
                    instructions.Add(new Instruction.Label(endLabel));
                    return dst;
                }
            case Expression.FunctionCallExpression functionCallExpression:
                {
                    List<Val> arguments = [];
                    foreach (var arg in functionCallExpression.Arguments)
                    {
                        var val = EmitInstruction(arg, instructions);
                        arguments.Add(val);
                    }

                    var dstName = MakeTemporary();
                    var dst = new Val.Variable(dstName);
                    instructions.Add(new Instruction.FunctionCall(functionCallExpression.Identifier, arguments, dst));
                    return dst;
                }
            default:
                throw new NotImplementedException();
        }
    }

    private Val EmitShortCurcuit(Expression.BinaryExpression binary, List<Instruction> instructions)
    {
        var dstName = MakeTemporary();
        var dst = new Val.Variable(dstName);

        if (binary.Operator == Expression.BinaryOperator.And)
        {
            var v1 = EmitInstruction(binary.ExpressionLeft, instructions);
            var falseLabel = MakeLabel();
            instructions.Add(new Instruction.JumpIfZero(v1, falseLabel));
            var v2 = EmitInstruction(binary.ExpressionRight, instructions);
            instructions.Add(new Instruction.JumpIfZero(v2, falseLabel));
            instructions.Add(new Instruction.Copy(new Val.Constant(1), dst));
            var endLabel = MakeLabel();
            instructions.Add(new Instruction.Jump(endLabel));
            instructions.Add(new Instruction.Label(falseLabel));
            instructions.Add(new Instruction.Copy(new Val.Constant(0), dst));
            instructions.Add(new Instruction.Label(endLabel));
        }
        else
        {
            var v1 = EmitInstruction(binary.ExpressionLeft, instructions);
            var trueLabel = MakeLabel();
            instructions.Add(new Instruction.JumpIfNotZero(v1, trueLabel));
            var v2 = EmitInstruction(binary.ExpressionRight, instructions);
            instructions.Add(new Instruction.JumpIfNotZero(v2, trueLabel));
            instructions.Add(new Instruction.Copy(new Val.Constant(0), dst));
            var endLabel = MakeLabel();
            instructions.Add(new Instruction.Jump(endLabel));
            instructions.Add(new Instruction.Label(trueLabel));
            instructions.Add(new Instruction.Copy(new Val.Constant(1), dst));
            instructions.Add(new Instruction.Label(endLabel));
        }
        return dst;
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