using Conde.Otimizacao;
using Xunit;

namespace Conde.Otimizacao.Testes;

public class DescidaTestes
{
    private static double[] Partida(int dimensao)
    {
        var x = new double[dimensao];
        for (var i = 0; i < dimensao; i++) x[i] = 1;
        return x;
    }

    [Fact]
    public void ADescidaChegaNoMinimoDeVerdade()
    {
        foreach (var k in new[] { 1.0, 10.0, 100.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var r = Descida.Fixa(funcao, Partida(funcao.Dimensao), Descida.PassoOtimo(1, k));

            Assert.True(r.Convergiu);
            Assert.True(r.Distancia < 1e-6, $"k={k} parou a {r.Distancia} do minimo");
        }
    }

    /// <summary>
    /// Com passo acima de 2/L ela DIVERGE: passa do fundo e sobe do outro lado
    /// mais do que desceu.
    /// </summary>
    [Fact]
    public void AcimaDeDoisSobreLElaDiverge()
    {
        var funcao = Funcao.Quadratica(100);
        var inicio = Partida(funcao.Dimensao);

        Assert.True(Descida.Fixa(funcao, inicio, 1.9 / 100, limite: 20_000).Convergiu);
        Assert.False(Descida.Fixa(funcao, inicio, 2.1 / 100, limite: 20_000).Convergiu);
    }

    /// <summary>
    /// E o limite medido por bissecao bate com 2/L, sem que nada no codigo da
    /// descida saiba o que e L.
    /// </summary>
    [Fact]
    public void OLimiteMedidoBateComDoisSobreL()
    {
        foreach (var k in new[] { 1.0, 10.0, 100.0, 1000.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var medido = Busca.MaiorPassoQueConverge(funcao, Partida(funcao.Dimensao));

            Assert.InRange(medido, 2 / k * 0.99, 2 / k * 1.01);
        }
    }

    /// <summary>
    /// O ACHADO CENTRAL: o numero de iteracoes cresce PROPORCIONAL ao
    /// condicionamento. Dez vezes mais mal condicionado, dez vezes mais trabalho,
    /// mesmo com o melhor passo possivel.
    /// </summary>
    [Fact]
    public void OTrabalhoCresceProporcionalAoCondicionamento()
    {
        var anterior = 0;

        foreach (var k in new[] { 10.0, 100.0, 1000.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var iteracoes = Descida.IteracoesPara(funcao, Partida(funcao.Dimensao), Descida.PassoOtimo(1, k), 1e6);

            if (anterior > 0) Assert.InRange((double)iteracoes / anterior, 9, 11);
            anterior = iteracoes;
        }
    }

    /// <summary>
    /// A razao medida com que o erro encolhe bate com (k-1)/(k+1) ate a quarta
    /// casa, e esse numero nao esta escrito em lugar nenhum do codigo da
    /// descida.
    /// </summary>
    [Fact]
    public void ARazaoMedidaBateComATeorica()
    {
        foreach (var k in new[] { 10.0, 100.0, 1000.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var medida = Descida.RazaoMedida(funcao, Partida(funcao.Dimensao), Descida.PassoOtimo(1, k));

            Assert.Equal(Descida.RazaoTeorica(k), medida, 3);
        }
    }

    /// <summary>
    /// E o passo otimo e mesmo otimo: nenhum outro passo encolhe o erro mais
    /// depressa que 2/(L + m).
    /// </summary>
    [Fact]
    public void NenhumPassoGanhaDoPassoOtimo()
    {
        var funcao = Funcao.Quadratica(100);
        var inicio = Partida(funcao.Dimensao);
        var otimo = Descida.IteracoesPara(funcao, inicio, Descida.PassoOtimo(1, 100), 1e6);

        foreach (var fator in new[] { 0.5, 0.8, 0.95, 1.05, 1.5 })
        {
            var outro = Descida.IteracoesPara(funcao, inicio, Descida.PassoOtimo(1, 100) * fator, 1e6);
            Assert.True(outro >= otimo, $"o passo vezes {fator} levou {outro} contra {otimo}");
        }
    }

    /// <summary>
    /// Com k igual a um a funcao e uma tigela redonda: a direcao de descida
    /// aponta direto para o minimo e UM passo resolve.
    /// </summary>
    [Fact]
    public void NaTigelaRedondaUmPassoResolve()
    {
        var funcao = Funcao.Quadratica(1);
        var r = Descida.Fixa(funcao, Partida(funcao.Dimensao), Descida.PassoOtimo(1, 1));

        Assert.True(r.Convergiu);
        Assert.Equal(1, r.Iteracoes);
    }

    [Fact]
    public void OPassoOtimoNaoEhUmSobreL()
    {
        Assert.Equal(2.0 / 101, Descida.PassoOtimo(1, 100), 12);
        Assert.NotEqual(1.0 / 100, Descida.PassoOtimo(1, 100), 6);

        // e ele e maior que 1/L, nao menor
        Assert.True(Descida.PassoOtimo(1, 100) > 1.0 / 100);
    }

    [Fact]
    public void ARazaoTeoricaEhZeroQuandoAFuncaoEhRedonda()
    {
        Assert.Equal(0, Descida.RazaoTeorica(1), 12);
        Assert.True(Descida.RazaoTeorica(1e6) > 0.999998);
    }
}
