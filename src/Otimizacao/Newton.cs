namespace Conde.Otimizacao;

/// <summary>
/// O metodo de NEWTON: usar a curvatura, e nao so a inclinacao.
///
/// A descida de gradiente aproxima a funcao por um plano e anda na direcao em
/// que ele desce. Newton aproxima por uma PARABOLA, em todas as direcoes ao
/// mesmo tempo, e vai direto para o fundo dela.
///
/// Numa quadratica a parabola nao e aproximacao: e a propria funcao. Entao
/// Newton acha o minimo em UM passo, de qualquer ponto de partida, com qualquer
/// condicionamento. O numero que domina a descida de gradiente simplesmente nao
/// aparece aqui, e a medida mostra isso com k variando por seis ordens de
/// grandeza.
///
/// O preco e a hessiana: resolver o sistema custa n^3, enquanto um passo de
/// gradiente custa n. Para n grande isso inverte a conta, e e por isso que os
/// metodos do meio do caminho, que aproximam a curvatura em vez de calcular,
/// existem.
/// </summary>
public static class Newton
{
    /// <summary>
    /// O passo puro: resolve H*d = -g e anda d inteiro.
    ///
    /// Sem busca de linha nenhuma. Numa quadratica isso basta e e exato; numa
    /// funcao qualquer, longe do minimo, isso as vezes anda demais, e a medida
    /// mostra quando.
    /// </summary>
    public static Caminhada Puro(Funcao funcao, double[] inicio, double tolerancia = 1e-8, int limite = 1000)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(inicio);

        if (funcao.Hessiana is null) throw new ArgumentException("esta funcao nao tem hessiana", nameof(funcao));

        var x = Vetor.Copia(inicio);
        long gradientes = 0;

        for (var iteracao = 0; iteracao < limite; iteracao++)
        {
            var g = funcao.Gradiente(x);
            gradientes++;

            if (Vetor.Norma(g) <= tolerancia)
                return new Caminhada(x, funcao.Valor(x), iteracao, 1, gradientes, true, funcao.Distancia(x));

            var direcao = Vetor.Resolver(funcao.Hessiana(x), Vetor.Vezes(-1, g));
            if (direcao is null)
                return new Caminhada(x, funcao.Valor(x), iteracao, 1, gradientes, false, funcao.Distancia(x));

            x = Vetor.Mais(x, direcao);

            if (double.IsNaN(x[0]) || Math.Abs(x[0]) > 1e100)
                return new Caminhada(x, double.PositiveInfinity, iteracao, 0, gradientes, false, double.PositiveInfinity);
        }

        return new Caminhada(x, funcao.Valor(x), limite, 1, gradientes, false, funcao.Distancia(x));
    }

    /// <summary>
    /// Newton com busca de linha, que e a versao que nao quebra longe do
    /// minimo.
    ///
    /// A direcao continua sendo a de Newton; so o tamanho do passo passa a ser
    /// conferido. Perto do minimo a busca aceita o passo inteiro na primeira
    /// tentativa, entao a convergencia quadratica fica intacta.
    /// </summary>
    public static Caminhada ComBusca(Funcao funcao, double[] inicio, double tolerancia = 1e-8, int limite = 1000)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(inicio);

        if (funcao.Hessiana is null) throw new ArgumentException("esta funcao nao tem hessiana", nameof(funcao));

        var x = Vetor.Copia(inicio);
        long avaliacoes = 0, gradientes = 0;

        for (var iteracao = 0; iteracao < limite; iteracao++)
        {
            var g = funcao.Gradiente(x);
            gradientes++;

            if (Vetor.Norma(g) <= tolerancia)
                return new Caminhada(x, funcao.Valor(x), iteracao, avaliacoes, gradientes, true, funcao.Distancia(x));

            var direcao = Vetor.Resolver(funcao.Hessiana(x), Vetor.Vezes(-1, g));

            // Quando a hessiana nao e definida positiva, a direcao de Newton
            // pode apontar para CIMA: ela vai para um ponto critico, e nem todo
            // ponto critico e minimo. Cair no gradiente nesse caso e o conserto
            // mais simples que existe e e o que todo metodo de verdade faz.
            if (direcao is null || Vetor.Interno(g, direcao) >= 0) direcao = Vetor.Vezes(-1, g);

            var (passo, custo) = Busca.Armijo(funcao, x, direcao, g);
            avaliacoes += custo;

            if (passo <= 0)
                return new Caminhada(x, funcao.Valor(x), iteracao, avaliacoes, gradientes, false, funcao.Distancia(x));

            x = Vetor.Anda(x, passo, direcao);
        }

        return new Caminhada(x, funcao.Valor(x), limite, avaliacoes, gradientes, false, funcao.Distancia(x));
    }

    /// <summary>
    /// O expoente da convergencia, medido: quando o erro novo e proporcional ao
    /// erro velho ELEVADO A p, o logaritmo de um contra o logaritmo do outro da
    /// p.
    ///
    /// Para a descida de gradiente isso da um, que e a convergencia linear: o
    /// numero de digitos certos cresce por uma quantidade fixa a cada iteracao.
    /// Para Newton isso da dois, que e a convergencia quadratica: o numero de
    /// digitos certos DOBRA a cada iteracao.
    /// </summary>
    public static double ExpoenteMedido(IReadOnlyList<double> erros)
    {
        ArgumentNullException.ThrowIfNull(erros);

        var expoentes = new List<double>();

        for (var i = 1; i < erros.Count; i++)
        {
            if (erros[i - 1] <= 0 || erros[i] <= 0) continue;
            if (erros[i - 1] >= 1 || erros[i] >= 1) continue;
            if (erros[i] < 1e-14) break;

            expoentes.Add(Math.Log(erros[i]) / Math.Log(erros[i - 1]));
        }

        return expoentes.Count == 0 ? double.NaN : expoentes[^1];
    }

    /// <summary>A trilha de erros de Newton, para medir o expoente.</summary>
    public static List<double> Erros(Funcao funcao, double[] inicio, int iteracoes = 12)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        if (funcao.Hessiana is null) throw new ArgumentException("esta funcao nao tem hessiana", nameof(funcao));

        var x = Vetor.Copia(inicio);
        var erros = new List<double> { funcao.Distancia(x) };

        for (var i = 0; i < iteracoes; i++)
        {
            var direcao = Vetor.Resolver(funcao.Hessiana(x), Vetor.Vezes(-1, funcao.Gradiente(x)));
            if (direcao is null) break;

            x = Vetor.Mais(x, direcao);
            erros.Add(funcao.Distancia(x));
        }

        return erros;
    }
}
