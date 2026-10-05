namespace Conde.Otimizacao;

/// <summary>
/// O que uma caminhada devolve, com o custo junto.
///
/// O custo e contado em AVALIACOES da funcao e do gradiente, nunca em relogio.
/// Metodo de otimizacao e comparado por quanto da funcao ele precisa ver, e esse
/// numero e o mesmo em qualquer maquina.
/// </summary>
public sealed record Caminhada(double[] Ponto, double Valor, int Iteracoes,
                               long AvaliacoesDeF, long AvaliacoesDeGradiente,
                               bool Convergiu, double Distancia)
{
    /// <summary>O custo total, contando o gradiente como n avaliacoes de f.</summary>
    public long Custo(int dimensao) => AvaliacoesDeF + AvaliacoesDeGradiente * dimensao;
}

/// <summary>
/// A DESCIDA DE GRADIENTE, que e o metodo mais simples que existe: andar na
/// direcao em que a funcao cai mais depressa.
///
/// "Mais depressa" e local, e e por isso que ela e lenta. Numa funcao cujas
/// curvas de nivel sao elipses compridas, a direcao de descida mais ingreme
/// aponta quase perpendicular ao fundo do vale, e a caminhada ziguezagueia de
/// parede a parede em vez de andar para o minimo.
///
/// O numero que decide tudo e o CONDICIONAMENTO, a razao entre a maior e a menor
/// curvatura. Com o melhor passo possivel, o erro encolhe por (k-1)/(k+1) a cada
/// iteracao, entao o numero de iteracoes cresce PROPORCIONAL a k. Dobrar o
/// condicionamento dobra o trabalho, e nenhum ajuste de passo conserta isso.
/// </summary>
public static class Descida
{
    /// <summary>
    /// Descida com passo FIXO, que e a versao de livro.
    ///
    /// O passo precisa ser menor que 2/L, com L a maior curvatura, ou o metodo
    /// diverge: ele passa do fundo e sobe do outro lado mais do que desceu. A
    /// medida encontra esse limite sem saber dele.
    /// </summary>
    public static Caminhada Fixa(Funcao funcao, double[] inicio, double passo,
                                 double tolerancia = 1e-8, int limite = 2_000_000)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(inicio);

        var x = Vetor.Copia(inicio);
        long gradientes = 0;

        for (var iteracao = 0; iteracao < limite; iteracao++)
        {
            var g = funcao.Gradiente(x);
            gradientes++;

            if (Vetor.Norma(g) <= tolerancia)
                return new Caminhada(x, funcao.Valor(x), iteracao, 1, gradientes, true, funcao.Distancia(x));

            x = Vetor.Anda(x, -passo, g);

            if (double.IsNaN(x[0]) || Math.Abs(x[0]) > 1e100)
                return new Caminhada(x, double.PositiveInfinity, iteracao, 0, gradientes, false, double.PositiveInfinity);
        }

        return new Caminhada(x, funcao.Valor(x), limite, 1, gradientes, false, funcao.Distancia(x));
    }

    /// <summary>
    /// O PASSO OTIMO para uma quadratica: 2/(L + m), com L e m a maior e a menor
    /// curvatura.
    ///
    /// Ele nao e 1/L, que seria o palpite natural. O passo que minimiza o pior
    /// caso equilibra o encolhimento na direcao mais curva e na mais plana, e
    /// com ele os dois encolhem pela mesma razao (k-1)/(k+1).
    /// </summary>
    public static double PassoOtimo(double menorCurvatura, double maiorCurvatura) =>
        2 / (menorCurvatura + maiorCurvatura);

    /// <summary>
    /// A razao com que o erro encolhe a cada iteracao, no melhor caso possivel
    /// para a descida de gradiente.
    /// </summary>
    public static double RazaoTeorica(double condicionamento) =>
        (condicionamento - 1) / (condicionamento + 1);

    /// <summary>
    /// A razao MEDIDA: a media geometrica do encolhimento do erro nas ultimas
    /// iteracoes.
    ///
    /// Ela e medida longe do comeco de proposito. As primeiras iteracoes
    /// encolhem o erro mais depressa que a razao assintotica, porque a direcao
    /// do erro inicial ainda nao se alinhou com a pior direcao da funcao.
    /// </summary>
    public static double RazaoMedida(Funcao funcao, double[] inicio, double passo, int iteracoes = 400)
    {
        ArgumentNullException.ThrowIfNull(funcao);

        var x = Vetor.Copia(inicio);
        var metade = iteracoes / 2;
        var noMeio = 0.0;

        for (var i = 0; i < iteracoes; i++)
        {
            if (i == metade) noMeio = funcao.Distancia(x);
            x = Vetor.Anda(x, -passo, funcao.Gradiente(x));
        }

        var noFim = funcao.Distancia(x);
        if (noMeio <= 0 || noFim <= 0) return double.NaN;

        return Math.Pow(noFim / noMeio, 1.0 / (iteracoes - metade));
    }

    /// <summary>
    /// Quantas iteracoes a descida precisa para encolher a distancia por um
    /// fator dado, que e a medida que mostra o custo do condicionamento.
    /// </summary>
    public static int IteracoesPara(Funcao funcao, double[] inicio, double passo, double fator,
                                    int limite = 2_000_000)
    {
        ArgumentNullException.ThrowIfNull(funcao);

        var x = Vetor.Copia(inicio);
        var alvo = funcao.Distancia(inicio) / fator;

        for (var i = 0; i < limite; i++)
        {
            if (funcao.Distancia(x) <= alvo) return i;
            x = Vetor.Anda(x, -passo, funcao.Gradiente(x));

            if (double.IsNaN(x[0]) || Math.Abs(x[0]) > 1e100) return limite;
        }

        return limite;
    }
}
