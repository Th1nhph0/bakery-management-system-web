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

        public class UpdateStatusRequest
        {
            public string TrangThaiMoi { get; set; } = null!;
            public string TenNhanVien { get; set; } = null!;
        }

        [HttpGet("XemToanBoDonHang")]
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

        [HttpPost("TaoDonHang")]
        public async Task<IActionResult> TaoDonHangMoi([FromBody] TaoDonHangDTO request)
        {
            // Kiểm tra dữ liệu bắt buộc
            if (request == null)
            {
                return BadRequest(new { Message = "Dữ liệu đơn hàng không hợp lệ!" });
            }

            if (string.IsNullOrWhiteSpace(request.Ten_Nguoi_Nhan) || 
                string.IsNullOrWhiteSpace(request.SDT_Nguoi_Nhan) || 
                string.IsNullOrWhiteSpace(request.Dia_Chi_Giao))
            {
                return BadRequest(new { Message = "Vui lòng nhập đầy đủ thông tin người nhận (tên, SĐT, địa chỉ)!" });
            }

            if (request.ChiTietGioHang == null || request.ChiTietGioHang.Count == 0)
            {
                return BadRequest(new { Message = "Giỏ hàng không được trống!" });
            }

            // 1. Tạo đối tượng Đơn Hàng mới (Entity)
            var donHangMoi = new DonHang
            {
                KhachHangId = request.Khach_Hang_ID,
                NhanVienId = request.Nhan_Vien_ID,
                KhuyenMaiId = request.Khuyen_Mai_ID,
                TenNguoiNhan = request.Ten_Nguoi_Nhan,
                SdtNguoiNhan = request.SDT_Nguoi_Nhan,
                DiaChiGiao = request.Dia_Chi_Giao,
                NgayDatHang = DateTime.Now,
                TrangThai = "Chờ xử lý",
                TongTien = 0 // Tạm thời để bằng 0, sẽ cộng dồn sau
            };

            try
            {
                _context.DonHangs.Add(donHangMoi);
                await _context.SaveChangesAsync(); // SQL sẽ "hét" lên ở đây nếu truyền ID tào lao
            }
            catch (DbUpdateException ex)
            {
                // Bắt đúng lỗi của SQL và trả về 400 Bad Request cho Frontend
                return BadRequest(new { Message = "Lỗi dữ liệu: Mã khách hàng, nhân viên hoặc khuyến mãi không tồn tại trong hệ thống!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi máy chủ: " + ex.Message });
            } // Lưu để lấy được mã Đơn Hàng ID

            decimal tongTienDonHang = 0;

            // 2. Xử lý từng món trong giỏ hàng
            foreach (var item in request.ChiTietGioHang)
            {
                // QUAN TRỌNG: Tự tìm giá gốc trong Database dựa vào ID sản phẩm
                var sanPhamTonTai = await _context.SanPhams.FindAsync(item.SanPham_ID);

                if (sanPhamTonTai != null)
                {
                    var chiTiet = new ChiTietDonHang
                    {
                        DonHangId = donHangMoi.DonHangId,
                        SanPhamId = item.SanPham_ID,
                        SoLuong = item.So_Luong,
                        // Gán Đơn Giá bằng giá thực tế trong Database
                        DonGia = sanPhamTonTai.DonGiaBan,
                    };

                    _context.ChiTietDonHangs.Add(chiTiet);

                    // Cộng dồn tiền để lát nữa cập nhật lại Tổng Tiền Đơn Hàng
                    tongTienDonHang += ((sanPhamTonTai.DonGiaBan ?? 0) * item.So_Luong);
                }
            }

            // 3. Cập nhật lại tổng tiền thực tế
            donHangMoi.TongTien = tongTienDonHang;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tạo đơn hàng thành công!", DonHangId = donHangMoi.DonHangId });
        }



        [HttpPut("CapNhatTrangThaiDonHang{id}")]
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

        [HttpGet("KiemTraDonHang/{donHangId}")]
        public async Task<IActionResult> KiemTraDonCustom(int donHangId)
        {
            try
            {
                // BƯỚC 1: Kiểm tra xem Đơn Hàng này có thực sự tồn tại trong hệ thống không
                // Dùng AnyAsync cho nhẹ server, nó chỉ trả về true/false chứ không lôi cả dòng data lên
                var donHangTonTai = await _context.DonHangs.AnyAsync(d => d.DonHangId == donHangId);

                if (!donHangTonTai)
                {
                    // Nếu không tồn tại, chặn ngay từ cửa và báo lỗi 404
                    return NotFound(new { Message = $"Không tìm thấy đơn hàng nào có mã số {donHangId}." });
                }

                // BƯỚC 2: Nếu đơn hàng CÓ TỒN TẠI, mới tiếp tục kiểm tra xem có phải đơn Custom không
                var thongTinCustom = await _context.DonBanhCustoms
                    .FirstOrDefaultAsync(c => c.DonHangId == donHangId);

                // Nếu tìm không thấy trong bảng Custom -> Chắc chắn đây là đơn mua bánh sẵn
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

        [HttpGet("XuatHoaDonXMLDonHang/{id}")]
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
    }
}