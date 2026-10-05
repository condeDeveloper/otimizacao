namespace Conde.Otimizacao;

/// <summary>
/// NELDER e MEAD, 1965: minimizar SEM DERIVADA NENHUMA.
///
/// O metodo carrega n+1 pontos, um simplex, e so sabe comparar valores da
/// funcao. A cada rodada ele joga o pior ponto para o outro lado do centro dos
/// demais, e conforme o resultado ele estica, encolhe ou contrai o simplex
/// inteiro. Nao existe gradiente, nem curvatura, nem passo: existe comparacao.
///
/// Ele esta aqui como contraste honesto. Quando a derivada existe, usar a
/// derivada ganha de longe, e a medida diz por quanto. Quando a derivada NAO
/// existe, que e o caso de funcao vinda de simulacao ou de medicao, isto e o que
/// sobra, e funciona.
/// </summary>
public static class Nelder
{
    /// <summary>
    /// O simplex inicial: o ponto dado e mais um deslocamento em cada eixo.
    ///
    /// O deslocamento e proporcional a coordenada quando ela nao e zero, o que
    /// mantem o simplex com o tamanho da escala do problema em vez de um tamanho
    /// arbitrario.
    /// </summary>
    public static double[][] SimplexInicial(double[] inicio, double tamanho = 0.1)
    {
        ArgumentNullException.ThrowIfNull(inicio);

        var n = inicio.Length;
        var pontos = new double[n + 1][];
        pontos[0] = Vetor.Copia(inicio);

        for (var i = 0; i < n; i++)
        {
            var ponto = Vetor.Copia(inicio);
            ponto[i] += Math.Abs(ponto[i]) > 1e-12 ? tamanho * ponto[i] : tamanho;
            pontos[i + 1] = ponto;
        }

        return pontos;
    }

    /// <summary>
    /// A caminhada do simplex. O criterio de parada e o TAMANHO do simplex, e
    /// nao a norma do gradiente, porque gradiente e justamente o que ele nao
    /// tem.
    /// </summary>
    public static Caminhada Minimizar(Funcao funcao, double[] inicio, double tolerancia = 1e-10,
                                      int limite = 100_000)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(inicio);

        var n = inicio.Length;
        var pontos = SimplexInicial(inicio);
        var valores = new double[n + 1];
        long avaliacoes = 0;

        for (var i = 0; i <= n; i++) { valores[i] = funcao.Valor(pontos[i]); avaliacoes++; }

        for (var rodada = 0; rodada < limite; rodada++)
        {
            Ordenar(pontos, valores);

            if (Tamanho(pontos) <= tolerancia)
                return new Caminhada(pontos[0], valores[0], rodada, avaliacoes, 0, true, funcao.Distancia(pontos[0]));

            // O centro dos melhores, sem contar o pior: e para longe dele que o
            // pior ponto e jogado.
            var centro = Vetor.Zeros(n);
            for (var i = 0; i < n; i++) centro = Vetor.Mais(centro, pontos[i]);
            centro = Vetor.Vezes(1.0 / n, centro);

            var direcao = Vetor.Menos(centro, pontos[n]);

            var refletido = Vetor.Anda(centro, 1, direcao);
            var valorRefletido = funcao.Valor(refletido);
            avaliacoes++;

            if (valorRefletido < valores[0])
            {
                // Deu melhor que todos: vale a pena tentar ir mais longe ainda.
                var esticado = Vetor.Anda(centro, 2, direcao);
                var valorEsticado = funcao.Valor(esticado);
                avaliacoes++;

                if (valorEsticado < valorRefletido) { pontos[n] = esticado; valores[n] = valorEsticado; }
                else { pontos[n] = refletido; valores[n] = valorRefletido; }
                continue;
            }

            if (valorRefletido < valores[n - 1])
            {
                pontos[n] = refletido;
                valores[n] = valorRefletido;
                continue;
            }

            var contraido = Vetor.Anda(centro, -0.5, direcao);
            var valorContraido = funcao.Valor(contraido);
            avaliacoes++;

            if (valorContraido < valores[n])
            {
                pontos[n] = contraido;
                valores[n] = valorContraido;
                continue;
            }

            // Nada funcionou: encolhe o simplex inteiro na direcao do melhor
            // ponto. E a rodada mais cara, e ela e rara.
            for (var i = 1; i <= n; i++)
            {
                pontos[i] = Vetor.Vezes(0.5, Vetor.Mais(pontos[i], pontos[0]));
                valores[i] = funcao.Valor(pontos[i]);
                avaliacoes++;
            }
        }

        Ordenar(pontos, valores);
        return new Caminhada(pontos[0], valores[0], limite, avaliacoes, 0, false, funcao.Distancia(pontos[0]));
    }

    /// <summary>A maior distancia entre o melhor ponto e os outros.</summary>
    public static double Tamanho(double[][] pontos)
    {
        ArgumentNullException.ThrowIfNull(pontos);

        var maior = 0.0;
        for (var i = 1; i < pontos.Length; i++)
            maior = Math.Max(maior, Vetor.Norma(Vetor.Menos(pontos[i], pontos[0])));

        return maior;
    }

    private static void Ordenar(double[][] pontos, double[] valores)
    {
        // Insercao: sao n+1 pontos e quase sempre so um saiu do lugar.
        for (var i = 1; i < valores.Length; i++)
        {
            var valor = valores[i];
            var ponto = pontos[i];
            var j = i - 1;

            while (j >= 0 && valores[j] > valor)
            {
                valores[j + 1] = valores[j];
                pontos[j + 1] = pontos[j];
                j--;
            }

            valores[j + 1] = valor;
            pontos[j + 1] = ponto;
        }
    }
}
