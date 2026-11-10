using Simulador.Dados.Dominio;

namespace Simulador.Dados.Contratos;

/// <summary>Identificadores derivados que outros módulos precisam ler, sem depender do domínio.</summary>
public static class IdentificadoresDeDados
{
    /// <summary>O recurso dono de uma nota de negociação; a própria nota, se não for derivada.</summary>
    public static string RecursoDaNota(string notaId) => IdsDerivados.RecursoDaNota(notaId) ?? notaId;
}
