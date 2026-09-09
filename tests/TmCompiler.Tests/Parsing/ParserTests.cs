using TmCompiler.Application.Lexing;
using TmCompiler.Application.Parsing;
using TmCompiler.Domain.Lexical;
using TmCompiler.Domain.Syntactic;
using Xunit;

namespace TmCompiler.Tests;

public class ParserTests
{
    private static ProgramNode Parse(string source)
    {
        var lexer = new Lexer(source, "test.tmpr");
        var tokens = lexer.Tokenize();

        var parser = new Parser(tokens, "test.tmpr");
        return parser.Parse();
    }

    [Fact]
    public void Parse_EmptySession_ReturnsProgramNode()
    {
        var program = Parse(
            """
            session >
            <
            """
        );

        Assert.NotNull(program);
        Assert.NotNull(program.Session);
        Assert.NotNull(program.Session.Body);
    }

    [Fact]
    public void Parse_EmptySession_HasNoStatements()
    {
        var program = Parse(
            """
            session >
            <
            """
        );

        Assert.Empty(program.Session.Body.Statements);
    }

    [Fact]
    public void Parse_MissingBlockStart_ThrowsParserException()
    {
        const string source =
            """
            session <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_MissingBlockEnd_ThrowsParserException()
    {
        const string source =
            """
            session >
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_MissingSession_ThrowsParserException()
    {
        const string source =
            """
            >
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_ExtraTokensAfterSession_ThrowsParserException()
    {
        const string source =
            """
            session >
            <
            extra
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_VariableDeclaration_CreatesVariableDeclarationNode()
    {
        var program = Parse(
            """
            session >
                sample x = 10;
            <
            """
        );

        var statement = Assert.Single(program.Session.Body.Statements);

        var variable = Assert.IsType<VariableDeclarationNode>(statement);

        Assert.Equal("sample", variable.TypeName);
        Assert.Equal("x", variable.Identifier);
    }

    [Fact]
    public void Parse_VariableDeclaration_WithIntegerLiteral_CreatesLiteralInitializer()
    {
        var program = Parse(
            """
            session >
                sample edad = 22;
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        var literal = Assert.IsType<LiteralExpressionNode>(
            variable.Initializer);

        Assert.Equal(TokenType.IntegerLiteral, literal.LiteralType);
        Assert.Equal("22", literal.Value);
    }

    [Fact]
    public void Parse_MultipleVariableDeclarations_CreatesMultipleStatements()
    {
        var program = Parse(
            """
            session >
                sample edad = 22;
                wave altura = 1.75;
                phrase nombre = "Ana";
                tone inicial = 'A';
                signal activo = true;
            <
            """
        );

        Assert.Equal(5, program.Session.Body.Statements.Count);
    }

    [Fact]
    public void Parse_VariableDeclaration_MissingIdentifier_ThrowsParserException()
    {
        const string source =
            """
            session >
                sample = 10;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_VariableDeclaration_MissingAssign_ThrowsParserException()
    {
        const string source =
            """
            session >
                sample x 10;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_VariableDeclaration_MissingInitializer_ThrowsParserException()
    {
        const string source =
            """
            session >
                sample x = ;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_VariableDeclaration_MissingSemicolon_ThrowsParserException()
    {
        const string source =
            """
            session >
                sample x = 10
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_AdditiveExpression_CreatesBinaryExpression()
    {
        var program = Parse(
            """
            session >
                sample x = 10 + 20;
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        var binary = Assert.IsType<BinaryExpressionNode>(
            variable.Initializer);

        Assert.Equal(TokenType.Plus, binary.Operator);

        var left = Assert.IsType<LiteralExpressionNode>(binary.Left);
        var right = Assert.IsType<LiteralExpressionNode>(binary.Right);

        Assert.Equal("10", left.Value);
        Assert.Equal("20", right.Value);
    }

    [Fact]
    public void Parse_MultiplicationHasHigherPrecedenceThanAddition()
    {
        var program = Parse(
            """
            session >
                sample x = 2 + 3 * 4;
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        var addition = Assert.IsType<BinaryExpressionNode>(
            variable.Initializer);

        Assert.Equal(TokenType.Plus, addition.Operator);

        var left = Assert.IsType<LiteralExpressionNode>(
            addition.Left);

        Assert.Equal("2", left.Value);

        var multiplication = Assert.IsType<BinaryExpressionNode>(
            addition.Right);

        Assert.Equal(TokenType.Multiply, multiplication.Operator);

        var multLeft = Assert.IsType<LiteralExpressionNode>(
            multiplication.Left);

        var multRight = Assert.IsType<LiteralExpressionNode>(
            multiplication.Right);

        Assert.Equal("3", multLeft.Value);
        Assert.Equal("4", multRight.Value);
    }

    [Fact]
    public void Parse_ParenthesesOverrideOperatorPrecedence()
    {
        var program = Parse(
            """
            session >
                sample x = (2 + 3) * 4;
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        var multiplication = Assert.IsType<BinaryExpressionNode>(
            variable.Initializer);

        Assert.Equal(TokenType.Multiply, multiplication.Operator);

        var addition = Assert.IsType<BinaryExpressionNode>(
            multiplication.Left);

        Assert.Equal(TokenType.Plus, addition.Operator);

        var right = Assert.IsType<LiteralExpressionNode>(
            multiplication.Right);

        Assert.Equal("4", right.Value);
    }

    [Fact]
    public void Parse_NegativeNumber_CreatesUnaryExpression()
    {
        var program = Parse(
            """
            session >
                sample x = -10;
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        var unary = Assert.IsType<UnaryExpressionNode>(
            variable.Initializer);

        Assert.Equal(TokenType.Minus, unary.Operator);

        var operand = Assert.IsType<LiteralExpressionNode>(
            unary.Operand);

        Assert.Equal("10", operand.Value);
    }

    [Fact]
    public void Parse_IdentifierExpression_CreatesIdentifierNode()
    {
        var program = Parse(
            """
            session >
                sample x = 10;
                sample y = x;
            <
            """
        );

        var second = Assert.IsType<VariableDeclarationNode>(
            program.Session.Body.Statements[1]);

        var identifier = Assert.IsType<IdentifierExpressionNode>(
            second.Initializer);

        Assert.Equal("x", identifier.Name);
    }

    [Fact]
    public void Parse_RelationalAndLogicalExpression_RespectsPrecedence()
    {
        var program = Parse(
            """
            session >
                signal resultado = x >= 10 and activo;
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        var andExpression = Assert.IsType<BinaryExpressionNode>(
            variable.Initializer);

        Assert.Equal(TokenType.And, andExpression.Operator);

        var comparison = Assert.IsType<BinaryExpressionNode>(
            andExpression.Left);

        Assert.Equal(TokenType.GreaterOrEqual, comparison.Operator);

        var active = Assert.IsType<IdentifierExpressionNode>(
            andExpression.Right);

        Assert.Equal("activo", active.Name);
    }

    [Fact]
    public void Parse_UnclosedParenthesizedExpression_ThrowsParserException()
    {
        const string source =
            """
            session >
                sample x = (10 + 20;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_OutputStatement_CreatesOutputNode()
    {
        var program = Parse(
            """
            session >
                output >> "Hola";
            <
            """
        );

        var statement = Assert.Single(program.Session.Body.Statements);

        var output = Assert.IsType<OutputStatementNode>(statement);

        var literal = Assert.IsType<LiteralExpressionNode>(
            output.Expression);

        Assert.Equal("Hola", literal.Value);
    }

    [Fact]
    public void Parse_OutputStatement_WithExpression_CreatesExpressionTree()
    {
        var program = Parse(
            """
            session >
                output >> x + 10;
            <
            """
        );

        var output = Assert.IsType<OutputStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        var binary = Assert.IsType<BinaryExpressionNode>(
            output.Expression);

        Assert.Equal(TokenType.Plus, binary.Operator);

        var left = Assert.IsType<IdentifierExpressionNode>(
            binary.Left);

        var right = Assert.IsType<LiteralExpressionNode>(
            binary.Right);

        Assert.Equal("x", left.Name);
        Assert.Equal("10", right.Value);
    }

    [Fact]
    public void Parse_InputStatement_CreatesInputNode()
    {
        var program = Parse(
            """
            session >
                input << edad;
            <
            """
        );

        var statement = Assert.Single(program.Session.Body.Statements);

        var input = Assert.IsType<InputStatementNode>(statement);

        Assert.Equal("edad", input.Identifier);
    }

    [Fact]
    public void Parse_OutputStatement_MissingOutputOperator_ThrowsParserException()
    {
        const string source =
            """
            session >
                output "Hola";
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_InputStatement_MissingInputOperator_ThrowsParserException()
    {
        const string source =
            """
            session >
                input edad;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_InputStatement_WithNonIdentifier_ThrowsParserException()
    {
        const string source =
            """
            session >
                input << 10;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_ConstantDeclaration_CreatesConstantNode()
    {
        var program = Parse(
            """
            session >
                const sample MAX = 100;
            <
            """
        );

        var statement = Assert.Single(program.Session.Body.Statements);

        var constant = Assert.IsType<ConstantDeclarationNode>(statement);

        Assert.Equal("sample", constant.TypeName);
        Assert.Equal("MAX", constant.Identifier);

        var literal = Assert.IsType<LiteralExpressionNode>(
            constant.Initializer);

        Assert.Equal("100", literal.Value);
    }

    [Fact]
    public void Parse_ConstantDeclaration_MissingType_ThrowsParserException()
    {
        const string source =
            """
            session >
                const MAX = 100;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_ConstantDeclaration_MissingIdentifier_ThrowsParserException()
    {
        const string source =
            """
            session >
                const sample = 100;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_AssignmentStatement_CreatesAssignmentNode()
    {
        var program = Parse(
            """
            session >
                sample x = 10;
                x = x + 5;
            <
            """
        );

        var assignment = Assert.IsType<AssignmentStatementNode>(
            program.Session.Body.Statements[1]);

        Assert.Equal("x", assignment.Identifier);

        var expression = Assert.IsType<BinaryExpressionNode>(
            assignment.Value);

        Assert.Equal(TokenType.Plus, expression.Operator);
    }

    [Fact]
    public void Parse_AssignmentStatement_MissingValue_ThrowsParserException()
    {
        const string source =
            """
            session >
                sample x = 10;
                x = ;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_AssignmentStatement_MissingSemicolon_ThrowsParserException()
    {
        const string source =
            """
            session >
                sample x = 10;
                x = 20
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_ReturnWithoutExpression_CreatesReturnNode()
    {
        var program = Parse(
            """
            session >
                ret;
            <
            """
        );

        var statement = Assert.Single(program.Session.Body.Statements);

        var ret = Assert.IsType<ReturnStatementNode>(statement);

        Assert.Null(ret.Expression);
    }

    [Fact]
    public void Parse_ReturnWithExpression_CreatesReturnNode()
    {
        var program = Parse(
            """
            session >
                ret 10;
            <
            """
        );

        var statement = Assert.Single(program.Session.Body.Statements);

        var ret = Assert.IsType<ReturnStatementNode>(statement);

        var literal = Assert.IsType<LiteralExpressionNode>(
            ret.Expression);

        Assert.Equal("10", literal.Value);
    }

    [Fact]
    public void Parse_ReturnWithBinaryExpression_CreatesExpressionTree()
    {
        var program = Parse(
            """
            session >
                ret a + b;
            <
            """
        );

        var ret = Assert.IsType<ReturnStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        var binary = Assert.IsType<BinaryExpressionNode>(
            ret.Expression);

        Assert.Equal(TokenType.Plus, binary.Operator);

        var left = Assert.IsType<IdentifierExpressionNode>(
            binary.Left);

        var right = Assert.IsType<IdentifierExpressionNode>(
            binary.Right);

        Assert.Equal("a", left.Name);
        Assert.Equal("b", right.Name);
    }

    [Fact]
    public void Parse_ReturnMissingSemicolon_ThrowsParserException()
    {
        const string source =
            """
            session >
                ret 10
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_IfStatement_CreatesIfNode()
    {
        var program = Parse(
            """
            session >
                if [x > 10] >
                    output >> "Mayor";
                <
            <
            """
        );

        var statement = Assert.Single(program.Session.Body.Statements);

        var ifNode = Assert.IsType<IfStatementNode>(statement);

        Assert.NotNull(ifNode.Condition);
        Assert.NotNull(ifNode.ThenBlock);
        Assert.Empty(ifNode.ElseIfClauses);
        Assert.Null(ifNode.ElseBlock);
    }

    [Fact]
    public void Parse_IfElseStatement_CreatesElseBlock()
    {
        var program = Parse(
            """
            session >
                if [x > 10] >
                    output >> "Mayor";
                <
                else >
                    output >> "Menor o igual";
                <
            <
            """
        );

        var ifNode = Assert.IsType<IfStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        Assert.NotNull(ifNode.ElseBlock);
        Assert.Empty(ifNode.ElseIfClauses);
    }

    [Fact]
    public void Parse_ElseIfStatement_CreatesElseIfClause()
    {
        var program = Parse(
            """
            session >
                if [x > 10] >
                    output >> "Mayor";
                <
                else if [x == 10] >
                    output >> "Igual";
                <
                else >
                    output >> "Menor";
                <
            <
            """
        );

        var ifNode = Assert.IsType<IfStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        var elseIf = Assert.Single(ifNode.ElseIfClauses);

        Assert.NotNull(elseIf.Condition);
        Assert.NotNull(elseIf.Body);
        Assert.NotNull(ifNode.ElseBlock);
    }

    [Fact]
    public void Parse_MultipleElseIfClauses_CreatesAllClauses()
    {
        var program = Parse(
            """
            session >
                if [x == 1] >
                    output >> "Uno";
                <
                else if [x == 2] >
                    output >> "Dos";
                <
                else if [x == 3] >
                    output >> "Tres";
                <
                else >
                    output >> "Otro";
                <
            <
            """
        );

        var ifNode = Assert.IsType<IfStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        Assert.Equal(2, ifNode.ElseIfClauses.Count);
        Assert.NotNull(ifNode.ElseBlock);
    }

    [Fact]
    public void Parse_IfCondition_UsesExpressionParser()
    {
        var program = Parse(
            """
            session >
                if [x >= 10 and activo] >
                    output >> "Valido";
                <
            <
            """
        );

        var ifNode = Assert.IsType<IfStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        var andExpression = Assert.IsType<BinaryExpressionNode>(
            ifNode.Condition);

        Assert.Equal(TokenType.And, andExpression.Operator);

        var comparison = Assert.IsType<BinaryExpressionNode>(
            andExpression.Left);

        Assert.Equal(TokenType.GreaterOrEqual, comparison.Operator);
    }

    [Fact]
    public void Parse_IfMissingLeftBracket_ThrowsParserException()
    {
        const string source =
            """
            session >
                if x > 10] >
                <
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_IfMissingRightBracket_ThrowsParserException()
    {
        const string source =
            """
            session >
                if [x > 10 >
                <
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_ElseWithoutBlock_ThrowsParserException()
    {
        const string source =
            """
            session >
                if [x > 10] >
                <
                else
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_RepeatStatement_CreatesRepeatNode()
    {
        var program = Parse(
            """
            session >
                repeat [x < 10] >
                    x = x + 1;
                <
            <
            """
        );

        var repeat = Assert.IsType<RepeatStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        Assert.NotNull(repeat.Condition);
        Assert.NotNull(repeat.Body);

        var condition = Assert.IsType<BinaryExpressionNode>(
            repeat.Condition);

        Assert.Equal(TokenType.LessThan, condition.Operator);
    }

    [Fact]
    public void Parse_RepeatMissingBrackets_ThrowsParserException()
    {
        const string source =
            """
            session >
                repeat x < 10 >
                <
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_LoopStatement_WithIncrement_CreatesLoopNode()
    {
        var program = Parse(
            """
            session >
                loop [sample i = 0; i < 5; i++] >
                    output >> i;
                <
            <
            """
        );

        var loop = Assert.IsType<LoopStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        Assert.Equal("i", loop.Initializer.Identifier);

        var condition = Assert.IsType<BinaryExpressionNode>(
            loop.Condition);

        Assert.Equal(TokenType.LessThan, condition.Operator);

        Assert.Equal("i", loop.Update.Identifier);
        Assert.Equal(TokenType.Increment, loop.Update.Operator);
        Assert.Null(loop.Update.Value);

        Assert.NotNull(loop.Body);
    }

    [Fact]
    public void Parse_LoopStatement_WithDecrement_CreatesDecrementUpdate()
    {
        var program = Parse(
            """
            session >
                loop [sample i = 10; i > 0; i--] >
                    output >> i;
                <
            <
            """
        );

        var loop = Assert.IsType<LoopStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        Assert.Equal(TokenType.Decrement, loop.Update.Operator);
        Assert.Null(loop.Update.Value);
    }

    [Fact]
    public void Parse_LoopStatement_WithAssignmentUpdate_CreatesUpdateExpression()
    {
        var program = Parse(
            """
            session >
                loop [sample i = 0; i < 10; i = i + 2] >
                    output >> i;
                <
            <
            """
        );

        var loop = Assert.IsType<LoopStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        Assert.Equal(TokenType.Assign, loop.Update.Operator);

        var updateExpression = Assert.IsType<BinaryExpressionNode>(
            loop.Update.Value);

        Assert.Equal(TokenType.Plus, updateExpression.Operator);
    }

    [Fact]
    public void Parse_LoopMissingInitializer_ThrowsParserException()
    {
        const string source =
            """
            session >
                loop [i < 5; i++] >
                <
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_LoopMissingConditionSemicolon_ThrowsParserException()
    {
        const string source =
            """
            session >
                loop [sample i = 0; i < 5 i++] >
                <
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_LoopInvalidUpdate_ThrowsParserException()
    {
        const string source =
            """
            session >
                loop [sample i = 0; i < 5; i + 1] >
                <
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_MixStatement_CreatesMixNode()
    {
        var program = Parse(
            """
            session >
                sample opcion = 2;

                mix opcion >
                    case 1: >
                        output >> "Uno";
                    <
                    case 2: >
                        output >> "Dos";
                    <
                    default: >
                        output >> "Otro";
                    <
                <
            <
            """
        );

        var mix = Assert.IsType<MixStatementNode>(
            program.Session.Body.Statements[1]);

        var expression = Assert.IsType<IdentifierExpressionNode>(
            mix.Expression);

        Assert.Equal("opcion", expression.Name);
        Assert.Equal(2, mix.Cases.Count);
        Assert.NotNull(mix.DefaultBlock);
    }

    [Fact]
    public void Parse_MixWithoutDefault_IsValid()
    {
        var program = Parse(
            """
            session >
                mix opcion >
                    case 1: >
                        output >> "Uno";
                    <
                    case 2: >
                        output >> "Dos";
                    <
                <
            <
            """
        );

        var mix = Assert.IsType<MixStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        Assert.Equal(2, mix.Cases.Count);
        Assert.Null(mix.DefaultBlock);
    }

    [Fact]
    public void Parse_MixCase_CreatesCaseValueAndBody()
    {
        var program = Parse(
            """
            session >
                mix opcion >
                    case 10: >
                        output >> "Diez";
                    <
                <
            <
            """
        );

        var mix = Assert.IsType<MixStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        var mixCase = Assert.Single(mix.Cases);

        var value = Assert.IsType<LiteralExpressionNode>(
            mixCase.Value);

        Assert.Equal("10", value.Value);
        Assert.Single(mixCase.Body.Statements);
    }

    [Fact]
    public void Parse_MixMissingCaseColon_ThrowsParserException()
    {
        const string source =
            """
            session >
                mix opcion >
                    case 1 >
                        output >> "Uno";
                    <
                <
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_MixMissingCaseBlock_ThrowsParserException()
    {
        const string source =
            """
            session >
                mix opcion >
                    case 1:
                        output >> "Uno";
                <
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_MixWithMultipleDefaults_ThrowsParserException()
    {
        const string source =
            """
            session >
                mix opcion >
                    default: >
                        output >> "Uno";
                    <
                    default: >
                        output >> "Dos";
                    <
                <
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_MixUnexpectedStatementOutsideCase_ThrowsParserException()
    {
        const string source =
            """
            session >
                mix opcion >
                    output >> "Incorrecto";
                <
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_MixMissingClosingDelimiter_ThrowsParserException()
    {
        const string source =
            """
            session >
                mix opcion >
                    case 1: >
                        output >> "Uno";
                    <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_FunctionDeclaration_CreatesFunctionNode()
    {
        var program = Parse(
            """
            sample : compose sumar(sample a, sample b) >
                ret a + b;
            <

            session >
            <
            """
        );

        var function = Assert.Single(program.Functions);

        Assert.Equal("sample", function.ReturnType);
        Assert.Equal("sumar", function.Identifier);
        Assert.Equal(2, function.Parameters.Count);
        Assert.Equal("a", function.Parameters[0].Identifier);
        Assert.Equal("b", function.Parameters[1].Identifier);
        Assert.NotNull(function.Body);
    }

    [Fact]
    public void Parse_VoidFunctionWithoutParameters_IsValid()
    {
        var program = Parse(
            """
            void : compose saludar() >
                output >> "Hola";
                ret;
            <

            session >
            <
            """
        );

        var function = Assert.Single(program.Functions);

        Assert.Equal("void", function.ReturnType);
        Assert.Equal("saludar", function.Identifier);
        Assert.Empty(function.Parameters);
    }

    [Fact]
    public void Parse_MultipleFunctions_CreatesAllFunctions()
    {
        var program = Parse(
            """
            sample : compose sumar(sample a, sample b) >
                ret a + b;
            <

            void : compose saludar(phrase nombre) >
                output >> nombre;
                ret;
            <

            session >
            <
            """
        );

        Assert.Equal(2, program.Functions.Count);
    }

    [Fact]
    public void Parse_FunctionCallInsideVariableDeclaration_CreatesCallExpression()
    {
        var program = Parse(
            """
            sample : compose sumar(sample a, sample b) >
                ret a + b;
            <

            session >
                sample resultado = sumar(2, 3);
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        var call = Assert.IsType<CallExpressionNode>(
            variable.Initializer);

        Assert.Equal("sumar", call.Identifier);
        Assert.Equal(2, call.Arguments.Count);
    }

    [Fact]
    public void Parse_FunctionCallAsStatement_CreatesExpressionStatement()
    {
        var program = Parse(
            """
            void : compose saludar(phrase nombre) >
                ret;
            <

            session >
                saludar("Ana");
            <
            """
        );

        var statement = Assert.Single(program.Session.Body.Statements);

        var expressionStatement =
            Assert.IsType<ExpressionStatementNode>(statement);

        var call = Assert.IsType<CallExpressionNode>(
            expressionStatement.Expression);

        Assert.Equal("saludar", call.Identifier);
        Assert.Single(call.Arguments);
    }

    [Fact]
    public void Parse_RecursiveFunctionCall_IsSyntacticallyValid()
    {
        var program = Parse(
            """
            sample : compose factorial(sample n) >
                if [n <= 1] >
                    ret 1;
                <
                ret n * factorial(n - 1);
            <

            session >
            <
            """
        );

        var function = Assert.Single(program.Functions);

        Assert.Equal("factorial", function.Identifier);
        Assert.NotNull(function.Body);
    }

    [Fact]
    public void Parse_FunctionMissingColon_ThrowsParserException()
    {
        const string source =
            """
            sample compose sumar(sample a) >
                ret a;
            <

            session >
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_FunctionMissingCompose_ThrowsParserException()
    {
        const string source =
            """
            sample : sumar(sample a) >
                ret a;
            <

            session >
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_FunctionMissingClosingParenthesis_ThrowsParserException()
    {
        const string source =
            """
            sample : compose sumar(sample a >
                ret a;
            <

            session >
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_FunctionCallMissingClosingParenthesis_ThrowsParserException()
    {
        const string source =
            """
            session >
                sample resultado = sumar(2, 3;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_SequenceDeclaration_CreatesCollectionLiteral()
    {
        var program = Parse(
            """
            session >
                sequence<sample> numeros = [1, 2, 3];
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        Assert.Equal("sequence<sample>", variable.TypeName);

        var collection = Assert.IsType<CollectionLiteralExpressionNode>(
            variable.Initializer);

        Assert.Equal(3, collection.Elements.Count);
    }

    [Fact]
    public void Parse_ScoreDeclaration_CreatesNestedCollections()
    {
        var program = Parse(
            """
            session >
                score<sample> matriz = [
                    [1, 2],
                    [3, 4]
                ];
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        Assert.Equal("score<sample>", variable.TypeName);

        var matrix = Assert.IsType<CollectionLiteralExpressionNode>(
            variable.Initializer);

        Assert.Equal(2, matrix.Elements.Count);

        Assert.IsType<CollectionLiteralExpressionNode>(
            matrix.Elements[0]);

        Assert.IsType<CollectionLiteralExpressionNode>(
            matrix.Elements[1]);
    }

    [Fact]
    public void Parse_SequenceIndex_CreatesIndexExpression()
    {
        var program = Parse(
            """
            session >
                sample valor = numeros[0];
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        var index = Assert.IsType<IndexExpressionNode>(
            variable.Initializer);

        var target = Assert.IsType<IdentifierExpressionNode>(
            index.Target);

        Assert.Equal("numeros", target.Name);

        var indexValue = Assert.IsType<LiteralExpressionNode>(
            index.Index);

        Assert.Equal("0", indexValue.Value);
    }

    [Fact]
    public void Parse_MatrixDoubleIndex_CreatesNestedIndexExpressions()
    {
        var program = Parse(
            """
            session >
                sample valor = matriz[1][2];
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        var outerIndex = Assert.IsType<IndexExpressionNode>(
            variable.Initializer);

        var innerIndex = Assert.IsType<IndexExpressionNode>(
            outerIndex.Target);

        var matrix = Assert.IsType<IdentifierExpressionNode>(
            innerIndex.Target);

        Assert.Equal("matriz", matrix.Name);
    }

    [Fact]
    public void Parse_FunctionWithSequenceParameter_IsValid()
    {
        var program = Parse(
            """
            sample : compose primero(sequence<sample> numeros) >
                ret numeros[0];
            <

            session >
            <
            """
        );

        var function = Assert.Single(program.Functions);

        var parameter = Assert.Single(function.Parameters);

        Assert.Equal("sequence<sample>", parameter.TypeName);
        Assert.Equal("numeros", parameter.Identifier);
    }

    [Fact]
    public void Parse_FunctionReturningSequence_IsValid()
    {
        var program = Parse(
            """
            sequence<sample> : compose obtener() >
                ret [1, 2, 3];
            <

            session >
            <
            """
        );

        var function = Assert.Single(program.Functions);

        Assert.Equal("sequence<sample>", function.ReturnType);
    }

    [Fact]
    public void Parse_CompositeTypeMissingClosingGreaterThan_ThrowsParserException()
    {
        const string source =
            """
            session >
                sequence<sample numeros = [1, 2, 3];
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_CollectionMissingClosingBracket_ThrowsParserException()
    {
        const string source =
            """
            session >
                sequence<sample> numeros = [1, 2, 3;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_IndexedAssignment_CreatesIndexedAssignmentNode()
    {
        var program = Parse(
            """
            session >
                numeros[0] = 10;
            <
            """
        );

        var assignment = Assert.IsType<IndexedAssignmentStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        Assert.NotNull(assignment.Target);

        var value = Assert.IsType<LiteralExpressionNode>(
            assignment.Value);

        Assert.Equal("10", value.Value);
    }

    [Fact]
    public void Parse_DoubleIndexedAssignment_CreatesNestedIndexTarget()
    {
        var program = Parse(
            """
            session >
                matriz[1][2] = 20;
            <
            """
        );

        var assignment = Assert.IsType<IndexedAssignmentStatementNode>(
            Assert.Single(program.Session.Body.Statements));

        var outer = assignment.Target;

        Assert.IsType<IndexExpressionNode>(
            outer.Target);

        var value = Assert.IsType<LiteralExpressionNode>(
            assignment.Value);

        Assert.Equal("20", value.Value);
    }

    [Fact]
    public void Parse_LengthExpression_CreatesLengthNode()
    {
        var program = Parse(
            """
            session >
                sample cantidad = numeros.length();
            <
            """
        );

        var variable = Assert.IsType<VariableDeclarationNode>(
            Assert.Single(program.Session.Body.Statements));

        var length = Assert.IsType<LengthExpressionNode>(
            variable.Initializer);

        var target = Assert.IsType<IdentifierExpressionNode>(
            length.Target);

        Assert.Equal("numeros", target.Name);
    }

    [Fact]
    public void Parse_LengthExpressionInsideRepeat_IsValid()
    {
        var program = Parse(
            """
            session >
                sample i = 0;

                repeat [i < numeros.length()] >
                    i = i + 1;
                <
            <
            """
        );

        Assert.Equal(2, program.Session.Body.Statements.Count);

        var repeat = Assert.IsType<RepeatStatementNode>(
            program.Session.Body.Statements[1]);

        Assert.NotNull(repeat.Condition);
    }

    [Fact]
    public void Parse_UnknownMemberAfterDot_ThrowsParserException()
    {
        const string source =
            """
            session >
                sample x = numeros.size();
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_IndexedAssignmentMissingAssign_ThrowsParserException()
    {
        const string source =
            """
            session >
                numeros[0] 10;
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_Import_CreatesImportNode()
    {
        var program = Parse(
            """
            import "utilidades.tm";

            session >
            <
            """
        );

        var import = Assert.Single(program.Imports);

        Assert.Equal("utilidades.tm", import.FilePath);
    }

    [Fact]
    public void Parse_MultipleImports_CreatesAllImports()
    {
        var program = Parse(
            """
            import "matematicas.tm";
            import "utilidades.tm";

            session >
            <
            """
        );

        Assert.Equal(2, program.Imports.Count);
    }

    [Fact]
    public void Parse_ImportsFunctionsAndSession_CreatesCompleteProgram()
    {
        var program = Parse(
            """
            import "utilidades.tm";

            sample : compose sumar(sample a, sample b) >
                ret a + b;
            <

            session >
                sample resultado = sumar(2, 3);
                output >> resultado;
            <
            """
        );

        Assert.Single(program.Imports);
        Assert.Single(program.Functions);
        Assert.Equal(2, program.Session.Body.Statements.Count);
    }

    [Fact]
    public void Parse_ImportMissingFilePath_ThrowsParserException()
    {
        const string source =
            """
            import;

            session >
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_ImportMissingSemicolon_ThrowsParserException()
    {
        const string source =
            """
            import "utilidades.tm"

            session >
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_ImportAfterFunction_ThrowsParserException()
    {
        const string source =
            """
            sample : compose ejemplo() >
                ret 1;
            <

            import "utilidades.tm";

            session >
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_IncrementStatement_CreatesUpdateNode()
    {
        var program = Parse(
            """
            session >
                sample contador = 0;
                contador++;
            <
            """
        );

        var update = Assert.IsType<UpdateStatementNode>(
            program.Session.Body.Statements[1]);

        Assert.Equal("contador", update.Identifier);
        Assert.Equal(TokenType.Increment, update.Operator);
    }

    [Fact]
    public void Parse_DecrementStatement_CreatesUpdateNode()
    {
        var program = Parse(
            """
            session >
                sample contador = 10;
                contador--;
            <
            """
        );

        var update = Assert.IsType<UpdateStatementNode>(
            program.Session.Body.Statements[1]);

        Assert.Equal(TokenType.Decrement, update.Operator);
    }

    [Fact]
    public void Parse_RepeatWithIncrementStatement_IsValid()
    {
        var program = Parse(
            """
            session >
                sample contador = 0;

                repeat [contador < 5] >
                    contador++;
                <
            <
            """
        );

        var repeat = Assert.IsType<RepeatStatementNode>(
            program.Session.Body.Statements[1]);

        Assert.Single(repeat.Body.Statements);
        Assert.IsType<UpdateStatementNode>(
            repeat.Body.Statements[0]);
    }

    [Fact]
    public void Parse_ModuleWithoutSession_IsValid()
    {
        var program = Parse(
            """
            sample : compose sumar(sample a, sample b) >
                ret a + b;
            <
            """
        );

        Assert.Single(program.Functions);
        Assert.Null(program.Session);
    }

    [Fact]
    public void Parse_ModuleWithImportAndFunctionWithoutSession_IsValid()
    {
        var program = Parse(
            """
            import "utilidades.tm";

            sample : compose doble(sample x) >
                ret x * 2;
            <
            """
        );

        Assert.Single(program.Imports);
        Assert.Single(program.Functions);
        Assert.Null(program.Session);
    }

    [Fact]
    public void Parse_MultipleSessions_ThrowsParserException()
    {
        const string source =
            """
            session >
            <

            session >
            <
            """;

        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void Parse_CompleteIntegratedProgram_IsValid()
    {
        var program = Parse(
            """
            import "utilidades.tm";

            sample : compose sumarLista(sequence<sample> numeros) >
                sample total = 0;
                sample i = 0;

                repeat [i < numeros.length()] >
                    total = total + numeros[i];
                    i++;
                <

                ret total;
            <

            session >
                sequence<sample> valores = [10, 20, 30, 40];

                sample suma = sumarLista(valores);

                output >> "Suma: " + suma;

                score<sample> matriz = [
                    [1, 2],
                    [3, 4]
                ];

                matriz[0][1] = 9;

                if [suma > 50] >
                    output >> "Suma grande";
                <
                else >
                    output >> "Suma pequena";
                <

                mix suma >
                    case 100: >
                        output >> "Cien";
                    <
                    default: >
                        output >> "Otro valor";
                    <
                <
            <
            """
        );

        Assert.Single(program.Imports);
        Assert.Single(program.Functions);
        Assert.NotNull(program.Session);
        Assert.True(program.Session.Body.Statements.Count > 0);
    }

    [Fact]
    public void Parse_CompleteRecursiveProgram_IsValid()
    {
        var program = Parse(
            """
            sample : compose factorial(sample n) >
                if [n <= 1] >
                    ret 1;
                <

                ret n * factorial(n - 1);
            <

            session >
                sample resultado = factorial(5);
                output >> "Factorial: " + resultado;
            <
            """
        );

        var function = Assert.Single(program.Functions);

        Assert.Equal("factorial", function.Identifier);
        Assert.NotNull(program.Session);
        Assert.Equal(2, program.Session.Body.Statements.Count);
    }

    [Fact]
    public void Parse_CompleteModuleWithoutSession_IsValid()
    {
        var program = Parse(
            """
            import "base.tm";

            sequence<sample> : compose duplicar(sequence<sample> numeros) >
                sample i = 0;

                repeat [i < numeros.length()] >
                    numeros[i] = numeros[i] * 2;
                    i++;
                <

                ret numeros;
            <
            """
        );

        Assert.Single(program.Imports);
        Assert.Single(program.Functions);
        Assert.Null(program.Session);
    }
}