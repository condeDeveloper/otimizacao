namespace Conde.Otimizacao;

/// <summary>
/// A CONFERENCIA DO GRADIENTE por diferencas finitas.
///
/// Gradiente errado e o erro mais caro que existe em otimizacao, e o mais
/// dificil de notar: o metodo continua rodando, continua descendo alguma coisa e
/// continua parando num ponto. Ele so para no lugar errado, e o lugar errado
/// tem a mesma cara do certo.
///
/// A conferencia e simples e tem uma armadilha instrutiva. A diferenca finita
/// tem dois erros que brigam: o de truncamento, que cai com o passo, e o de
/// arredondamento, que CRESCE quando o passo encolhe, porque subtrair dois
/// numeros quase iguais joga fora os digitos que importam. O melhor passo nao e
/// o menor passo, e a medida mostra onde ele fica.
/// </summary>
public static class Gradiente
{
    /// <summary>A derivada parcial pela frente: (f(x + h) - f(x)) / h.</summary>
    public static double PelaFrente(Funcao funcao, double[] ponto, int i, double passo)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(ponto);

        var adiante = Vetor.Copia(ponto);
        adiante[i] += passo;

        return (funcao.Valor(adiante) - funcao.Valor(ponto)) / passo;
    }

    /// <summary>
    /// A derivada parcial centrada: (f(x + h) - f(x - h)) / (2h).
    ///
    /// Ela custa uma avaliacao a mais e o erro de truncamento cai com h ao
    /// QUADRADO em vez de com h, porque os termos de ordem impar se cancelam
    /// entre os dois lados. E uma troca boa e quase sempre vale a pena.
    /// </summary>
    public static double Centrada(Funcao funcao, double[] ponto, int i, double passo)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(ponto);

        var adiante = Vetor.Copia(ponto);
        var atras = Vetor.Copia(ponto);
        adiante[i] += passo;
        atras[i] -= passo;

        return (funcao.Valor(adiante) - funcao.Valor(atras)) / (2 * passo);
    }

    /// <summary>
    /// O maior erro relativo entre o gradiente analitico e o numerico, que e o
    /// numero que diz se o gradiente escrito a mao esta certo.
    /// </summary>
    public static double Conferir(Funcao funcao, double[] ponto, double passo = 1e-5, bool centrada = true)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(ponto);

        var analitico = funcao.Gradiente(ponto);
        var pior = 0.0;

        for (var i = 0; i < ponto.Length; i++)
        {
            var numerico = centrada ? Centrada(funcao, ponto, i, passo) : PelaFrente(funcao, ponto, i, passo);
            var fundo = Math.Max(1e-12, Math.Abs(analitico[i]));
            pior = Math.Max(pior, Math.Abs(numerico - analitico[i]) / fundo);
        }

        return pior;
    }

    /// <summary>
    /// O passo que minimiza o erro da conferencia, procurado em vez de citado.
    ///
    /// A teoria diz que o melhor passo da centrada fica perto da raiz CUBICA da
    /// precisao da maquina, uns 6e-6, e o da progressiva perto da raiz
    /// quadrada, uns 1e-8. A medida devolve o numero que realmente acontece.
    /// </summary>
    public static (double Passo, double Erro) MelhorPasso(Funcao funcao, double[] ponto, bool centrada = true)
    {
        var melhorPasso = 1.0;
        var melhorErro = double.PositiveInfinity;

        for (var expoente = -1; expoente >= -14; expoente--)
        {
            var passo = Math.Pow(10, expoente);
            var erro = Conferir(funcao, ponto, passo, centrada);

            if (erro < melhorErro)
            {
                melhorErro = erro;
                melhorPasso = passo;
            }
        }

        return (melhorPasso, melhorErro);
    }
}
