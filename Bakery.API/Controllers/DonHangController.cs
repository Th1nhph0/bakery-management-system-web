using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DonHangController : ControllerBase
    {
        private readonly BakeryManagementDbContext _context;
        private readonly DonHangService _donHangService;
        public DonHangController(DonHangService donHangService, BakeryManagementDbContext context)
        {
            _context = context;
            _donHangService = donHangService;
        }

        [HttpPost("TaoDonHang")]
        public async Task<IActionResult> TaoDonHangMoi([FromBody] TaoDonHangRequest request)
        {
            try
            {
                if (request.ChiTietGioHang == null || request.ChiTietGioHang.Count == 0)
                {
                    return BadRequest("Giỏ hàng không được để trống!");
                }

                int newOrderId = await _donHangService.TaoDonHangMoiAsync(request);
                return Ok(new { Message = "Đặt hàng thành công!", DonHangID = newOrderId });
            }
            catch (Exception ex)
            {
                // Nắm bắt cái lỗi THROW từ SQL Server (như hết hàng, lỗi transaction)
                return StatusCode(500, new { Message = "Lỗi khi tạo đơn hàng: " + ex.Message });
            }
        }
        [HttpGet("XuatHoaDonXML/{id}")]
        public async Task<IActionResult> XuatHoaDonXML(int id)
        {
            try
            {
                var xmlString = await _donHangService.XuatHoaDonXmlAsync(id);

                if (xmlString == null)
                {
                    return NotFound(new { Message = "Không tìm thấy đơn hàng số " + id });
                }

                // Chuyển chuỗi XML thành mảng Byte để ép trình duyệt tải file về
                byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(xmlString);
                string fileName = $"HoaDon_So_{id}.xml";

                // Trả về định dạng File (application/xml)
                return File(fileBytes, "application/xml", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi tạo XML: " + ex.Message });
            }
        }
        // GET: api/DonHang (Admin xem tất cả đơn)
        [HttpGet]
        // GET: api/DonHang (Admin xem tất cả đơn)
        [HttpGet]
        public async Task<IActionResult> GetAllDonHang()
        {
            // Kết hợp LINQ để kiểm tra xem đơn hàng có nằm trong bảng Custom hay không ngay lúc query
            var dsDonHang = await _context.DonHangs
                                  .OrderByDescending(d => d.NgayDatHang)
                                  .Select(dh => new
                                  {
                                      // 1. Lấy toàn bộ thông tin gốc của đơn hàng
                                      dh.DonHangId,
                                      dh.MaDhHienThi,
                                      dh.TenNguoiNhan,
                                      dh.SdtNguoiNhan,
                                      dh.DiaChiGiao,
                                      dh.NgayDatHang,
                                      dh.TrangThai,
                                      dh.TongTien,
                                      dh.SoTienGiam,
                                      dh.NguoiCapNhat,
                                      dh.NgayCapNhat,

                                      // 2. VŨ KHÍ TỐI ƯU: Thêm một cột ảo (Cờ) kiểm tra Custom
                                      LaDonCustom = _context.DonBanhCustoms.Any(c => c.DonHangId == dh.DonHangId)
                                  })
                                  .ToListAsync();

            return Ok(dsDonHang);
        }
        // PUT: api/DonHang/5/Status (Admin và Staff cập nhật trạng thái đơn)
        //[Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id}/Status")]
        public async Task<IActionResult> CapNhatTrangThai(int id, [FromBody] UpdateStatusRequest request)
        {
            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null) return NotFound();

            donHang.TrangThai = request.TrangThaiMoi;
            donHang.NguoiCapNhat = request.TenNhanVien; // Lưu tên người vừa thao tác
            donHang.NgayCapNhat = DateTime.Now; // Lưu luôn giờ giấc cho chắc

            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Nhân viên {request.TenNhanVien} đã cập nhật trạng thái!" });
        }
        public class UpdateStatusRequest
        {
            public string TrangThaiMoi { get; set; } = null!;
            public string TenNhanVien { get; set; } = null!;
        }
        // GET: api/DonBanhCustom/TheoDonHang/5
        [HttpGet("TheoDonHang/{donHangId}")]
        public async Task<IActionResult> KiemTraDonCustom(int donHangId)
        {
            try
            {
                // Tìm trong bảng DonBanhCustom xem có cái nào mang Don_Hang_ID này không
                var thongTinCustom = await _context.DonBanhCustoms
                                           .FirstOrDefaultAsync(c => c.DonHangId == donHangId);

                // Nếu tìm không thấy -> Báo cho Frontend biết đây là đơn mua bánh sẵn
                if (thongTinCustom == null)
                {
                    return Ok(new { LaDonCustom = false, Message = "Đây là đơn bánh có sẵn bình thường." });
                }

                // Nếu tìm thấy -> Báo là đơn Custom và trả về chi tiết (màu sắc, size, lời chúc...)
                return Ok(new
                {
                    LaDonCustom = true,
                    Data = thongTinCustom
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}