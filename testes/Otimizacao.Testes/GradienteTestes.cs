using Conde.Otimizacao;
using Xunit;

namespace Conde.Otimizacao.Testes;

public class GradienteTestes
{
    private static double[] Ponto(Funcao funcao)
    {
        var x = new double[funcao.Dimensao];
        for (var i = 0; i < x.Length; i++) x[i] = 1;
        x[0] = 0.3;
        return x;
    }

    /// <summary>
    /// Todo gradiente escrito a mao neste repositorio confere com a diferenca
    /// finita. Gradiente errado nao faz o metodo quebrar: faz ele parar no lugar
    /// errado, e o lugar errado tem a mesma cara do certo.
    /// </summary>
    [Fact]
    public void OGradienteEscritoAMaoConfere()
    {
        foreach (var funcao in Funcao.Todas())
            Assert.True(Gradiente.Conferir(funcao, Ponto(funcao)) < 1e-6,
                        $"{funcao.Nome} errou {Gradiente.Conferir(funcao, Ponto(funcao))}");
    }

    /// <summary>
    /// E confere em varios pontos, nao so num: um sinal trocado em um termo
    /// pode passar despercebido num ponto especial.
    /// </summary>
    [Fact]
    public void EConfereEmVariosPontos()
    {
        foreach (var funcao in Funcao.Todas())
            foreach (var escala in new[] { -2.0, -0.5, 0.7, 3.0 })
            {
                var ponto = new double[funcao.Dimensao];
                for (var i = 0; i < ponto.Length; i++) ponto[i] = escala * (1 + 0.1 * i);

                Assert.True(Gradiente.Conferir(funcao, ponto) < 1e-5,
                            $"{funcao.Nome} em {escala} errou {Gradiente.Conferir(funcao, ponto)}");
            }
    }

    /// <summary>
    /// O ACHADO: o melhor passo NAO e o menor passo.
    ///
    /// Sao dois erros brigando. O de truncamento cai com o passo; o de
    /// arredondamento cresce quando o passo encolhe, porque subtrair dois
    /// numeros quase iguais joga fora os digitos que importam.
    /// </summary>
    [Fact]
    public void OMenorPassoNaoEhOMelhorPasso()
    {
        var funcao = Funcao.Rosenbrock();
        var ponto = Vetor.De(0.3, 1);

        var minusculo = Gradiente.Conferir(funcao, ponto, 1e-13);
        var razoavel = Gradiente.Conferir(funcao, ponto, 1e-5);

        Assert.True(minusculo > razoavel * 100, $"minusculo {minusculo}, razoavel {razoavel}");
    }

    /// <summary>
    /// E o melhor passo da centrada na Rosenbrock cai perto de 1e-6, que e a
    /// casa da raiz cubica da precisao da maquina, exatamente onde a teoria diz
    /// que ele fica.
    /// </summary>
    [Fact]
    public void OMelhorPassoCaiOndeATeoriaDiz()
    {
        var (passo, erro) = Gradiente.MelhorPasso(Funcao.Rosenbrock(), Vetor.De(0.3, 1));

        Assert.InRange(passo, 1e-7, 1e-4);
        Assert.True(erro < 1e-9);
    }

    /// <summary>
    /// Na QUADRATICA a diferenca centrada e exata, e isso explica a tabela: o
    /// melhor passo medido ali e o maior testado. O erro da centrada comeca na
    /// terceira derivada, e num polinomio de grau dois ela vale zero, entao so
    /// sobra arredondamento, e passo maior sempre melhora.
    /// </summary>
    [Fact]
    public void NaQuadraticaACentradaEhExata()
    {
        var funcao = Funcao.Quadratica(100);
        var ponto = Ponto(funcao);

        foreach (var passo in new[] { 1e-1, 1.0, 10.0 })
            for (var i = 0; i < funcao.Dimensao; i++)
                Assert.Equal(funcao.Gradiente(ponto)[i], Gradiente.Centrada(funcao, ponto, i, passo), 8);
    }

    /// <summary>
    /// E a de olhar so para a frente NAO e exata nem na quadratica: ela erra
    /// proporcional ao passo, e metade do passo e metade do erro.
    /// </summary>
    [Fact]
    public void PelaFrenteErraProporcionalAoPasso()
    {
        var funcao = Funcao.Quadratica(100);
        var ponto = Ponto(funcao);
        var certo = funcao.Gradiente(ponto)[0];

        var grosso = Math.Abs(Gradiente.PelaFrente(funcao, ponto, 0, 1e-2) - certo);
        var fino = Math.Abs(Gradiente.PelaFrente(funcao, ponto, 0, 5e-3) - certo);

        Assert.InRange(grosso / fino, 1.9, 2.1);
    }

    /// <summary>
    /// A centrada vale a avaliacao a mais: no melhor passo de cada uma, ela erra
    /// ordens de grandeza menos.
    /// </summary>
    [Fact]
    public void ACentradaGanhaDaProgressivaPorOrdensDeGrandeza()
    {
        foreach (var funcao in Funcao.Todas())
        {
            var (_, frente) = Gradiente.MelhorPasso(funcao, Ponto(funcao), centrada: false);
            var (_, centro) = Gradiente.MelhorPasso(funcao, Ponto(funcao));

            Assert.True(centro * 100 < frente, $"{funcao.Nome}: frente {frente}, centro {centro}");
        }
    }

    /// <summary>
    /// E a conferencia PEGA um gradiente errado: uma funcao com o sinal trocado
    /// num termo passa em todo o resto do repositorio e e reprovada aqui.
    /// </summary>
    [Fact]
    public void AConferenciaPegaUmGradienteErrado()
    {
        var errada = new Funcao(
            "quadratica com o sinal trocado",
            x => x[0] * x[0] + 3 * x[1] * x[1],
            x => new[] { 2 * x[0], -6 * x[1] },
            new[] { 0.0, 0.0 },
            0);

        Assert.True(Gradiente.Conferir(errada, Vetor.De(1, 1)) > 1);
    }
}
