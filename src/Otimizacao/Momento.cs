namespace Conde.Otimizacao;

/// <summary>
/// O MOMENTO, de Polyak, 1964: lembrar da direcao anterior.
///
/// A descida de gradiente ziguezagueia num vale comprido porque cada passo
/// esquece o anterior: ela corrige para um lado, depois para o outro, e as
/// correcoes se cancelam enquanto o avanco pelo fundo do vale e minusculo.
///
/// O momento soma uma fracao do passo anterior ao passo de agora. As correcoes
/// que se alternam continuam se cancelando, e o avanco pelo fundo, que aponta
/// sempre para o mesmo lado, se ACUMULA. A ideia e essa, e a conta e uma linha.
///
/// O que ela compra tem nome e numero: o custo deixa de crescer com o
/// condicionamento e passa a crescer com a RAIZ dele. Com k igual a dez mil,
/// isso e a diferenca entre dez mil iteracoes e cem.
/// </summary>
public static class Momento
{
    /// <summary>
    /// Os dois parametros que a teoria da para a quadratica: o momento e
    /// ((raiz de k - 1)/(raiz de k + 1))^2 e o passo e (1 + momento)/L.
    /// </summary>
    public static (double Passo, double Peso) Parametros(double menorCurvatura, double maiorCurvatura)
    {
        var k = maiorCurvatura / menorCurvatura;
        var raiz = Math.Sqrt(k);
        var peso = (raiz - 1) / (raiz + 1);
        peso *= peso;

        return ((1 + peso) / maiorCurvatura, peso);
    }

    /// <summary>
    /// Os parametros do Nesterov, que NAO sao os mesmos da bola pesada.
    ///
    /// Isso me custou uma medida inteira: rodar o Nesterov com os parametros da
    /// bola pesada nao converge, e o limite de dois milhoes de iteracoes bateu
    /// em todas as linhas da tabela. O peso dele e a raiz de k, sem o quadrado,
    /// e o passo e 1/L, sem o (1 + peso).
    /// </summary>
    public static (double Passo, double Peso) ParametrosNesterov(double menorCurvatura, double maiorCurvatura)
    {
        var raiz = Math.Sqrt(maiorCurvatura / menorCurvatura);
        return (1 / maiorCurvatura, (raiz - 1) / (raiz + 1));
    }

    /// <summary>
    /// A BOLA PESADA: o passo de agora mais uma fracao do passo anterior.
    ///
    /// O nome vem da imagem certa: uma bola pesada descendo o vale nao muda de
    /// direcao a cada esbarrada na parede, ela segue pelo fundo.
    /// </summary>
    public static Caminhada BolaPesada(Funcao funcao, double[] inicio, double passo, double peso,
                                       double tolerancia = 1e-8, int limite = 2_000_000)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(inicio);

        var x = Vetor.Copia(inicio);
        var anterior = Vetor.Zeros(x.Length);
        long gradientes = 0;

        for (var iteracao = 0; iteracao < limite; iteracao++)
        {
            var g = funcao.Gradiente(x);
            gradientes++;

            if (Vetor.Norma(g) <= tolerancia)
                return new Caminhada(x, funcao.Valor(x), iteracao, 1, gradientes, true, funcao.Distancia(x));

            var movimento = Vetor.Menos(Vetor.Vezes(peso, anterior), Vetor.Vezes(passo, g));
            x = Vetor.Mais(x, movimento);
            anterior = movimento;

            if (double.IsNaN(x[0]) || Math.Abs(x[0]) > 1e100)
                return new Caminhada(x, double.PositiveInfinity, iteracao, 0, gradientes, false, double.PositiveInfinity);
        }

        return new Caminhada(x, funcao.Valor(x), limite, 1, gradientes, false, funcao.Distancia(x));
    }

    /// <summary>
    /// NESTEROV, 1983: olhar o gradiente ONDE O MOMENTO VAI CAIR, e nao onde se
    /// esta agora.
    ///
    /// A diferenca para a bola pesada e so o ponto em que o gradiente e
    /// avaliado, e ela e o que transforma um metodo que as vezes oscila num
    /// metodo com garantia. O momento ja sabe para onde vai levar; medir a
    /// inclinacao la e usar informacao que ja estava na mao.
    /// </summary>
    public static Caminhada Nesterov(Funcao funcao, double[] inicio, double passo, double peso,
                                     double tolerancia = 1e-8, int limite = 2_000_000)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(inicio);

        var x = Vetor.Copia(inicio);
        var anterior = Vetor.Zeros(x.Length);
        long gradientes = 0;

        for (var iteracao = 0; iteracao < limite; iteracao++)
        {
            if (Vetor.Norma(funcao.Gradiente(x)) <= tolerancia)
                return new Caminhada(x, funcao.Valor(x), iteracao, 1, gradientes + 1, true, funcao.Distancia(x));

            var espiada = Vetor.Anda(x, peso, anterior);
            var g = funcao.Gradiente(espiada);
            gradientes += 2;

            var movimento = Vetor.Menos(Vetor.Vezes(peso, anterior), Vetor.Vezes(passo, g));
            x = Vetor.Mais(x, movimento);
            anterior = movimento;

            if (double.IsNaN(x[0]) || Math.Abs(x[0]) > 1e100)
                return new Caminhada(x, double.PositiveInfinity, iteracao, 0, gradientes, false, double.PositiveInfinity);
        }

        return new Caminhada(x, funcao.Valor(x), limite, 1, gradientes, false, funcao.Distancia(x));
    }

    /// <summary>
    /// Quantas iteracoes a bola pesada precisa para encolher a distancia por um
    /// fator dado, para comparar com a descida sem momento.
    /// </summary>
    public static int IteracoesPara(Funcao funcao, double[] inicio, double passo, double peso,
                                    double fator, int limite = 2_000_000)
    {
        ArgumentNullException.ThrowIfNull(funcao);

        var x = Vetor.Copia(inicio);
        var anterior = Vetor.Zeros(x.Length);
        var alvo = funcao.Distancia(inicio) / fator;

        for (var i = 0; i < limite; i++)
        {
            if (funcao.Distancia(x) <= alvo) return i;

            var movimento = Vetor.Menos(Vetor.Vezes(peso, anterior), Vetor.Vezes(passo, funcao.Gradiente(x)));
            x = Vetor.Mais(x, movimento);
            anterior = movimento;

            if (double.IsNaN(x[0]) || Math.Abs(x[0]) > 1e100) return limite;
        }

        return limite;
    }
}
