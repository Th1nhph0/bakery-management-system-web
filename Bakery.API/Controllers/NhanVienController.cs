using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NhanVienController : ControllerBase
    {
        private readonly BakeryManagementDbContext _context;

        public NhanVienController(BakeryManagementDbContext context)
        {
            _context = context;
        }

        [HttpGet("HienThiDanhSachNhanVien")]
        public async Task<IActionResult> LayDanhSachNhanVien()
        {
            var list = await _context.NhanViens.ToListAsync();
            return Ok(list);
        }

        [HttpPost("ThemNhanVienMoi")]
        public async Task<IActionResult> TaoNhanVienMoi([FromBody] NhanVienDTO request)
        {
            if (request.RoleNguoiSua != "Admin" && request.RoleNguoiSua != "Chủ quán" && request.RoleNguoiSua != "Quản trị web")
            {
                return StatusCode(403, new { Message = "⛔ Lỗi bảo mật: Bạn không có quyền khởi tạo tài khoản nhân sự!" });
            }
            try
            {
                // 1. Kiểm tra xem Email có bị trùng không (Rất quan trọng để không bị lỗi SQL)
                var emailTonTai = await _context.NhanViens.AnyAsync(nv => nv.Email == request.Email);
                if (emailTonTai)
                {
                    return BadRequest(new { Message = "Email này đã có người xài rồi ông ơi!" });
                }

                // 2. Chuyển từ DTO sang Model thực tế của Database
                var nvMoi = new NhanVien
                {
                    TenNhanVien = request.TenNhanVien,
                    Sdt = request.Sdt,
                    Email = request.Email,
                    MatKhau = request.MatKhau,
                    ChucVu = request.ChucVu,
                    NgayVaoLam = DateTime.Now
                };

                // 3. Đẩy xuống SQL Server
                _context.NhanViens.Add(nvMoi);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Tạo nhân viên thành công rực rỡ!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống C#: " + ex.Message });
            }
        }

        [HttpPut("CapNhatNhanVien/{id}")]
        public async Task<IActionResult> CapNhatNhanVien(int id, [FromBody] NhanVienDTO nvUpdate)
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            // Cập nhật các thông tin cơ bản
            nv.TenNhanVien = nvUpdate.TenNhanVien;
            nv.Sdt = nvUpdate.Sdt;
            nv.Email = nvUpdate.Email;

            // Nếu là Sếp (Admin/Chủ quán) thì mới cho phép cập nhật Chức vụ
            // (Bên Frontend mình đã chặn rồi, nhưng C# cứ chặn thêm cho chắc)
            if (!string.IsNullOrEmpty(nvUpdate.ChucVu))
            {
                nv.ChucVu = nvUpdate.ChucVu;
            }

            /// 🔥 LOGIC MẬT KHẨU: CÓ PHÂN QUYỀN VÀ XÁC THỰC
            if (!string.IsNullOrEmpty(nvUpdate.MatKhau))
            {
                // Nếu người đang sửa KHÔNG PHẢI LÀ SẾP (nghĩa là Nhân viên tự sửa)
                if (nvUpdate.RoleNguoiSua != "Admin" && nvUpdate.RoleNguoiSua != "Quản trị web" && nvUpdate.RoleNguoiSua != "Chủ quán")
                {
                    // Bắt buộc kiểm tra Mật khẩu cũ (Nhớ check kỹ tên cột nv.MatKhau hay nv.Mat_Khau trong DB nha)
                    if (nv.MatKhau != nvUpdate.MatKhauCu)
                    {
                        return BadRequest(new { Message = "Mật khẩu cũ không chính xác! Không thể đổi mật khẩu." });
                    }
                }

                // Vượt qua kiểm tra (hoặc là Sếp reset) -> Cho phép đổi Pass
                nv.MatKhau = nvUpdate.MatKhau;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Cập nhật thành công!" });
        }

        [HttpDelete("XoaNhanVien/{id}")]
        public async Task<IActionResult> XoaNhanVien(int id)
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            try
            {
                _context.NhanViens.Remove(nv);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Đã xóa nhân viên thành công!" });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // Bắt lỗi dính khóa ngoại SQL Server
                return BadRequest(new { Message = "Không thể xóa! Nhân viên này đang có dữ liệu liên quan (Ví dụ: Đã từng lập đơn hàng)." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("LayNhanVien/{id}")]
        public async Task<IActionResult> LayNhanVienTheoId(int id)
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();
            return Ok(nv);
        }

    }
}