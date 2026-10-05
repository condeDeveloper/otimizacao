namespace Conde.Otimizacao;

/// <summary>
/// As contas de vetor que todo metodo deste repositorio usa.
///
/// Elas sao poucas de proposito. Otimizacao e quase toda feita de somar vetor
/// com vetor vezes escalar, e deixar essa conta num lugar so faz os metodos
/// ficarem com o tamanho das ideias deles, em vez de somem dentro de tres lacos
/// aninhados.
/// </summary>
public static class Vetor
{
    public static double[] Zeros(int n) => new double[n];

    public static double[] De(params double[] valores) => (double[])valores.Clone();

    public static double[] Copia(double[] a)
    {
        ArgumentNullException.ThrowIfNull(a);
        return (double[])a.Clone();
    }

    public static double[] Mais(double[] a, double[] b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var r = new double[a.Length];
        for (var i = 0; i < a.Length; i++) r[i] = a[i] + b[i];
        return r;
    }

    public static double[] Menos(double[] a, double[] b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var r = new double[a.Length];
        for (var i = 0; i < a.Length; i++) r[i] = a[i] - b[i];
        return r;
    }

    public static double[] Vezes(double escalar, double[] a)
    {
        ArgumentNullException.ThrowIfNull(a);

        var r = new double[a.Length];
        for (var i = 0; i < a.Length; i++) r[i] = escalar * a[i];
        return r;
    }

    /// <summary>a + escalar * b, que e o passo de todo metodo de descida.</summary>
    public static double[] Anda(double[] a, double escalar, double[] b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var r = new double[a.Length];
        for (var i = 0; i < a.Length; i++) r[i] = a[i] + escalar * b[i];
        return r;
    }

    public static double Interno(double[] a, double[] b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var soma = 0.0;
        for (var i = 0; i < a.Length; i++) soma += a[i] * b[i];
        return soma;
    }

    public static double Norma(double[] a) => Math.Sqrt(Interno(a, a));

    /// <summary>A matriz vezes o vetor, para a hessiana do metodo de Newton.</summary>
    public static double[] Vezes(double[,] matriz, double[] a)
    {
        ArgumentNullException.ThrowIfNull(matriz);
        ArgumentNullException.ThrowIfNull(a);

        var n = a.Length;
        var r = new double[n];

        for (var i = 0; i < n; i++)
        {
            var soma = 0.0;
            for (var j = 0; j < n; j++) soma += matriz[i, j] * a[j];
            r[i] = soma;
        }

        return r;
    }

    /// <summary>
    /// Resolve o sistema por eliminacao com pivotamento parcial.
    ///
    /// O metodo de Newton precisa disto a cada iteracao, e nao precisa de mais
    /// nada: a hessiana e pequena e densa, e trocar linhas para nao dividir por
    /// um pivo minusculo e o cuidado inteiro.
    /// </summary>
    public static double[]? Resolver(double[,] matriz, double[] lado)
    {
        ArgumentNullException.ThrowIfNull(matriz);
        ArgumentNullException.ThrowIfNull(lado);

        var n = lado.Length;
        var a = new double[n, n + 1];

        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++) a[i, j] = matriz[i, j];
            a[i, n] = lado[i];
        }

        for (var coluna = 0; coluna < n; coluna++)
        {
            var melhor = coluna;
            for (var linha = coluna + 1; linha < n; linha++)
                if (Math.Abs(a[linha, coluna]) > Math.Abs(a[melhor, coluna])) melhor = linha;

            if (Math.Abs(a[melhor, coluna]) < 1e-300) return null;

            if (melhor != coluna)
                for (var j = coluna; j <= n; j++) (a[coluna, j], a[melhor, j]) = (a[melhor, j], a[coluna, j]);

            for (var linha = coluna + 1; linha < n; linha++)
            {
                var fator = a[linha, coluna] / a[coluna, coluna];
                for (var j = coluna; j <= n; j++) a[linha, j] -= fator * a[coluna, j];
            }
        }

        var x = new double[n];
        for (var i = n - 1; i >= 0; i--)
        {
            var soma = a[i, n];
            for (var j = i + 1; j < n; j++) soma -= a[i, j] * x[j];
            x[i] = soma / a[i, i];
        }

        return x;
    }
}
