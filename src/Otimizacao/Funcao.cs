namespace Conde.Otimizacao;

/// <summary>
/// Uma FUNCAO para minimizar, com o minimo conhecido junto.
///
/// Todo problema deste repositorio vem com a resposta certa, e isso nao e
/// conveniencia: e o juiz. Metodo de otimizacao erra de um jeito silencioso,
/// porque ele sempre devolve um ponto, e um ponto quase bom tem a mesma cara de
/// um ponto bom. Sem o minimo de verdade, "convergiu" quer dizer apenas "parou".
///
/// Com o minimo conhecido, a pergunta "quantas iteracoes este metodo precisa?"
/// vira numero, e a pergunta mais interessante, "como esse numero muda quando o
/// problema fica mal condicionado?", tambem.
/// </summary>
public sealed class Funcao
{
    public Funcao(string nome, Func<double[], double> valor, Func<double[], double[]> gradiente,
                  double[] otimo, double valorOtimo, Func<double[], double[,]>? hessiana = null,
                  double condicionamento = double.NaN)
    {
        Nome = nome;
        Valor = valor;
        Gradiente = gradiente;
        Otimo = otimo;
        ValorOtimo = valorOtimo;
        Hessiana = hessiana;
        Condicionamento = condicionamento;
    }

    public string Nome { get; }

    public Func<double[], double> Valor { get; }

    /// <summary>O gradiente analitico, que a medida confere contra diferencas finitas.</summary>
    public Func<double[], double[]> Gradiente { get; }

    /// <summary>O minimo de verdade, que e o juiz.</summary>
    public double[] Otimo { get; }

    public double ValorOtimo { get; }

    /// <summary>A hessiana, para o metodo de Newton. Nem toda funcao precisa dar a dela.</summary>
    public Func<double[], double[,]>? Hessiana { get; }

    /// <summary>
    /// A razao entre o maior e o menor autovalor da hessiana, quando ela e
    /// constante. E o unico numero que decide a velocidade da descida de
    /// gradiente, e o metodo de Newton nao enxerga ele.
    /// </summary>
    public double Condicionamento { get; }

    public int Dimensao => Otimo.Length;

    public override string ToString() => Nome;

    /// <summary>A distancia do ponto ate o minimo de verdade.</summary>
    public double Distancia(double[] ponto) => Vetor.Norma(Vetor.Menos(ponto, Otimo));

    /// <summary>
    /// Uma QUADRATICA com condicionamento escolhido: f(x) = soma de a_i x_i^2 / 2.
    ///
    /// Os coeficientes vao de um ate o condicionamento, espacados em escala
    /// geometrica, entao a hessiana e diagonal e os autovalores sao os proprios
    /// coeficientes. O minimo e a origem e vale zero, sem erro de arredondamento
    /// nenhum.
    ///
    /// Ela existe porque e o unico caso em que a teoria da um numero exato para
    /// comparar: a descida de gradiente com o melhor passo possivel encolhe o
    /// erro por (k-1)/(k+1) a cada iteracao, e nada mais rapido que isso.
    /// </summary>
    public static Funcao Quadratica(double condicionamento, int dimensao = 10)
    {
        var pesos = new double[dimensao];
        for (var i = 0; i < dimensao; i++)
            pesos[i] = dimensao == 1 ? 1 : Math.Pow(condicionamento, (double)i / (dimensao - 1));

        return new Funcao(
            $"quadratica k={condicionamento:N0}",
            x => { var soma = 0.0; for (var i = 0; i < dimensao; i++) soma += pesos[i] * x[i] * x[i] / 2; return soma; },
            x => { var g = new double[dimensao]; for (var i = 0; i < dimensao; i++) g[i] = pesos[i] * x[i]; return g; },
            new double[dimensao],
            0,
            _ => { var h = new double[dimensao, dimensao]; for (var i = 0; i < dimensao; i++) h[i, i] = pesos[i]; return h; },
            condicionamento);
    }

    /// <summary>
    /// A banana de ROSENBROCK, de 1960: f(x, y) = (1 - x)^2 + 100*(y - x^2)^2.
    ///
    /// O minimo e (1, 1) e vale zero, e ele fica no fundo de um vale curvo
    /// estreito. Ela existe porque a dificuldade dela nao e condicionamento
    /// constante: a curvatura MUDA de ponto para ponto, entao nenhum passo fixo
    /// serve para o caminho inteiro.
    /// </summary>
    public static Funcao Rosenbrock() => new(
        "Rosenbrock",
        x => (1 - x[0]) * (1 - x[0]) + 100 * (x[1] - x[0] * x[0]) * (x[1] - x[0] * x[0]),
        x => new[]
        {
            -2 * (1 - x[0]) - 400 * x[0] * (x[1] - x[0] * x[0]),
            200 * (x[1] - x[0] * x[0])
        },
        new[] { 1.0, 1.0 },
        0,
        x => new[,]
        {
            { 2 - 400 * (x[1] - 3 * x[0] * x[0]), -400 * x[0] },
            { -400 * x[0], 200.0 }
        });

    /// <summary>
    /// Uma quadratica em duas variaveis com os eixos TORTOS, para o caso
    /// diagonal nao ser o unico medido.
    ///
    /// f(x, y) = 0,5*(x^2 + 100*y^2) depois de girar quarenta e cinco graus. O
    /// condicionamento continua cem, e agora nenhuma coordenada sozinha aponta
    /// para o minimo.
    /// </summary>
    public static Funcao Torta()
    {
        // H = R^T D R com D = diag(1, 100) e R a rotacao de 45 graus.
        const double a = 50.5, b = -49.5;

        return new Funcao(
            "quadratica torta k=100",
            x => (a * x[0] * x[0] + 2 * b * x[0] * x[1] + a * x[1] * x[1]) / 2,
            x => new[] { a * x[0] + b * x[1], b * x[0] + a * x[1] },
            new[] { 0.0, 0.0 },
            0,
            _ => new[,] { { a, b }, { b, a } },
            100);
    }

    public static IEnumerable<Funcao> Todas()
    {
        yield return Quadratica(1);
        yield return Quadratica(100);
        yield return Torta();
        yield return Rosenbrock();
    }
}
