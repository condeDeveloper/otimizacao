using Conde.Otimizacao;
using Xunit;

namespace Conde.Otimizacao.Testes;

public class VetorTestes
{
    [Fact]
    public void AsContasBasicasSaoOQueDizem()
    {
        var a = Vetor.De(1, 2, 3);
        var b = Vetor.De(10, 20, 30);

        Assert.Equal(new[] { 11.0, 22, 33 }, Vetor.Mais(a, b));
        Assert.Equal(new[] { -9.0, -18, -27 }, Vetor.Menos(a, b));
        Assert.Equal(new[] { 2.0, 4, 6 }, Vetor.Vezes(2, a));
        Assert.Equal(140, Vetor.Interno(a, b), 12);
        Assert.Equal(5, Vetor.Norma(Vetor.De(3, 4)), 12);
    }

    /// <summary>
    /// Andar e a conta que todo metodo de descida faz, entao ela e conferida
    /// contra a soma feita na mao.
    /// </summary>
    [Fact]
    public void AndarEhSomarComEscalar()
    {
        var a = Vetor.De(1, 1);
        var direcao = Vetor.De(2, -4);

        Assert.Equal(new[] { 1.5, 0.0 }, Vetor.Anda(a, 0.25, direcao));
        Assert.Equal(Vetor.Mais(a, Vetor.Vezes(0.25, direcao)), Vetor.Anda(a, 0.25, direcao));
    }

    [Fact]
    public void CopiarNaoCompartilha()
    {
        var a = Vetor.De(1, 2);
        var copia = Vetor.Copia(a);
        copia[0] = 99;

        Assert.Equal(1, a[0]);
    }

    [Fact]
    public void AMatrizVezesOVetorEhOQueDiz()
    {
        var matriz = new[,] { { 1.0, 2 }, { 3, 4.0 } };
        Assert.Equal(new[] { 5.0, 11 }, Vetor.Vezes(matriz, Vetor.De(1, 2)));
    }

    /// <summary>
    /// O sistema resolvido confere de volta: multiplicar a matriz pela solucao
    /// tem que devolver o lado direito.
    /// </summary>
    [Fact]
    public void ResolverDevolveUmaSolucaoDeVerdade()
    {
        var matriz = new[,] { { 4.0, 1, 2 }, { 1, 5, 3 }, { 2, 3, 6 } };
        var lado = Vetor.De(7, 9, 11);

        var x = Vetor.Resolver(matriz, lado);
        Assert.NotNull(x);

        var volta = Vetor.Vezes(matriz, x!);
        for (var i = 0; i < 3; i++) Assert.Equal(lado[i], volta[i], 10);
    }

    /// <summary>
    /// O pivotamento nao e enfeite: sem trocar linhas, o primeiro pivo aqui e
    /// zero e a eliminacao divide por zero logo na primeira coluna.
    /// </summary>
    [Fact]
    public void OPivotamentoSalvaOCasoComZeroNaDiagonal()
    {
        var matriz = new[,] { { 0.0, 1 }, { 1, 0.0 } };
        var x = Vetor.Resolver(matriz, Vetor.De(3, 5));

        Assert.NotNull(x);
        Assert.Equal(5, x![0], 12);
        Assert.Equal(3, x[1], 12);
    }

    [Fact]
    public void MatrizSingularDevolveNulo()
    {
        var matriz = new[,] { { 1.0, 2 }, { 2, 4.0 } };
        Assert.Null(Vetor.Resolver(matriz, Vetor.De(1, 2)));
    }
}
