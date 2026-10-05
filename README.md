# otimizacao

Otimização sem restrições do zero em C# e .NET 8: descida de gradiente, busca de
linha de Armijo, momento de Polyak, Nesterov, Newton e o simplex de Nelder-Mead.
Todo problema vem com o **mínimo conhecido** junto, e é ele que julga.

```
$ dotnet medidor.dll condicionamento

         k     descida   razao teorica   razao medida    momento   Newton
         1           1        0.000000            NaN          1        1
        10          65        0.818182       0.818182         61        1
       100         651        0.980198       0.980193        217        1
     1,000       6,506        0.998002       0.997687        693        1
    10,000      65,054        0.999800       0.999414      2,194        1
```

A primeira coluna de números é o custo da descida de gradiente, com o **melhor
passo possível**, para encolher o erro por um milhão. Ela cresce **proporcional a
k**: dez vezes mais mal condicionado, dez vezes mais trabalho.

A coluna do momento cresce com a **raiz** de k, e é a mesma função, o mesmo ponto
de partida e uma linha a mais de conta.

E a do Newton não cresce. Ela é **um**, sempre.

## A razão medida não vem de teoria nenhuma

O código da descida não sabe o que é condicionamento: ele soma, multiplica e anda.
A razão com que o erro encolhe é medida depois, dividindo distâncias, e ela bate
com `(k-1)/(k+1)` até a sexta casa.

Esse é o ponto do repositório inteiro. "A descida de gradiente é lenta em
problemas mal condicionados" é uma frase; `0,980193` contra `0,980198` é um
número.

## Newton não enxerga o condicionamento

```csharp
var direcao = Vetor.Resolver(funcao.Hessiana(x), Vetor.Vezes(-1, g));
x = Vetor.Mais(x, direcao);
```

Numa quadrática, a parábola que Newton ajusta não é aproximação: é a própria
função. Ele vai direto para o fundo dela, de qualquer ponto de partida, com `k`
igual a um ou a um milhão.

O preço é a hessiana: resolver o sistema custa `n^3`, enquanto um passo de
gradiente custa `n`. É por isso que os métodos do meio do caminho, que aproximam
a curvatura em vez de calcular, existem.

## O limite de passo, medido sem saber o que é L

```
$ dotnet medidor.dll passo

funcao                          medido           2/L   passo otimo           1/L
quadratica k=1              2.000E+000    2.000E+000    1.000E+000    1.000E+000
quadratica k=10             2.001E-001    2.000E-001    1.818E-001    1.000E-001
quadratica k=100            2.001E-002    2.000E-002    1.980E-002    1.000E-002
quadratica k=1,000          2.001E-003    2.000E-003    1.998E-003    1.000E-003
```

A bisseção só roda a descida e olha quem explode, e o limite que ela encontra é
`2/L` nas quatro linhas.

E o melhor passo não é o maior que converge, nem `1/L`, que é o palpite natural:
é `2/(L + m)`, que equilibra o encolhimento na direção mais curva e na mais
plana. Nenhum outro passo chega no mínimo em menos iterações, e um teste tenta
cinco deles.

## Newton na Rosenbrock: os dígitos certos dobram

```
$ dotnet medidor.dll newton

    iteracao  0     2.200E+000
    iteracao  1     2.208E+000
    iteracao  2     4.182E+000
    iteracao  3     4.796E-001
    iteracao  4     5.597E-002
    iteracao  5     9.625E-006
    iteracao  6     1.853E-011
    iteracao  7     0.000E+000

  expoente medido: 2.14
```

Olhe as três primeiras linhas: Newton **piora** antes de melhorar, e na iteração
dois está a quase o dobro da distância em que começou. Ele vai para o fundo da
parábola ajustada, e longe do mínimo essa parábola não parece com a função.

Depois disso, o erro novo é proporcional ao erro velho **ao quadrado**: `5,6e-2`
vira `9,6e-6`, que vira `1,9e-11`, que vira zero. O número de dígitos certos
dobra a cada iteração, enquanto a descida de gradiente ganha um número **fixo** de
dígitos por iteração.

```
  metodo                       iteracoes   gradientes  avaliacoes de f
  Newton puro                          6            7                1
  Newton com busca                    21           22               49
  gradiente com busca             20,790       20,791           62,378
```

## O que as medidas me corrigiram

**Eu esperava que o simplex sem derivada perdesse em todas as linhas, e ele ganha
da descida de gradiente em três das quatro funções.** Em avaliações da função, na
Rosenbrock, são 270 contra 103.960: um fator de 385. Usar mais informação não é o
mesmo que usar bem a informação, e a descida de gradiente usa a derivada e
desperdiça, ziguezagueando num vale comprido. O simplex, que só compara valores,
não ziguezagueia. Contra Newton ele perde em todas, e a ordem que aparece é essa:
curvatura ganha de comparação, e comparação ganha de inclinação mal usada.

```
$ dotnet medidor.dll semderivada

funcao                     Nelder-Mead   gradiente    Newton   distancia NM
quadratica k=1                   1,998          22        22      5.31E-011
quadratica k=100                 3,068      11,026        22      7.99E-011
quadratica torta k=100             201       3,772         6      2.26E-012
Rosenbrock                         270     103,960        93      3.20E-011
```

**Os parâmetros do Nesterov não são os da bola pesada, e rodar com os errados não
aparece como erro: aparece como lentidão.** Com o peso ao quadrado e o passo
`(1 + peso)/L`, o Nesterov simplesmente não converge, e a tabela bateu o limite de
dois milhões de iterações em todas as linhas. O peso dele é a raiz de `k` **sem o
quadrado**, e o passo é `1/L` **sem** o `(1 + peso)`. Um teste prende os dois
casos, o que converge e o que não converge.

**Na quadrática, a diferença centrada é exata, e isso explicou um número que
parecia errado.** A busca pelo melhor passo da conferência de gradiente devolvia
`1e-1`, o maior passo testado, o que contraria a ideia de que existe um ponto
ótimo no meio. O motivo é que o erro da centrada começa na terceira derivada, e
num polinômio de grau dois ela vale zero: ali só sobra arredondamento, e passo
maior sempre melhora. A Rosenbrock, que tem termo de grau quatro, é a única linha
com os dois erros de verdade, e o melhor passo dela cai em `1e-6`, perto da raiz
cúbica da precisão da máquina, que é onde a teoria diz.

**Newton puro chega em menos iterações que Newton com busca de linha**, nesta
partida. A busca é seguro, e seguro custa: ela recusa o passo inteiro em
iterações que, neste caminho, teriam dado certo. O que ela compra aparece em
outro lugar, na sela, onde a direção de Newton aponta para **cima** e sem a busca
o método iria alegremente para um ponto crítico que não é mínimo.

**O expoente medido da convergência linear não dá um.** Ele se aproxima de um por
cima e devagar: com dez décadas de erro ele ainda marca `1,11`. Não é defeito da
medida, é a conta: o logaritmo de um erro dividido pelo logaritmo do anterior só
vira um quando os dois já são minúsculos. O que o número separa bem é a
**categoria**: `2,00` na mosca para Newton, descendo para um no resto.

## Por que o mínimo conhecido é o juiz

Método de otimização erra de um jeito silencioso. Ele sempre devolve um ponto, e
um ponto quase bom tem a mesma cara de um ponto bom: sem a resposta certa,
"convergiu" quer dizer apenas "parou".

E gradiente zero não basta como juiz, porque ponto de sela também tem gradiente
zero. Os testes conferem a vizinhança inteira de cada mínimo declarado, em todas
as direções dos eixos e nos dois sentidos.

Nenhuma medida aqui usa relógio. Todas são contagem de iterações ou de avaliações
da função, então a saída é a mesma no Linux, no Windows e no macOS, e a
integração contínua roda nos três.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Vetor.cs` | as contas de vetor e a eliminação com pivotamento |
| `Funcao.cs` | as funções objetivo, cada uma com o mínimo conhecido junto |
| `Gradiente.cs` | a conferência do gradiente por diferenças finitas |
| `Descida.cs` | a descida de gradiente, o passo ótimo e a razão medida |
| `Busca.cs` | a condição de Armijo e a descida que não precisa saber a curvatura |
| `Momento.cs` | a bola pesada de Polyak e o Nesterov |
| `Newton.cs` | o passo de Newton e o expoente de convergência medido |
| `Nelder.cs` | o simplex, que minimiza sem derivada nenhuma |

## Como rodar

```
dotnet test testes/Otimizacao.Testes/Otimizacao.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `gradiente`, `passo`, `condicionamento`, `momento`, `newton`,
`semderivada` e `tudo`.

## Licença

MIT.
