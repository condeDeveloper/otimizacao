using Conde.Otimizacao;

namespace Conde.Otimizacao.Medidor;

/// <summary>
/// As medidas. Tudo contado em iteracoes e em avaliacoes da funcao: nenhuma
/// depende de relogio nem de maquina.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "gradiente": Conferencia(); break;
            case "passo": Passo(); break;
            case "condicionamento": Condicionamento(); break;
            case "momento": Momentos(); break;
            case "newton": Newtons(); break;
            case "semderivada": SemDerivada(); break;
            case "tudo": Conferencia(); Passo(); Condicionamento(); Momentos(); Newtons(); SemDerivada(); break;
            default:
                Console.Error.WriteLine("medidas: gradiente, passo, condicionamento, momento, newton, semderivada, tudo");
                return 1;
        }
        return 0;
    }

    private static double[] Partida(int dimensao)
    {
        var x = new double[dimensao];
        for (var i = 0; i < dimensao; i++) x[i] = 1;
        return x;
    }

    /// <summary>O gradiente analitico contra diferencas finitas.</summary>
    private static void Conferencia()
    {
        Console.WriteLine("== o gradiente escrito a mao contra a diferenca finita ==");
        Console.WriteLine();
        Console.WriteLine($"{"funcao",-24}{"melhor passo",16}{"erro ali",14}{"erro com 1e-12",17}");

        foreach (var funcao in Funcao.Todas())
        {
            var ponto = Partida(funcao.Dimensao);
            ponto[0] = 0.3;

            var (passo, erro) = Gradiente.MelhorPasso(funcao, ponto);
            var minusculo = Gradiente.Conferir(funcao, ponto, 1e-12);

            Console.WriteLine($"{funcao.Nome,-24}{passo,16:E0}{erro,14:E2}{minusculo,17:E2}");
        }

        Console.WriteLine();
        Console.WriteLine("o melhor passo NAO e o menor passo, e a ultima coluna mostra o preco de");
        Console.WriteLine("achar que e: com passo de 1e-12 o erro e milhoes de vezes maior que com o");
        Console.WriteLine("passo certo");
        Console.WriteLine();
        Console.WriteLine("sao dois erros brigando. O de truncamento cai com o passo; o de");
        Console.WriteLine("arredondamento CRESCE quando o passo encolhe, porque subtrair dois numeros");
        Console.WriteLine("quase iguais joga fora os digitos que importam");
        Console.WriteLine();
        Console.WriteLine("nas quadraticas o melhor passo medido e 1e-1, que e o MAIOR que a busca");
        Console.WriteLine("testou, e isso tem motivo: a diferenca centrada e EXATA em polinomio de grau");
        Console.WriteLine("dois, porque o erro dela comeca na terceira derivada e ela vale zero. Ali so");
        Console.WriteLine("sobra o arredondamento, e passo maior sempre melhora");
        Console.WriteLine();
        Console.WriteLine("a Rosenbrock, que tem termo de grau quatro, e a unica linha com os dois");
        Console.WriteLine("erros de verdade: o melhor passo dela cai em 1e-6, perto da raiz cubica da");
        Console.WriteLine("precisao da maquina, que e onde a teoria diz que ele fica");
        Console.WriteLine();

        Console.WriteLine($"  e a diferenca entre olhar para a frente e olhar para os dois lados:");
        Console.WriteLine($"  {"funcao",-24}{"pela frente",16}{"centrada",14}");

        foreach (var funcao in Funcao.Todas())
        {
            var ponto = Partida(funcao.Dimensao);
            ponto[0] = 0.3;

            var (_, frente) = Gradiente.MelhorPasso(funcao, ponto, centrada: false);
            var (_, centro) = Gradiente.MelhorPasso(funcao, ponto);

            Console.WriteLine($"  {funcao.Nome,-24}{frente,16:E2}{centro,14:E2}");
        }

        Console.WriteLine();
        Console.WriteLine("  uma avaliacao a mais compra tres ordens de grandeza na Rosenbrock e sete");
        Console.WriteLine("  nas quadraticas, porque os termos de ordem impar do erro se cancelam entre");
        Console.WriteLine("  os dois lados, e na quadratica eles eram o erro inteiro");
        Console.WriteLine();
    }

    /// <summary>O maior passo em que a descida fixa ainda converge.</summary>
    private static void Passo()
    {
        Console.WriteLine("== o maior passo em que a descida de gradiente ainda converge ==");
        Console.WriteLine();
        Console.WriteLine($"{"funcao",-24}{"medido",14}{"2/L",14}{"passo otimo",14}{"1/L",14}");

        foreach (var k in new[] { 1.0, 10.0, 100.0, 1000.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var medido = Busca.MaiorPassoQueConverge(funcao, Partida(funcao.Dimensao));

            Console.WriteLine($"{funcao.Nome,-24}{medido,14:E3}{2 / k,14:E3}" +
                              $"{Descida.PassoOtimo(1, k),14:E3}{1 / k,14:E3}");
        }

        Console.WriteLine();
        Console.WriteLine("o limite medido bate com 2/L sem que nada no codigo saiba o que e L: a");
        Console.WriteLine("bissecao so roda a descida e olha quem explode");
        Console.WriteLine();
        Console.WriteLine("e o melhor passo nao e o maior que converge, nem 1/L, que e o palpite");
        Console.WriteLine("natural: e 2/(L + m), que equilibra o encolhimento na direcao mais curva");
        Console.WriteLine("e na mais plana");
        Console.WriteLine();
    }

    /// <summary>O custo do condicionamento, que e o assunto central.</summary>
    private static void Condicionamento()
    {
        Console.WriteLine("== quantas iteracoes para encolher o erro por um milhao ==");
        Console.WriteLine();
        Console.WriteLine($"{"k",10}{"descida",12}{"razao teorica",16}{"razao medida",15}{"momento",11}{"Newton",9}");

        foreach (var k in new[] { 1.0, 10.0, 100.0, 1000.0, 10000.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var inicio = Partida(funcao.Dimensao);

            var passo = Descida.PassoOtimo(1, k);
            var descida = Descida.IteracoesPara(funcao, inicio, passo, 1e6);

            var (passoMomento, peso) = Momento.Parametros(1, k);
            var momento = Momento.IteracoesPara(funcao, inicio, passoMomento, peso, 1e6);

            var newton = Newton.Puro(funcao, inicio).Iteracoes;

            Console.WriteLine($"{k,10:N0}{descida,12:N0}{Descida.RazaoTeorica(k),16:N6}" +
                              $"{Descida.RazaoMedida(funcao, inicio, passo),15:N6}{momento,11:N0}{newton,9}");
        }

        Console.WriteLine();
        Console.WriteLine("a coluna da descida cresce PROPORCIONAL a k: dez vezes mais mal condicionado,");
        Console.WriteLine("dez vezes mais iteracoes. E a razao medida bate com (k-1)/(k+1) na sexta");
        Console.WriteLine("casa, sem que o codigo da descida conheca k");
        Console.WriteLine();
        Console.WriteLine("a coluna do momento cresce com a RAIZ de k, e e a mesma funcao, o mesmo");
        Console.WriteLine("ponto de partida e uma linha a mais de conta");
        Console.WriteLine();
        Console.WriteLine("e a coluna do Newton nao cresce. Ela e UM, sempre: numa quadratica a");
        Console.WriteLine("parabola que ele ajusta nao e aproximacao, e a propria funcao, entao ele vai");
        Console.WriteLine("direto para o fundo de qualquer ponto e com qualquer condicionamento");
        Console.WriteLine();
    }

    /// <summary>Bola pesada e Nesterov, lado a lado.</summary>
    private static void Momentos()
    {
        Console.WriteLine("== o momento, medido em gradientes e nao em iteracoes ==");
        Console.WriteLine();
        Console.WriteLine($"{"k",10}{"descida",14}{"bola pesada",14}{"Nesterov",14}{"ganho",10}");

        foreach (var k in new[] { 100.0, 1000.0, 10000.0 })
        {
            var funcao = Funcao.Quadratica(k);
            var inicio = Partida(funcao.Dimensao);

            var descida = Descida.Fixa(funcao, inicio, Descida.PassoOtimo(1, k), 1e-8);
            var (passo, peso) = Momento.Parametros(1, k);
            var pesada = Momento.BolaPesada(funcao, inicio, passo, peso, 1e-8);
            var (passoN, pesoN) = Momento.ParametrosNesterov(1, k);
            var nesterov = Momento.Nesterov(funcao, inicio, passoN, pesoN, 1e-8);

            Console.WriteLine($"{k,10:N0}{descida.AvaliacoesDeGradiente,14:N0}{pesada.AvaliacoesDeGradiente,14:N0}" +
                              $"{nesterov.AvaliacoesDeGradiente,14:N0}{(double)descida.AvaliacoesDeGradiente / pesada.AvaliacoesDeGradiente,10:N1}");
        }

        Console.WriteLine();
        Console.WriteLine("o Nesterov gasta DOIS gradientes por iteracao nesta implementacao, porque ele");
        Console.WriteLine("olha a inclinacao no ponto para onde o momento vai levar e ainda confere a");
        Console.WriteLine("parada no ponto de verdade. Contar iteracao esconderia esse custo");
        Console.WriteLine();
        Console.WriteLine("os dois crescem com a RAIZ de k e a descida cresce com k, entao o ganho na");
        Console.WriteLine("ultima coluna nao e uma constante: ele mesmo cresce com a raiz. Quanto pior");
        Console.WriteLine("o problema, mais o momento vale");
        Console.WriteLine();
        Console.WriteLine("medido em gradientes, a bola pesada ganha do Nesterov na quadratica, e isso");
        Console.WriteLine("nao e surpresa: a quadratica e justamente o caso em que ela nao tem como");
        Console.WriteLine("falhar. O que o Nesterov compra nao aparece nesta tabela");
        Console.WriteLine();
        Console.WriteLine("e os parametros dos dois NAO sao os mesmos. Rodar o Nesterov com os da bola");
        Console.WriteLine("pesada bate o limite de dois milhoes de iteracoes em todas as linhas: o peso");
        Console.WriteLine("dele e a raiz de k sem o quadrado, e o passo e 1/L sem o (1 + peso)");
        Console.WriteLine();
    }

    /// <summary>Newton: um passo na quadratica, convergencia quadratica no resto.</summary>
    private static void Newtons()
    {
        Console.WriteLine("== Newton: quantos digitos certos por iteracao ==");
        Console.WriteLine();
        Console.WriteLine("  os erros de Newton na Rosenbrock, saindo de (-1,2 ; 1):");

        var rosenbrock = Funcao.Rosenbrock();
        var erros = Newton.Erros(rosenbrock, new[] { -1.2, 1.0 });

        for (var i = 0; i < erros.Count; i++)
            Console.WriteLine($"    iteracao {i,2}   {erros[i],12:E3}");

        Console.WriteLine();
        Console.WriteLine($"  expoente medido: {Newton.ExpoenteMedido(erros),0:N2}");
        Console.WriteLine();
        Console.WriteLine("o erro novo e proporcional ao erro velho ao QUADRADO, entao o numero de");
        Console.WriteLine("digitos certos dobra a cada iteracao: tres viram seis, seis viram doze");
        Console.WriteLine();
        Console.WriteLine("a descida de gradiente ganha um numero FIXO de digitos por iteracao, e esse");
        Console.WriteLine("numero depende do condicionamento. E outra categoria de convergencia");
        Console.WriteLine();

        Console.WriteLine($"  e o custo, na Rosenbrock, de (-1,2 ; 1) ate o gradiente abaixo de 1e-8:");
        Console.WriteLine($"  {"metodo",-26}{"iteracoes",12}{"gradientes",13}{"avaliacoes de f",17}");

        var inicio = new[] { -1.2, 1.0 };
        var comBusca = Newton.ComBusca(rosenbrock, inicio);
        var puro = Newton.Puro(rosenbrock, inicio);
        var gradiente = Busca.Descida(rosenbrock, inicio);

        foreach (var (nome, c) in new[] { ("Newton puro", puro), ("Newton com busca", comBusca), ("gradiente com busca", gradiente) })
            Console.WriteLine($"  {nome,-26}{c.Iteracoes,12:N0}{c.AvaliacoesDeGradiente,13:N0}{c.AvaliacoesDeF,17:N0}");

        Console.WriteLine();
        Console.WriteLine("  a Rosenbrock nao tem condicionamento constante: a curvatura muda de ponto");
        Console.WriteLine("  para ponto, entao nenhum passo fixo serve para o caminho inteiro, e a");
        Console.WriteLine("  busca de linha deixa de ser conforto e vira necessidade");
        Console.WriteLine();
    }

    /// <summary>O preco de nao ter derivada.</summary>
    private static void SemDerivada()
    {
        Console.WriteLine("== o preco de nao ter derivada ==");
        Console.WriteLine();
        Console.WriteLine($"{"funcao",-24}{"Nelder-Mead",14}{"gradiente",12}{"Newton",10}{"distancia NM",15}");

        foreach (var funcao in Funcao.Todas())
        {
            var inicio = Partida(funcao.Dimensao);
            inicio[0] = -1.2;

            var nelder = Nelder.Minimizar(funcao, inicio);
            var gradiente = Busca.Descida(funcao, inicio);
            var newton = Newton.ComBusca(funcao, inicio);

            var custoGradiente = gradiente.Custo(funcao.Dimensao);
            var custoNewton = newton.Custo(funcao.Dimensao);

            Console.WriteLine($"{funcao.Nome,-24}{nelder.AvaliacoesDeF,14:N0}{custoGradiente,12:N0}" +
                              $"{custoNewton,10:N0}{nelder.Distancia,15:E2}");
        }

        Console.WriteLine();
        Console.WriteLine("a conta e em avaliacoes da funcao, contando um gradiente como n avaliacoes,");
        Console.WriteLine("que e o que ele custaria se fosse calculado por diferenca finita");
        Console.WriteLine();
        Console.WriteLine("e isso me corrigiu: eu esperava que o simplex perdesse em todas as linhas,");
        Console.WriteLine("e ele ganha da descida de gradiente em TRES das quatro. A unica em que");
        Console.WriteLine("perde e a tigela redonda, onde a descida resolve em uma iteracao");
        Console.WriteLine();
        Console.WriteLine("usar mais informacao nao e o mesmo que usar bem a informacao. A descida");
        Console.WriteLine("de gradiente usa a derivada e desperdica: ela ziguezagueia num vale");
        Console.WriteLine("comprido, e o simplex, que so compara valores, nao ziguezagueia");
        Console.WriteLine();
        Console.WriteLine("contra Newton, que usa a CURVATURA, o simplex perde em todas. A ordem");
        Console.WriteLine("que aparece e essa: curvatura ganha de comparacao, e comparacao ganha de");
        Console.WriteLine("inclinacao mal usada");
        Console.WriteLine();
    }
}
