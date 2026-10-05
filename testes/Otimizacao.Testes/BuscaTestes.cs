using Conde.Otimizacao;
using Xunit;

namespace Conde.Otimizacao.Testes;

public class BuscaTestes
{
    private static double[] Partida(int dimensao)
    {
        var x = new double[dimensao];
        for (var i = 0; i < dimensao; i++) x[i] = 1;
        return x;
    }

    /// <summary>
    /// O passo de Armijo cumpre o que promete: a queda e pelo menos a fracao
    /// pedida da queda que a inclinacao prometia.
    /// </summary>
    [Fact]
    public void OPassoDeArmijoDesceOBastante()
    {
        foreach (var funcao in Funcao.Todas())
        {
            var ponto = Partida(funcao.Dimensao);
            ponto[0] = -1.2;

            var g = funcao.Gradiente(ponto);
            var direcao = Vetor.Vezes(-1, g);
            var (passo, _) = Busca.Armijo(funcao, ponto, direcao, g);

            Assert.True(passo > 0);
            Assert.True(funcao.Valor(Vetor.Anda(ponto, passo, direcao)) <=
                        funcao.Valor(ponto) + 1e-4 * passo * Vetor.Interno(g, direcao));
        }
    }

    /// <summary>
    /// Se a direcao nao desce, a busca devolve zero em vez de inventar um passo.
    /// Isso e erro de quem escolheu a direcao, e deixar o erro aparecer no lugar
    /// certo vale mais que um passo gigante para cima.
    /// </summary>
    [Fact]
    public void DirecaoQueSobeDevolvePassoZero()
    {
        var funcao = Funcao.Quadratica(10);
        var ponto = Partida(funcao.Dimensao);
        var g = funcao.Gradiente(ponto);

        var (passo, avaliacoes) = Busca.Armijo(funcao, ponto, g, g);

        Assert.Equal(0, passo);
        Assert.Equal(1, avaliacoes);
    }

    /// <summary>
    /// A busca encontra um passo mesmo com o chute inicial absurdamente grande:
    /// ela corta pela metade ate caber.
    /// </summary>
    [Fact]
    public void OChuteGrandeEhCortadoAteCaber()
    {
        var funcao = Funcao.Quadratica(100);
        var ponto = Partida(funcao.Dimensao);
        var g = funcao.Gradiente(ponto);

        var (passo, avaliacoes) = Busca.Armijo(funcao, ponto, Vetor.Vezes(-1, g), g, chute: 1e6);

        Assert.True(passo > 0);
        Assert.True(passo < 1);
        Assert.True(avaliacoes > 10, $"cortou so {avaliacoes} vezes");
    }

    /// <summary>
    /// A descida com busca chega no minimo das quatro funcoes SEM saber a
    /// curvatura de nenhuma delas, que e o ponto inteiro dela existir.
    /// </summary>
    [Fact]
    public void ADescidaComBuscaChegaSemSaberACurvatura()
    {
        foreach (var funcao in Funcao.Todas())
        {
            var inicio = Partida(funcao.Dimensao);
            inicio[0] = -1.2;

            var r = Busca.Descida(funcao, inicio);

            Assert.True(r.Convergiu, $"{funcao.Nome} nao convergiu");
            Assert.True(r.Distancia < 1e-5, $"{funcao.Nome} parou a {r.Distancia}");
        }
    }

    /// <summary>
    /// E ela custa poucas avaliacoes por iteracao. Comecar a busca seguinte no
    /// dobro do passo que funcionou e o que segura esse numero: sem isso, a
    /// busca corta do um toda vez e o custo por iteracao dobra.
    /// </summary>
    [Fact]
    public void ABuscaCustaPoucasAvaliacoesPorIteracao()
    {
        var funcao = Funcao.Quadratica(100);
        var r = Busca.Descida(funcao, Partida(funcao.Dimensao));

        Assert.True((double)r.AvaliacoesDeF / r.Iteracoes < 4,
                    $"deu {(double)r.AvaliacoesDeF / r.Iteracoes:N2} avaliacoes por iteracao");
    }

    /// <summary>
    /// Na Rosenbrock a busca deixa de ser conforto e vira necessidade: a
    /// curvatura muda de ponto para ponto, entao nenhum passo fixo serve para o
    /// caminho inteiro.
    /// </summary>
    [Fact]
    public void NaRosenbrockNenhumPassoFixoServeParaOCaminhoInteiro()
    {
        var funcao = Funcao.Rosenbrock();
        var inicio = Vetor.De(-1.2, 1);

        Assert.True(Busca.Descida(funcao, inicio).Convergiu);

        // a curvatura na partida e na chegada estao em ordens diferentes
        var naPartida = funcao.Hessiana!(inicio)[0, 0];
        var naChegada = funcao.Hessiana!(funcao.Otimo)[0, 0];

        Assert.True(Math.Abs(naPartida / naChegada) > 1.5 || Math.Abs(naChegada / naPartida) > 1.5);
    }

    [Fact]
    public void OLimiteMedidoEhInfinitoQuandoNadaExplode()
    {
        // numa funcao com curvatura minuscula, nenhum passo ate o limite da
        // busca faz a descida explodir
        var funcao = new Funcao(
            "quase plana",
            x => 1e-8 * x[0] * x[0] / 2,
            x => new[] { 1e-8 * x[0] },
            new[] { 0.0 },
            0);

        Assert.True(double.IsInfinity(Busca.MaiorPassoQueConverge(funcao, Vetor.De(1))));
    }
}
