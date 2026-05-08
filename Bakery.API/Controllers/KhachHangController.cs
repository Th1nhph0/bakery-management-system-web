using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class KhachHangController : ControllerBase
{
    private readonly KhachHangService _service;

    public KhachHangController(KhachHangService service) => _service = service;

    [HttpGet("TimKiem/{sdt}")]
    public async Task<IActionResult> GetBySdt(string sdt)
    {
        var kh = await _service.TimKhachHangTheoSDTAsync(sdt);
        if (kh == null) return NotFound(new { Message = "Khách hàng mới" });
        return Ok(kh);
    }

    [HttpPost]
    public async Task<IActionResult> Create(KhachHang kh)
    {
        var result = await _service.ThemKhachHangMoiAsync(kh);
        return Ok(result);
    }
}