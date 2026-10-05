using Conde.Otimizacao;
using Xunit;

namespace Conde.Otimizacao.Testes;

public class MomentoTestes
{
    private static double[] Partida(int dimensao)
    {
        var x = new double[dimensao];
        for (var i = 0; i < dimensao; i++) x[i] = 1;
        return x;
    }

    [Fact]
    public void ABolaPesadaChegaNoMinimo()
    {
        foreach (var k in new[] { 10.0, 100.0, 1000.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var (passo, peso) = Momento.Parametros(1, k);
            var r = Momento.BolaPesada(funcao, Partida(funcao.Dimensao), passo, peso);

            Assert.True(r.Convergiu);
            Assert.True(r.Distancia < 1e-6, $"k={k} parou a {r.Distancia}");
        }
    }

    /// <summary>
    /// O ACHADO: o custo do momento cresce com a RAIZ do condicionamento, e o da
    /// descida cresce com o condicionamento inteiro. Cem vezes mais mal
    /// condicionado, dez vezes mais trabalho em vez de cem.
    /// </summary>
    [Fact]
    public void OCustoDoMomentoCresceComARaizDoCondicionamento()
    {
        var anterior = 0;

        foreach (var k in new[] { 100.0, 10_000.0, 1_000_000.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var (passo, peso) = Momento.Parametros(1, k);
            var iteracoes = Momento.IteracoesPara(funcao, Partida(funcao.Dimensao), passo, peso, 1e6);

            if (anterior > 0) Assert.InRange((double)iteracoes / anterior, 7, 13);
            anterior = iteracoes;
        }
    }

    /// <summary>
    /// E a comparacao direta: na mesma funcao, do mesmo ponto, ate o mesmo
    /// alvo, o momento gasta uma fracao do que a descida gasta, e a fracao
    /// encolhe conforme o problema piora.
    /// </summary>
    [Fact]
    public void OGanhoDoMomentoCresceComOProblemaPiorando()
    {
        var anterior = 0.0;

        foreach (var k in new[] { 100.0, 1000.0, 10_000.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var inicio = Partida(funcao.Dimensao);

            var semMomento = Descida.IteracoesPara(funcao, inicio, Descida.PassoOtimo(1, k), 1e6);
            var (passo, peso) = Momento.Parametros(1, k);
            var comMomento = Momento.IteracoesPara(funcao, inicio, passo, peso, 1e6);

            var ganho = (double)semMomento / comMomento;

            Assert.True(ganho > 2, $"k={k} ganhou so {ganho:N1} vezes");
            Assert.True(ganho > anterior, $"k={k} ganhou {ganho:N1}, menos que o {anterior:N1} anterior");
            anterior = ganho;
        }
    }

    /// <summary>
    /// O Nesterov tambem chega, com os parametros DELE.
    ///
    /// Isso me custou uma medida inteira: rodar o Nesterov com os parametros da
    /// bola pesada nao converge, e o limite de iteracoes bate em todas as
    /// linhas. O peso dele e a raiz de k sem o quadrado, e o passo e 1/L sem o
    /// (1 + peso).
    /// </summary>
    [Fact]
    public void OsParametrosDoNesterovNaoSaoOsDaBolaPesada()
    {
        var funcao = Funcao.Quadratica(1000);
        var inicio = Partida(funcao.Dimensao);

        var (passoBola, pesoBola) = Momento.Parametros(1, 1000);
        var (passoNesterov, pesoNesterov) = Momento.ParametrosNesterov(1, 1000);

        Assert.NotEqual(pesoBola, pesoNesterov, 6);
        Assert.NotEqual(passoBola, passoNesterov, 6);

        var certo = Momento.Nesterov(funcao, inicio, passoNesterov, pesoNesterov, limite: 100_000);
        Assert.True(certo.Convergiu);

        var errado = Momento.Nesterov(funcao, inicio, passoBola, pesoBola, limite: 100_000);
        Assert.False(errado.Convergiu);
    }

    /// <summary>
    /// Os dois metodos de momento ficam na mesma ordem de grandeza de
    /// gradientes, e os dois ganham da descida por muito.
    /// </summary>
    [Fact]
    public void OsDoisMomentosGanhamDaDescidaPorMuito()
    {
        var funcao = Funcao.Quadratica(10_000);
        var inicio = Partida(funcao.Dimensao);

        var descida = Descida.Fixa(funcao, inicio, Descida.PassoOtimo(1, 10_000));

        var (passo, peso) = Momento.Parametros(1, 10_000);
        var pesada = Momento.BolaPesada(funcao, inicio, passo, peso);

        var (passoN, pesoN) = Momento.ParametrosNesterov(1, 10_000);
        var nesterov = Momento.Nesterov(funcao, inicio, passoN, pesoN);

        Assert.True(pesada.AvaliacoesDeGradiente * 10 < descida.AvaliacoesDeGradiente);
        Assert.True(nesterov.AvaliacoesDeGradiente * 10 < descida.AvaliacoesDeGradiente);
        Assert.InRange((double)nesterov.AvaliacoesDeGradiente / pesada.AvaliacoesDeGradiente, 0.5, 3);
    }

    /// <summary>
    /// O peso tem que ficar abaixo de um: um peso de um ou mais faz a bola nunca
    /// perder velocidade e o metodo nunca parar.
    /// </summary>
    [Fact]
    public void OPesoFicaSempreAbaixoDeUm()
    {
        foreach (var k in new[] { 1.0, 10.0, 1e6, 1e12 })
        {
            var (passo, peso) = Momento.Parametros(1, k);

            Assert.InRange(peso, 0, 1);
            Assert.True(passo > 0);
        }

        // com a funcao redonda nao ha ziguezague nenhum para corrigir
        Assert.Equal(0, Momento.Parametros(1, 1).Peso, 12);
    }

    [Fact]
    public void ONesterovGastaDoisGradientesPorIteracao()
    {
        var funcao = Funcao.Quadratica(100);
        var (passo, peso) = Momento.ParametrosNesterov(1, 100);
        var r = Momento.Nesterov(funcao, Partida(funcao.Dimensao), passo, peso);

        Assert.True(r.Convergiu);
        Assert.Equal(2 * r.Iteracoes + 1, r.AvaliacoesDeGradiente);
    }
}
