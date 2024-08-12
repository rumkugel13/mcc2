namespace mcc2;

using mcc2.AST;
using mcc2.TAC;

public class TackyEmitter
{
    private uint counter;

    public TACProgam Emit(ASTProgram astProgram)
    {
        return EmitProgram(astProgram);
    }

    private TACProgam EmitProgram(ASTProgram astProgram)
    {
        List<Function> functionDefinitions = [];
        foreach (var decl in astProgram.Declarations)
            if (decl is FunctionDeclaration fun && fun.Body != null)
                functionDefinitions.Add(EmitFunction(fun));
        return new TACProgam(functionDefinitions);
    }

    private Function EmitFunction(FunctionDeclaration functionDefinition)
    {
        List<Instruction> instructions = [];
        if (functionDefinition.Body != null)
        {
            foreach (var item in functionDefinition.Body.BlockItems)
                EmitInstruction(item, instructions);
            instructions.Add(new Return(new Constant(0)));
        }
        return new Function(functionDefinition.Identifier, functionDefinition.Parameters, instructions);
    }

    private void EmitInstruction(BlockItem blockItem, List<Instruction> instructions)
    {
        switch (blockItem)
        {
            case ReturnStatement returnStatement:
                {
                    var val = EmitInstruction(returnStatement.Expression, instructions);
                    instructions.Add(new Return(val));
                    break;
                }
            case VariableDeclaration declaration:
                if (declaration.Initializer != null)
                {
                    var result = EmitInstruction(declaration.Initializer, instructions);
                    instructions.Add(new Copy(result, new Variable(declaration.Identifier)));
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
                        instructions.Add(new JumpIfZero(cond, endLabel));
                    else
                        instructions.Add(new JumpIfZero(cond, elseLabel));
                    EmitInstruction(ifStatement.Then, instructions);
                    if (ifStatement.Else != null)
                    {
                        instructions.Add(new Jump(endLabel));
                        instructions.Add(new Label(elseLabel));
                        EmitInstruction(ifStatement.Else, instructions);
                    }
                    instructions.Add(new Label(endLabel));
                    break;
                }
            case CompoundStatement compoundStatement:
                foreach (var item in compoundStatement.Block.BlockItems)
                    EmitInstruction(item, instructions);
                break;
            case BreakStatement breakStatement:
                instructions.Add(new Jump(MakeBreakLabel(breakStatement.Label)));
                break;
            case ContinueStatement continueStatement:
                instructions.Add(new Jump(MakeContinueLabel(continueStatement.Label)));
                break;
            case DoWhileStatement doWhileStatement:
                {
                    var start = MakeStartLabel(doWhileStatement.Label);
                    instructions.Add(new Label(start));
                    EmitInstruction(doWhileStatement.Body, instructions);
                    instructions.Add(new Label(MakeContinueLabel(doWhileStatement.Label)));
                    var val = EmitInstruction(doWhileStatement.Condition, instructions);
                    instructions.Add(new JumpIfNotZero(val, start));
                    instructions.Add(new Label(MakeBreakLabel(doWhileStatement.Label)));
                    break;
                }
            case WhileStatement whileStatement:
                {
                    var continueLabel = MakeContinueLabel(whileStatement.Label);
                    instructions.Add(new Label(continueLabel));
                    var val = EmitInstruction(whileStatement.Condition, instructions);
                    var breakLabel = MakeBreakLabel(whileStatement.Label);
                    instructions.Add(new JumpIfZero(val, breakLabel));
                    EmitInstruction(whileStatement.Body, instructions);
                    instructions.Add(new Jump(continueLabel));
                    instructions.Add(new Label(breakLabel));
                    break;
                }
            case ForStatement forStatement:
                {
                    EmitInstruction(forStatement.Init, instructions);
                    var start = MakeStartLabel(forStatement.Label);
                    instructions.Add(new Label(start));
                    var breakLabel = MakeBreakLabel(forStatement.Label);
                    if (forStatement.Condition != null)
                    {
                        var val = EmitInstruction(forStatement.Condition, instructions);
                        instructions.Add(new JumpIfZero(val, breakLabel));
                    }
                    EmitInstruction(forStatement.Body, instructions);
                    var continueLabel = MakeContinueLabel(forStatement.Label);
                    instructions.Add(new Label(continueLabel));
                    if (forStatement.Post != null)
                        EmitInstruction(forStatement.Post, instructions);
                    instructions.Add(new Jump(start));
                    instructions.Add(new Label(breakLabel));
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
            case ConstantExpression constant:
                return new Constant(constant.Value);
            case UnaryExpression unary:
                {
                    var src = EmitInstruction(unary.Expression, instructions);
                    var dstName = MakeTemporary();
                    var dst = new Variable(dstName);
                    instructions.Add(new Unary(unary.Operator, src, dst));
                    return dst;
                }
            case BinaryExpression binary:
                {
                    if (binary.Operator == BinaryExpression.BinaryOperator.And ||
                        binary.Operator == BinaryExpression.BinaryOperator.Or)
                    {
                        return EmitShortCurcuit(binary, instructions);
                    }

                    var v1 = EmitInstruction(binary.ExpressionLeft, instructions);
                    var v2 = EmitInstruction(binary.ExpressionRight, instructions);
                    var dstName = MakeTemporary();
                    var dst = new Variable(dstName);
                    instructions.Add(new Binary(binary.Operator, v1, v2, dst));
                    return dst;
                }
            case VariableExpression variableExpression:
                return new Variable(variableExpression.Identifier);
            case AssignmentExpression assignmentExpression:
                {
                    var result = EmitInstruction(assignmentExpression.ExpressionRight, instructions);
                    var dst = new Variable(((VariableExpression)assignmentExpression.ExpressionLeft).Identifier);
                    instructions.Add(new Copy(result, dst));
                    return dst;
                }
            case ConditionalExpression conditionalExpression:
                {
                    var cond = EmitInstruction(conditionalExpression.Condition, instructions);
                    var exp2Label = MakeLabel();
                    instructions.Add(new JumpIfZero(cond, exp2Label));
                    var var1 = EmitInstruction(conditionalExpression.Then, instructions);
                    var dstName = MakeTemporary();
                    var dst = new Variable(dstName);
                    instructions.Add(new Copy(var1, dst));
                    var endLabel = MakeLabel();
                    instructions.Add(new Jump(endLabel));
                    instructions.Add(new Label(exp2Label));
                    var var2 = EmitInstruction(conditionalExpression.Else, instructions);
                    instructions.Add(new Copy(var2, dst));
                    instructions.Add(new Label(endLabel));
                    return dst;
                }
            case FunctionCallExpression functionCallExpression:
                {
                    List<Val> arguments = [];
                    foreach (var arg in functionCallExpression.Arguments)
                    {
                        var val = EmitInstruction(arg, instructions);
                        arguments.Add(val);
                    }
                    
                    var dstName = MakeTemporary();
                    var dst = new Variable(dstName);
                    instructions.Add(new FunctionCall(functionCallExpression.Identifier, arguments, dst));
                    return dst;
                }
            default:
                throw new NotImplementedException();
        }
    }

    private Val EmitShortCurcuit(BinaryExpression binary, List<Instruction> instructions)
    {
        var dstName = MakeTemporary();
        var dst = new Variable(dstName);

        if (binary.Operator == BinaryExpression.BinaryOperator.And)
        {
            var v1 = EmitInstruction(binary.ExpressionLeft, instructions);
            var falseLabel = MakeLabel();
            instructions.Add(new JumpIfZero(v1, falseLabel));
            var v2 = EmitInstruction(binary.ExpressionRight, instructions);
            instructions.Add(new JumpIfZero(v2, falseLabel));
            instructions.Add(new Copy(new Constant(1), dst));
            var endLabel = MakeLabel();
            instructions.Add(new Jump(endLabel));
            instructions.Add(new Label(falseLabel));
            instructions.Add(new Copy(new Constant(0), dst));
            instructions.Add(new Label(endLabel));
        }
        else
        {
            var v1 = EmitInstruction(binary.ExpressionLeft, instructions);
            var trueLabel = MakeLabel();
            instructions.Add(new JumpIfNotZero(v1, trueLabel));
            var v2 = EmitInstruction(binary.ExpressionRight, instructions);
            instructions.Add(new JumpIfNotZero(v2, trueLabel));
            instructions.Add(new Copy(new Constant(0), dst));
            var endLabel = MakeLabel();
            instructions.Add(new Jump(endLabel));
            instructions.Add(new Label(trueLabel));
            instructions.Add(new Copy(new Constant(1), dst));
            instructions.Add(new Label(endLabel));
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