namespace Tempo.Language.Evaluation;

/// <summary>
/// Destino de las ráfagas que lanza la sentencia <c>pulse</c> durante la
/// ejecución, cada una con una intensidad entre 0 y 100. El IDE las dibuja en el panel de partículas; la línea
/// de comandos usa <see cref="NullRuntimePulse"/>.
/// </summary>
public interface IRuntimePulse
{
    /// <summary>
    /// Lanza una ráfaga. El intérprete ya limitó <paramref name="intensity"/>
    /// al rango (0, 100]: con intensidad 0 no se llama.
    /// </summary>
    void Emit(double intensity);
}
