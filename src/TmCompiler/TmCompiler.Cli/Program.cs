using TmCompiler.Application.Lexing;
using TmCompiler.Domain.Lexical;
using TmCompiler.Infrastructure;

// Arnés de consola temporal para probar el pipeline del compilador de forma
// aislada, sin depender de TmIde. 

// Placeholder: hoy solo ejecuta el Lexer;
// se ampliará con Parser y análisis semántico a medida que existan.
if (args.Length == 0)
{
    Console.WriteLine("Uso: TmCompiler.Cli <archivo.tm>");
    return;
}

string path = args[0];
string source = File.ReadAllText(path);

ILexer lexer = new Lexer(source, path);
IEnumerable<Token> tokens = lexer.Tokenize();

Console.WriteLine(LexerDiagnosticReporter.GenerateTokenReport(tokens, path));
