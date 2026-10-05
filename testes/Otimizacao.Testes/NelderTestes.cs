using Conde.Otimizacao;
using Xunit;

namespace Conde.Otimizacao.Testes;

public class NelderTestes
{
    private static double[] Partida(int dimensao)
    {
        var x = new double[dimensao];
        for (var i = 0; i < dimensao; i++) x[i] = 1;
        x[0] = -1.2;
        return x;
    }

    /// <summary>
    /// O simplex chega no minimo das quatro funcoes sem olhar derivada nenhuma:
    /// ele so sabe comparar valores.
    /// </summary>
    [Fact]
    public void OSimplexChegaSemDerivadaNenhuma()
    {
        foreach (var funcao in Funcao.Todas())
        {
            var r = Nelder.Minimizar(funcao, Partida(funcao.Dimensao));

            Assert.True(r.Convergiu, $"{funcao.Nome} nao convergiu");
            Assert.True(r.Distancia < 1e-6, $"{funcao.Nome} parou a {r.Distancia}");
            Assert.Equal(0, r.AvaliacoesDeGradiente);
        }
    }

    /// <summary>
    /// O ACHADO: na Rosenbrock o simplex, SEM derivada, ganha da descida de
    /// gradiente com derivada e busca de linha, por quase quatrocentas vezes em
    /// avaliacoes da funcao.
    ///
    /// Nao e milagre: a descida de gradiente e simplesmente muito ruim num vale
    /// curvo, e "usar mais informacao" nao e o mesmo que "usar bem a informacao".
    /// </summary>
    [Fact]
    public void NaRosenbrockOSimplexGanhaDaDescidaDeGradiente()
    {
        var funcao = Funcao.Rosenbrock();
        var inicio = Vetor.De(-1.2, 1);

        var nelder = Nelder.Minimizar(funcao, inicio);
        var gradiente = Busca.Descida(funcao, inicio);

        Assert.True(nelder.AvaliacoesDeF * 100 < gradiente.Custo(2),
                    $"simplex {nelder.AvaliacoesDeF}, gradiente {gradiente.Custo(2)}");
    }

    /// <summary>
    /// E perde feio para Newton, que usa a curvatura. A ordem e sempre a mesma
    /// quando a derivada existe: curvatura ganha de inclinacao, e inclinacao
    /// costuma ganhar de comparacao.
    /// </summary>
    [Fact]
    public void EPerdeParaNewtonQuandoACurvaturaExiste()
    {
        var funcao = Funcao.Rosenbrock();
        var inicio = Vetor.De(-1.2, 1);

        var nelder = Nelder.Minimizar(funcao, inicio);
        var newton = Newton.ComBusca(funcao, inicio);

        Assert.True(newton.Custo(2) < nelder.AvaliacoesDeF);
    }

    /// <summary>
    /// O custo do simplex cresce com a DIMENSAO, e nao com o condicionamento:
    /// ele carrega n+1 pontos e cada rodada mexe em um. Em dez variaveis ele
    /// gasta uma ordem de grandeza a mais que em duas.
    /// </summary>
    [Fact]
    public void OCustoDoSimplexCresceComADimensao()
    {
        var duas = Nelder.Minimizar(Funcao.Torta(), Vetor.De(-1.2, 1));
        var dez = Nelder.Minimizar(Funcao.Quadratica(100), Partida(10));

        Assert.True(dez.AvaliacoesDeF > duas.AvaliacoesDeF * 5,
                    $"duas {duas.AvaliacoesDeF}, dez {dez.AvaliacoesDeF}");
    }

    [Fact]
    public void OSimplexInicialTemNMaisUmPontos()
    {
        var pontos = Nelder.SimplexInicial(Vetor.De(1, 2, 3));

        Assert.Equal(4, pontos.Length);
        Assert.Equal(new[] { 1.0, 2, 3 }, pontos[0]);

        // cada ponto difere do primeiro em uma coordenada so
        for (var i = 1; i < pontos.Length; i++)
        {
            var diferentes = 0;
            for (var j = 0; j < 3; j++) if (Math.Abs(pontos[i][j] - pontos[0][j]) > 1e-15) diferentes++;
            Assert.Equal(1, diferentes);
        }
    }

    /// <summary>
    /// O deslocamento e proporcional a coordenada quando ela nao e zero, o que
    /// mantem o simplex na escala do problema. Na coordenada zero isso daria um
    /// simplex degenerado, entao ali ele vira absoluto.
    /// </summary>
    [Fact]
    public void OSimplexNaoDegeneraNaCoordenadaZero()
    {
        var pontos = Nelder.SimplexInicial(Vetor.De(0, 100));

        Assert.Equal(0.1, pontos[1][0], 12);
        Assert.Equal(110, pontos[2][1], 12);
        Assert.True(Nelder.Tamanho(pontos) > 0);
    }

    [Fact]
    public void OTamanhoDoSimplexEhAMaiorDistanciaAteOPrimeiro()
    {
        var pontos = new[] { Vetor.De(0, 0), Vetor.De(3, 4), Vetor.De(1, 0) };
        Assert.Equal(5, Nelder.Tamanho(pontos), 12);
    }

    /// <summary>
    /// O simplex nao precisa de gradiente nem de hessiana, e isso da para
    /// conferir: uma funcao que nao tem nenhum dos dois escritos e minimizada do
    /// mesmo jeito.
    /// </summary>
    [Fact]
    public void FuncaoSemGradienteUtilTambemEhMinimizada()
    {
        var sem = new Funcao(
            "sem derivada escrita",
            x => Math.Abs(x[0] - 3) + Math.Abs(x[1] + 2),
            _ => throw new InvalidOperationException("esta funcao nao tem gradiente"),
            new[] { 3.0, -2.0 },
            0);

        var r = Nelder.Minimizar(sem, Vetor.De(0, 0), 1e-8);
        Assert.True(r.Distancia < 1e-6, $"parou a {r.Distancia}");
    }
}
