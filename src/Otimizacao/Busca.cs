namespace Conde.Otimizacao;

/// <summary>
/// A BUSCA DE LINHA: decidir o tamanho do passo olhando a funcao em vez de
/// chutando.
///
/// Passo fixo exige saber a maior curvatura da funcao, e quase nunca se sabe.
/// Pior: na Rosenbrock a curvatura muda de ponto para ponto, entao nao existe um
/// passo fixo bom para o caminho inteiro.
///
/// A condicao de ARMIJO troca a pergunta. Em vez de "qual passo?", ela pergunta
/// "este passo desceu o bastante?", e a resposta e uma comparacao: a queda tem
/// que ser pelo menos uma fracao da queda que a inclinacao prometia. Enquanto
/// nao for, corta o passo pela metade.
/// </summary>
public static class Busca
{
    /// <summary>
    /// O passo de Armijo, achado cortando pela metade a partir de um chute.
    ///
    /// Devolve o passo e quantas avaliacoes de f ele custou, porque esse custo e
    /// o preco da busca e ele entra na comparacao com o passo fixo.
    /// </summary>
    public static (double Passo, int Avaliacoes) Armijo(Funcao funcao, double[] ponto, double[] direcao,
                                                        double[] gradiente, double chute = 1.0,
                                                        double fracao = 1e-4, int cortes = 60)
    {
        ArgumentNullException.ThrowIfNull(funcao);

        var aqui = funcao.Valor(ponto);
        var inclinacao = Vetor.Interno(gradiente, direcao);
        var avaliacoes = 1;

        // Se a direcao nao desce, nenhuma busca salva: isso e erro de quem
        // escolheu a direcao, e devolver zero deixa o erro aparecer no lugar
        // certo em vez de virar um passo gigante para cima.
        if (inclinacao >= 0) return (0, avaliacoes);

        var passo = chute;

        for (var corte = 0; corte < cortes; corte++)
        {
            var adiante = Vetor.Anda(ponto, passo, direcao);
            var la = funcao.Valor(adiante);
            avaliacoes++;

            if (la <= aqui + fracao * passo * inclinacao) return (passo, avaliacoes);

            passo /= 2;
        }

        return (passo, avaliacoes);
    }

    /// <summary>
    /// Descida de gradiente com busca de linha, que e a versao que funciona sem
    /// saber nada sobre a funcao.
    /// </summary>
    public static Caminhada Descida(Funcao funcao, double[] inicio, double tolerancia = 1e-8,
                                    int limite = 200_000)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(inicio);

        var x = Vetor.Copia(inicio);
        long avaliacoes = 0, gradientes = 0;
        var chute = 1.0;

        for (var iteracao = 0; iteracao < limite; iteracao++)
        {
            var g = funcao.Gradiente(x);
            gradientes++;

            if (Vetor.Norma(g) <= tolerancia)
                return new Caminhada(x, funcao.Valor(x), iteracao, avaliacoes, gradientes, true, funcao.Distancia(x));

            var (passo, custo) = Armijo(funcao, x, Vetor.Vezes(-1, g), g, chute);
            avaliacoes += custo;

            if (passo <= 0)
                return new Caminhada(x, funcao.Valor(x), iteracao, avaliacoes, gradientes, false, funcao.Distancia(x));

            x = Vetor.Anda(x, passo, Vetor.Vezes(-1, g));

            // Comecar a proxima busca um pouco acima do passo que funcionou
            // evita cortar do um toda vez. Sem isso a busca gasta o mesmo
            // numero de cortes em todas as iteracoes, e o custo dela dobra.
            chute = passo * 2;
        }

        return new Caminhada(x, funcao.Valor(x), limite, avaliacoes, gradientes, false, funcao.Distancia(x));
    }

    /// <summary>
    /// O maior passo em que a descida fixa ainda converge, achado por bissecao.
    ///
    /// A teoria diz 2/L. A medida nao sabe disso: ela so roda e ve quem explode.
    /// </summary>
    public static double MaiorPassoQueConverge(Funcao funcao, double[] inicio, double limite = 10)
    {
        ArgumentNullException.ThrowIfNull(funcao);

        if (Converge(funcao, inicio, limite)) return double.PositiveInfinity;

        double seguro = 0, perigoso = limite;

        for (var volta = 0; volta < 60; volta++)
        {
            var meio = (seguro + perigoso) / 2;
            if (Converge(funcao, inicio, meio)) seguro = meio;
            else perigoso = meio;
        }

        return seguro;
    }

    private static bool Converge(Funcao funcao, double[] inicio, double passo)
    {
        var x = Vetor.Copia(inicio);
        var comecou = funcao.Distancia(x);

        for (var i = 0; i < 2000; i++)
        {
            x = Vetor.Anda(x, -passo, funcao.Gradiente(x));
            if (double.IsNaN(x[0]) || double.IsInfinity(x[0])) return false;
        }

        return funcao.Distancia(x) < comecou;
    }
}
