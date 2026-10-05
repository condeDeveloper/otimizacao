using Conde.Otimizacao;
using Xunit;

namespace Conde.Otimizacao.Testes;

public class NewtonTestes
{
    private static double[] Partida(int dimensao)
    {
        var x = new double[dimensao];
        for (var i = 0; i < dimensao; i++) x[i] = 1;
        return x;
    }

    /// <summary>
    /// O ACHADO: numa quadratica, Newton acha o minimo em UM passo, com qualquer
    /// condicionamento.
    ///
    /// A parabola que ele ajusta nao e aproximacao ali: e a propria funcao. O
    /// numero que domina a descida de gradiente simplesmente nao aparece.
    /// </summary>
    [Fact]
    public void NaQuadraticaNewtonResolveEmUmPasso()
    {
        foreach (var k in new[] { 1.0, 100.0, 10_000.0, 1_000_000.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var r = Newton.Puro(funcao, Partida(funcao.Dimensao));

            Assert.True(r.Convergiu);
            Assert.Equal(1, r.Iteracoes);
            Assert.True(r.Distancia < 1e-12, $"k={k} parou a {r.Distancia}");
        }
    }

    /// <summary>
    /// E tambem na quadratica TORTA, em que nenhuma coordenada sozinha aponta
    /// para o minimo. Newton nao olha coordenada: ele olha a curvatura inteira.
    /// </summary>
    [Fact]
    public void NaQuadraticaTortaTambemEhUmPasso()
    {
        var r = Newton.Puro(Funcao.Torta(), Vetor.De(3, -7));

        Assert.True(r.Convergiu);
        Assert.Equal(1, r.Iteracoes);
    }

    /// <summary>
    /// Na Rosenbrock a convergencia e QUADRATICA: o numero de digitos certos
    /// dobra a cada iteracao, e o expoente medido fica perto de dois.
    /// </summary>
    [Fact]
    public void NaRosenbrockOExpoenteMedidoFicaPertoDeDois()
    {
        var erros = Newton.Erros(Funcao.Rosenbrock(), Vetor.De(-1.2, 1));
        Assert.InRange(Newton.ExpoenteMedido(erros), 1.7, 2.5);
    }

    /// <summary>
    /// E a trilha de erros tem uma surpresa: Newton PURO piora nas primeiras
    /// iteracoes antes de desabar. Ele vai para o fundo da parabola ajustada, e
    /// longe do minimo essa parabola nao parece com a funcao.
    /// </summary>
    [Fact]
    public void NewtonPuroPioraAntesDeMelhorar()
    {
        var erros = Newton.Erros(Funcao.Rosenbrock(), Vetor.De(-1.2, 1));

        Assert.True(erros[2] > erros[0], $"a iteracao 2 deu {erros[2]} contra {erros[0]} da partida");
        Assert.True(erros[6] < 1e-9, $"a iteracao 6 deu {erros[6]}");
    }

    /// <summary>
    /// Mesmo assim ele converge, e em poucas iteracoes. A busca de linha e
    /// seguro, e seguro custa: nesta partida o Newton puro chega com menos
    /// iteracoes que o Newton com busca.
    /// </summary>
    [Fact]
    public void OSeguroDaBuscaCustaNestaPartida()
    {
        var funcao = Funcao.Rosenbrock();
        var inicio = Vetor.De(-1.2, 1);

        var puro = Newton.Puro(funcao, inicio);
        var comBusca = Newton.ComBusca(funcao, inicio);

        Assert.True(puro.Convergiu);
        Assert.True(comBusca.Convergiu);
        Assert.True(puro.Iteracoes < comBusca.Iteracoes);
    }

    /// <summary>
    /// E a diferenca entre Newton e gradiente na Rosenbrock e de tres ordens de
    /// grandeza em iteracoes.
    /// </summary>
    [Fact]
    public void NaRosenbrockNewtonGanhaDoGradientePorTresOrdens()
    {
        var funcao = Funcao.Rosenbrock();
        var inicio = Vetor.De(-1.2, 1);

        var newton = Newton.ComBusca(funcao, inicio);
        var gradiente = Busca.Descida(funcao, inicio);

        Assert.True(newton.Iteracoes * 100 < gradiente.Iteracoes,
                    $"Newton {newton.Iteracoes}, gradiente {gradiente.Iteracoes}");
    }

    /// <summary>
    /// Quando a hessiana nao e definida positiva, a direcao de Newton pode
    /// apontar para CIMA: ela vai para um ponto critico, e nem todo ponto
    /// critico e minimo. A versao com busca cai no gradiente nesse caso.
    /// </summary>
    [Fact]
    public void ComHessianaIndefinidaAVersaoComBuscaNaoSobe()
    {
        // uma sela: o minimo em x e o maximo em y
        var sela = new Funcao(
            "sela",
            x => x[0] * x[0] - x[1] * x[1],
            x => new[] { 2 * x[0], -2 * x[1] },
            new[] { 0.0, 0.0 },
            0,
            _ => new[,] { { 2.0, 0 }, { 0, -2.0 } });

        var inicio = Vetor.De(1, 0.5);
        var r = Newton.ComBusca(sela, inicio, limite: 50);

        Assert.True(r.Valor <= sela.Valor(inicio),
                    $"ele subiu: saiu de {sela.Valor(inicio)} e chegou em {r.Valor}");
    }

    [Fact]
    public void FuncaoSemHessianaRecusaDeCara()
    {
        var sem = new Funcao("sem hessiana", x => x[0] * x[0], x => new[] { 2 * x[0] }, new[] { 0.0 }, 0);

        Assert.Throws<ArgumentException>(() => Newton.Puro(sem, Vetor.De(1)));
        Assert.Throws<ArgumentException>(() => Newton.ComBusca(sem, Vetor.De(1)));
    }

    /// <summary>
    /// O expoente medido distingue as duas categorias de convergencia: dois para
    /// Newton, e descendo para um na convergencia linear.
    ///
    /// Ele chega em dois na mosca no caso quadratico, e no linear ele se
    /// aproxima de um POR CIMA e devagar: com dez decadas de erro ele ainda
    /// marca 1,11. Isso nao e defeito da medida, e a conta: o logaritmo de um
    /// erro dividido pelo logaritmo do anterior so vira um quando os dois ja
    /// sao minusculos.
    /// </summary>
    [Fact]
    public void OExpoenteSeparaLinearDeQuadratica()
    {
        var quadratico = Newton.ExpoenteMedido(new[] { 0.1, 0.01, 1e-4, 1e-8 });

        var linear = new List<double>();
        for (var i = 1; i <= 10; i++) linear.Add(Math.Pow(10, -i));
        var medidoLinear = Newton.ExpoenteMedido(linear);

        Assert.Equal(2, quadratico, 6);
        Assert.InRange(medidoLinear, 1.0, 1.2);
        Assert.True(medidoLinear < quadratico - 0.5);
    }

    [Fact]
    public void SemErroUtilOExpoenteNaoEhNumero()
    {
        Assert.True(double.IsNaN(Newton.ExpoenteMedido(Array.Empty<double>())));
        Assert.True(double.IsNaN(Newton.ExpoenteMedido(new[] { 0.0, 0.0 })));
    }
}
