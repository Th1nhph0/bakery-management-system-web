using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DonBanhCustomController : ControllerBase
    {
        private readonly BakeryManagementDbContext _context;
        private readonly DonBanhCustomService _service;

        // Tiêm Service vào Controller
        public DonBanhCustomController(DonBanhCustomService service, BakeryManagementDbContext context)
        {
            _service = service;
            _context = context;
        }

        [HttpGet("DanhSachDonBanhCustom")]
        public async Task<IActionResult> LayTatCaDonBanhCustom()
        {
            try
            {
                // Lấy toàn bộ đơn Custom (Nếu muốn xịn thì Include thêm bảng DonHang để biết ngày giờ giao)
                var danhSachCustom = await _context.DonBanhCustoms.ToListAsync();

                if (danhSachCustom.Count == 0)
                {
                    return Ok(new { Message = "Hôm nay xưởng bánh rảnh rỗi, chưa có đơn Custom nào!" });
                }

                return Ok(danhSachCustom);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi xưởng bánh: " + ex.Message });
            }
        }

        [HttpGet("XemThongTinDonBanhCustom/{donHangId}")]
        public async Task<IActionResult> GetChiTietCustom(int donHangId)
        {
            try
            {
                var thongTinCustom = await _context.DonBanhCustoms
                                           .FirstOrDefaultAsync(c => c.DonHangId == donHangId);

                // Nếu không tìm thấy (lỡ truyền nhầm ID của đơn bánh ngọt bình thường)
                if (thongTinCustom == null)
                {
                    return NotFound(new { Message = "Đơn hàng này không có thông tin yêu cầu Custom!" });
                }

                // Trả về toàn bộ chi tiết: Nhan_Banh, Mau_Sac_Chu_Dao, Ghi_Chu...
                return Ok(thongTinCustom);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("TaoDonHangDatBanhCustom")]
        public async Task<IActionResult> DatBanhCustom([FromBody] DonBanhCustomRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { Success = false, Message = "Dữ liệu đặt bánh custom không hợp lệ!" });
                }

                if (string.IsNullOrWhiteSpace(request.Kich_Thuoc))
                {
                    return BadRequest(new { Success = false, Message = "Vui lòng chọn kích thước bánh trước khi đặt." });
                }

                if (string.IsNullOrWhiteSpace(request.Ten_Nguoi_Nhan)
                    || string.IsNullOrWhiteSpace(request.SDT_Nguoi_Nhan)
                    || string.IsNullOrWhiteSpace(request.Dia_Chi_Giao)
                    || string.IsNullOrWhiteSpace(request.Mo_Ta_Yeu_Cau)
                    || string.IsNullOrWhiteSpace(request.Mau_Sac))
                {
                    return BadRequest(new { Success = false, Message = "Vui lòng nhập đầy đủ thông tin đặt bánh custom." });
                }

                if (request.Ngay_Lay_Banh == default)
                {
                    return BadRequest(new { Success = false, Message = "Vui lòng chọn ngày lấy bánh." });
                }

                // Gọi sang Service để lưu dữ liệu và lấy mã Đơn hàng (ID) về
                var id = await _service.LuuDonBanhCustomAsync(request);

                // Trả về JSON chuẩn format cho Frontend dễ đọc
                return Ok(new
                {
                    Success = true,
                    Message = $"Đặt thành công mã đơn #{id}.",
                    DonHangID = id
                });
            }
            catch (Exception ex)
            {
                // Trả về mã 500 kèm chi tiết lỗi nếu có sự cố
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        [HttpPut("CapNhatDonBanhCustom/{donHangId}")]
        public async Task<IActionResult> CapNhatDonCustom(int donHangId, [FromBody] UpdateDonBanhCustom request)
        {
            // Tìm đơn custom CHÍNH XÁC dựa vào mã Đơn Hàng (donHangId)
            var donCustomTonTai = await _context.DonBanhCustoms.FirstOrDefaultAsync(c => c.DonHangId == donHangId);

            if (donCustomTonTai == null)
            {
                return NotFound(new { Message = $"Không tìm thấy đơn bánh thiết kế nào thuộc Mã hóa đơn {donHangId}!" });
            }

            // Gán dữ liệu từ DTO (Gói hàng siêu gọn) vào Database
            donCustomTonTai.LoaiYeuCau = request.Loai_Yeu_Cau;
            donCustomTonTai.KichThuocSoLuong = request.Kich_Thuoc_So_Luong;
            donCustomTonTai.NhanBanh = request.Nhan_Banh;
            donCustomTonTai.MauSacChuDao = request.Mau_Sac;
            donCustomTonTai.GhiChu = request.Ghi_Chu;
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Đã cập nhật chi tiết bánh thiết kế thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
