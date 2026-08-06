using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.Services;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/catalogo")]
public class CatalogoController : ControllerBase
{
    private readonly IProductoService _productoService;

    public CatalogoController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    /// <summary>
    /// Obtiene el catálogo paginado de productos activos disponible para clientes.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> ObtenerCatalogo(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] int? categoriaId = null)
    {
        var (items, total) = await _productoService
            .ObtenerCatalogoPaginadoAsync(pagina, tamanoPagina, categoriaId);

        return Ok(new
        {
            data = items,
            pagina,
            tamanoPagina,
            total,
            totalPaginas = (int)Math.Ceiling((double)total / tamanoPagina)
        });
    }
}
