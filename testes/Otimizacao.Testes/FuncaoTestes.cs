using Conde.Otimizacao;
using Xunit;

namespace Conde.Otimizacao.Testes;

/// <summary>
/// O juiz tambem e julgado.
///
/// Todo problema deste repositorio vem com o minimo conhecido, e e contra ele
/// que todo metodo e medido. Se um desses minimos estivesse errado, os metodos
/// continuariam convergindo e o repositorio inteiro estaria medindo com muita
/// precisao a distancia ate o lugar errado.
/// </summary>
public class FuncaoTestes
{
    [Fact]
    public void OMinimoDeclaradoTemGradienteZero()
    {
        foreach (var funcao in Funcao.Todas())
            Assert.True(Vetor.Norma(funcao.Gradiente(funcao.Otimo)) < 1e-12,
                        $"{funcao.Nome}: o gradiente no otimo vale {Vetor.Norma(funcao.Gradiente(funcao.Otimo))}");
    }

    [Fact]
    public void EOValorAliEhOValorDeclarado()
    {
        foreach (var funcao in Funcao.Todas())
            Assert.Equal(funcao.ValorOtimo, funcao.Valor(funcao.Otimo), 12);
    }

    /// <summary>
    /// Gradiente zero sozinho nao prova minimo: um ponto de sela tambem tem.
    /// Aqui a vizinhanca inteira e conferida, em todas as direcoes dos eixos e
    /// nas diagonais.
    /// </summary>
    [Fact]
    public void NadaPertoDoMinimoEhMenorQueEle()
    {
        foreach (var funcao in Funcao.Todas())
            foreach (var raio in new[] { 1e-3, 1e-2, 0.1 })
                for (var i = 0; i < funcao.Dimensao; i++)
                    foreach (var lado in new[] { -1, 1 })
                    {
                        var perto = Vetor.Copia(funcao.Otimo);
                        perto[i] += lado * raio;

                        Assert.True(funcao.Valor(perto) > funcao.ValorOtimo,
                                    $"{funcao.Nome} tem ponto menor que o minimo declarado");
                    }
    }

    /// <summary>
    /// O condicionamento declarado da quadratica tem que ser o que a hessiana
    /// realmente diz. Ele e diagonal, entao os autovalores sao os proprios
    /// elementos.
    /// </summary>
    [Fact]
    public void OCondicionamentoDaQuadraticaEhOQueEleDiz()
    {
        foreach (var k in new[] { 1.0, 10.0, 100.0, 1000.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var hessiana = funcao.Hessiana!(funcao.Otimo);

            double menor = double.MaxValue, maior = 0;
            for (var i = 0; i < funcao.Dimensao; i++)
            {
                menor = Math.Min(menor, hessiana[i, i]);
                maior = Math.Max(maior, hessiana[i, i]);
            }

            Assert.Equal(1, menor, 10);
            Assert.Equal(k, maior, 8);
            Assert.Equal(k, funcao.Condicionamento, 8);
        }
    }

    /// <summary>
    /// A quadratica torta tem os mesmos autovalores 1 e 100 da diagonal, so que
    /// girados. O traco e a soma deles e o determinante e o produto, entao os
    /// dois numeros saem sem calcular autovalor nenhum.
    /// </summary>
    [Fact]
    public void AQuadraticaTortaTemOsMesmosAutovaloresGirados()
    {
        var torta = Funcao.Torta();
        var h = torta.Hessiana!(torta.Otimo);

        var traco = h[0, 0] + h[1, 1];
        var determinante = h[0, 0] * h[1, 1] - h[0, 1] * h[1, 0];

        Assert.Equal(101, traco, 10);
        Assert.Equal(100, determinante, 10);
        Assert.Equal(100, torta.Condicionamento, 10);

        // e ela nao e diagonal: nenhuma coordenada sozinha aponta para o minimo
        Assert.True(Math.Abs(h[0, 1]) > 1);
    }

    /// <summary>
    /// A hessiana e simetrica em todas elas, que e o que toda segunda derivada
    /// de funcao lisa tem que ser.
    /// </summary>
    [Fact]
    public void AHessianaEhSimetrica()
    {
        foreach (var funcao in Funcao.Todas())
        {
            var ponto = Vetor.Copia(funcao.Otimo);
            for (var i = 0; i < ponto.Length; i++) ponto[i] += 0.37;

            var h = funcao.Hessiana!(ponto);
            for (var i = 0; i < funcao.Dimensao; i++)
                for (var j = 0; j < funcao.Dimensao; j++)
                    Assert.Equal(h[i, j], h[j, i], 10);
        }
    }

    [Fact]
    public void ARosenbrockTemOMinimoEmUmUm()
    {
        var r = Funcao.Rosenbrock();

        Assert.Equal(new[] { 1.0, 1.0 }, r.Otimo);
        Assert.Equal(0, r.Valor(Vetor.De(1, 1)), 12);
        Assert.Equal(1, r.Valor(Vetor.De(0, 0)), 12);
        Assert.Equal(24.2, r.Valor(Vetor.De(-1.2, 1)), 10);
    }

    [Fact]
    public void ADistanciaEhAteOOtimoDeclarado()
    {
        var r = Funcao.Rosenbrock();

        Assert.Equal(0, r.Distancia(Vetor.De(1, 1)), 12);
        Assert.Equal(Math.Sqrt(2), r.Distancia(Vetor.De(0, 0)), 12);
    }
}
