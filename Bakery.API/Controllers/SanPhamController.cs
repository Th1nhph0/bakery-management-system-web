using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;


namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SanPhamController : ControllerBase
    {
        private readonly BakeryManagementDbContext _context;
        private readonly SanPhamService _sanPhamService;

        public SanPhamController(SanPhamService sanPhamService, BakeryManagementDbContext context)
        {
            _context = context;
            _sanPhamService = sanPhamService; 
        }
        [HttpGet("HienThiDanhSachSanPham")]
        public async Task<IActionResult> GetDanhSachSanPham()
        {
            // Gọi xuống tầng Service để lấy data
            var data = await _sanPhamService.GetAllSanPhamAsync();

            // Trả về dữ liệu dạng JSON với mã trạng thái 200 (Ok)
            return Ok(data);
        }

        [HttpGet("XemPhanLoaiSanPham")]
        public async Task<IActionResult> GetDanhSachDanhMuc()
        {
            try
            {
                // Gọi thông qua Service, Controller không cần quan tâm _context là gì nữa
                var danhMuc = await _sanPhamService.GetDanhSachDanhMucAsync();
                return Ok(danhMuc);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi: " + ex.Message);
            }
        }


        [HttpPost("ThemSanPhamMoi")]
        public async Task<IActionResult> ThemSanPhamMoi([FromBody] SanPhamUpdateDTO dto)
        {
            // 1. Ép chức vụ gửi lên thành chữ thường hết để chống lỗi phân biệt Hoa/Thường
            var role = dto.RoleNguoiSua?.Trim().ToLower() ?? "";

            // 2. BAO LÔ TOÀN BỘ các chức danh của Sếp
            if (role != "admin" && role != "chủ quán" && role != "quản trị web" && role != "quản trị viên")
            {
                return StatusCode(403, new { Message = "⛔ Lỗi bảo mật: Nhân viên không được quyền tạo sản phẩm mới!" });
            }

            // 3. Kiểm tra dữ liệu đầu vào (Tên bánh, Giá bán phải có)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 2. Map dữ liệu từ DTO sang thực thể Database Model gốc
                var sp = new SanPham
                {
                    TenSanPham = dto.TenSanPham,
                    DonGiaBan = dto.GiaBan,
                    SoLuongTon = dto.SoLuong,
                    PhanLoai = dto.PhanLoai, 
                    MoTa = dto.MoTa,
                    HinhAnh = dto.HinhAnh
                };

                // 3. Tiến hành lưu vào SQL Server
                _context.SanPhams.Add(sp);
                await _context.SaveChangesAsync();

                return Ok(new { Success = true, Message = "Thêm sản phẩm mới thành công!", Data = sp });
            }
            catch (Exception ex)
            {
                // Bắt các lỗi liên quan đến Database (như tràn bộ nhớ cột, sai khóa ngoại...)
                return StatusCode(500, new { Success = false, Message = "Lỗi Database: " + ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("TimKiemSanPham")]
        public async Task<IActionResult> TimKiemSanPham([FromQuery] string tuKhoa)
        {
            try
            {
                var data = await _sanPhamService.TimKiemSanPhamAsync(tuKhoa);

                if (data == null || data.Count == 0)
                {
                    return NotFound(new { Success = false, Message = "Không tìm thấy sản phẩm nào khớp với từ khóa!" });
                }

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("CapNhatSanPham/{id}")]
        public async Task<IActionResult> CapNhatSanPham(int id, [FromBody] SanPhamUpdateDTO spUpdate)
        {
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return NotFound(new { Message = "Không tìm thấy sản phẩm!" });

            // 🔥 VÁ LỖI PHÂN QUYỀN: Ép về chữ thường sạch sẽ để tránh lỗi lệch pha Hoa/Thường giữa C# và JS
            var role = spUpdate.RoleNguoiSua?.Trim().ToLower() ?? "";

            if (role != "admin" && role != "chủ quán" && role != "quản trị web" && role != "quản trị viên")
            {
                // Nếu là nhân viên -> CHỈ cập nhật duy nhất Số lượng tồn kho
                sp.SoLuongTon = spUpdate.SoLuong;
            }
            else
            {
                // Nếu là Sếp -> Cho phép cập nhật tất tay mọi thông số của bánh
                sp.TenSanPham = spUpdate.TenSanPham;
                sp.DonGiaBan = spUpdate.GiaBan;
                sp.SoLuongTon = spUpdate.SoLuong;
                sp.PhanLoai = spUpdate.PhanLoai;
                sp.MoTa = spUpdate.MoTa ?? "";
                if (!string.IsNullOrEmpty(spUpdate.HinhAnh))
                {
                    sp.HinhAnh = spUpdate.HinhAnh;
                }
            }

            try
            {
                // 4. Lưu lại thay đổi vào SQL Server
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Cập nhật thông tin bánh thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi lưu dữ liệu", Detail = ex.Message });
            }
        }

        [HttpDelete("XoaSanPham/{id}")]
        public async Task<IActionResult> XoaSanPham(int id)
        {
            try
            {
                var result = await _sanPhamService.XoaSanPhamAsync(id);
                if (!result)
                {
                    return NotFound(new { Success = false, Message = "Không tìm thấy sản phẩm để xóa!" });
                }

                return Ok(new { Success = true, Message = "Đã xóa sản phẩm thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi xóa dữ liệu: " + ex.Message });
            }
        }
        [HttpGet("LaySanPham/{id}")]
        public async Task<IActionResult> LaySanPham(int id)
        {
            var sp = await _context.SanPhams.FindAsync(id);

            if (sp == null)
            {
                return NotFound(new { Message = "Không tìm thấy sản phẩm này!" });
            }

            return Ok(sp); 
        }
    }
}