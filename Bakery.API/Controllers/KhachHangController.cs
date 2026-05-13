using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class KhachHangController : ControllerBase
{
    private readonly KhachHangService _service;
    private readonly BakeryManagementDbContext _context;


    public KhachHangController(KhachHangService service, BakeryManagementDbContext context) { 
        _context = context;
        _service = service; 
    }


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
    // GET: api/KhachHang (Lấy toàn bộ danh sách khách hàng)
    [HttpGet]
    public async Task<IActionResult> GetAllKhachHang()
    {
        return Ok(await _context.KhachHangs.ToListAsync());
    }

    // PUT: api/KhachHang/{id} (Cập nhật thông tin khách)
    [HttpPut("{id}")]
    public async Task<IActionResult> CapNhatKhachHang(int id, [FromBody] KhachHang khachHangCapNhat)
    {
        var kh = await _context.KhachHangs.FindAsync(id);
        if (kh == null) return NotFound(new { Message = "Không tìm thấy khách hàng!" });

        kh.TenKhachHang = khachHangCapNhat.TenKhachHang;
        kh.Sdt = khachHangCapNhat.Sdt;
        kh.DiaChi = khachHangCapNhat.DiaChi;
        kh.Email = khachHangCapNhat.Email;

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Cập nhật thành công!" });
    }

    // DELETE: api/KhachHang/{id} (Xóa khách hàng)
    [HttpDelete("{id}")]
    public async Task<IActionResult> XoaKhachHang(int id)
    {
        var kh = await _context.KhachHangs.FindAsync(id);
        if (kh == null) return NotFound();

        _context.KhachHangs.Remove(kh);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Đã xóa khách hàng!" });
    }
}