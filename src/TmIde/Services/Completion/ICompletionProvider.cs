using System.Collections.Generic;

namespace TmIde.Services.Completion;

/// <summary>
/// Una fuente de sugerencias (Strategy): el vocabulario del lenguaje, las
/// plantillas, los símbolos del proyecto. Devuelve sus candidatos sin filtrar
/// ni ordenar; de eso se encarga <see cref="CompletionEngine"/>.
/// </summary>
public interface ICompletionProvider
{
    IEnumerable<CompletionSuggestion> SuggestionsFor(CompletionContext context);
}
