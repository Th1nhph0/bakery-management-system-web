using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // ==========================================
        // 1. API: XEM TOÀN BỘ ĐƠN HÀNG (TRANG CHỦ + GRID)
        // ==========================================
        [HttpGet("XemToanBoDonHang")]
        public async Task<IActionResult> GetAllDonHang()
        {
            var dsDonHang = await _context.DonHangs
                                          .OrderByDescending(d => d.NgayDatHang)
                                          .Select(dh => new
                                          {
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
                                              // Thêm cột ảo kiểm tra trạng thái Custom trực tiếp
                                              LaDonCustom = _context.DonBanhCustoms.Any(c => c.DonHangId == dh.DonHangId)
                                          })
                                          .ToListAsync();

            return Ok(dsDonHang);
        }

        // ==========================================
        // 2. API: TẠO MỚI ĐƠN HÀNG ĐÔNG BỘ (CHECK KHO + TẠO CUSTOM)
        // ==========================================
        [HttpPost("TaoDonHang")]
        public async Task<IActionResult> TaoDonHangMoi([FromBody] TaoDonHangRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Kích hoạt tính năng quản trị giao dịch kép phòng thủ DB
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // A. Tạo đối tượng Đơn Hàng mới hành chính chung
                var donHangMoi = new DonHang
                {
                    KhachHangId = request.Khach_Hang_ID == 0 ? null : request.Khach_Hang_ID, // 0 tương ứng Khách vãng lai
                    NhanVienId = request.Nhan_Vien_ID,
                    KhuyenMaiId = request.Khuyen_Mai_ID,
                    TenNguoiNhan = request.Ten_Nguoi_Nhan,
                    SdtNguoiNhan = request.SDT_Nguoi_Nhan,
                    DiaChiGiao = request.Dia_Chi_Giao,
                    NgayDatHang = DateTime.Now,
                    TrangThai = "Chờ xử lý",
                    TongTien = 0
                };

                _context.DonHangs.Add(donHangMoi);
                await _context.SaveChangesAsync(); // Lưu trước để lấy DonHangId tự tăng từ SQL

                // Khởi tạo gốc tiền (Nếu gạt Custom thì lấy giá Custom làm mốc, ngược lại bắt đầu từ 0)
                decimal tongTienDonHang = request.LaDonCustom ? (request.TongTienCustom ?? 0) : 0;

                // B. Xử lý lưu từng món bánh sẵn có đi kèm trong giỏ hàng
                if (request.ChiTietGioHang != null && request.ChiTietGioHang.Count > 0)
                {
                    foreach (var item in request.ChiTietGioHang)
                    {
                        var sanPhamTonTai = await _context.SanPhams.FindAsync(item.SanPham_ID);
                        if (sanPhamTonTai != null)
                        {
                            // 🔒 THUẬT TOÁN BẢO MẬT: Kiểm tra số lượng tồn thực tế trong kho
                            if (sanPhamTonTai.SoLuongTon < item.So_Luong)
                            {
                                return BadRequest(new { Message = $"Bánh [{sanPhamTonTai.TenSanPham}] trong kho chỉ còn {sanPhamTonTai.SoLuongTon} cái, không đủ để đặt!" });
                            }

                            // Khấu trừ số lượng hàng tồn live trong kho bánh
                            sanPhamTonTai.SoLuongTon -= item.So_Luong;

                            var chiTiet = new ChiTietDonHang
                            {
                                DonHangId = donHangMoi.DonHangId,
                                SanPhamId = item.SanPham_ID,
                                SoLuong = item.So_Luong,
                                DonGia = sanPhamTonTai.DonGiaBan,
                            };
                            _context.ChiTietDonHangs.Add(chiTiet);

                            // Cộng dồn thêm tiền sản phẩm sẵn có vào tổng đơn
                            tongTienDonHang += ((sanPhamTonTai.DonGiaBan ?? 0) * item.So_Luong);
                        }
                    }
                }

                // C. 🎯 KHÚC QUAN TRỌNG: TỰ ĐỘNG KHỞI TẠO BẢN GHI BẢNG CUSTOM ĐƠN BÁNH
                if (request.LaDonCustom)
                {
                    var totalCustomRecords = await _context.DonBanhCustoms.CountAsync() + 1;
                    var maCustomHienThi = $"DC{totalCustomRecords:D3}"; // Tạo mã dạng DC001, DC002

                    var donBanhCustom = new DonBanhCustom
                    {
                        MaCustomHienThi = maCustomHienThi,
                        DonHangId = donHangMoi.DonHangId, // Gắn khóa ngoại kết nối chặt chẽ
                        LoaiYeuCau = request.LoaiYeuCau,
                        KichThuocSoLuong = request.KichThuocSoluong,
                        MauSacChuDao = request.MauSacChuDao,
                        GhiChu = request.GhiChu,
                        NhanBanh = "Cốt kem tươi vani tiêu chuẩn", // Mặc định nền cho thợ làm bánh
                        NgayLayHang = request.NgayLayHang ?? DateTime.Now.AddDays(3),
                        HinhAnh = request.HinhAnh ?? ""
                    };
                    _context.DonBanhCustoms.Add(donBanhCustom);
                }

                // D. TÍNH KHUYẾN MÃI VÀ TRỪ TIỀN
                decimal soTienGiam = 0;
                if (request.Khuyen_Mai_ID.HasValue)
                {
                    var khuyenMai = await _context.KhuyenMais.FindAsync(request.Khuyen_Mai_ID.Value);
                    if (khuyenMai != null && khuyenMai.PhanTramGiam.HasValue)
                    {
                        soTienGiam = tongTienDonHang * ((decimal)khuyenMai.PhanTramGiam.Value / 100);
                    }
                }

                donHangMoi.SoTienGiam = soTienGiam;
                donHangMoi.TongTien = tongTienDonHang - soTienGiam; // Số tiền thanh toán cuối cùng

                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // Đóng gói chu trình lưu an toàn thành công!

                return Ok(new { Message = "Tạo đơn hàng tổng hợp thành công!", DonHangId = donHangMoi.DonHangId });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { Message = "Lỗi dữ liệu liên kết hệ thống mã Id không khớp!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { Message = "Lỗi hệ thống máy chủ: " + ex.Message });
            }
        }

        // ==========================================
        // 3. API: CẬP NHẬT TRẠNG THÁI TIẾN ĐỘ SIÊU TỐC
        // ==========================================
        [HttpPut("CapNhatTrangThaiDonHang/{id}")]
        public async Task<IActionResult> CapNhatTrangThai(int id, [FromBody] UpdateStatusRequest request)
        {
            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null) return NotFound();

            donHang.TrangThai = request.TrangThaiMoi;
            donHang.NguoiCapNhat = request.TenNhanVien;
            donHang.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Nhân viên {request.TenNhanVien} đã cập nhật trạng thái!" });
        }

        // ==========================================
        // 4. API: KIỂM TRA ĐƠN HÀNG CUSTOM
        // ==========================================
        [HttpGet("KiemTraDonHang/{donHangId}")]
        public async Task<IActionResult> KiemTraDonCustom(int donHangId)
        {
            try
            {
                var donHangTonTai = await _context.DonHangs.AnyAsync(d => d.DonHangId == donHangId);
                if (!donHangTonTai)
                {
                    return NotFound(new { Message = $"Không tìm thấy đơn hàng mã số {donHangId}." });
                }

                var thongTinCustom = await _context.DonBanhCustoms.FirstOrDefaultAsync(c => c.DonHangId == donHangId);
                if (thongTinCustom == null)
                {
                    return Ok(new { LaDonCustom = false, Message = "Đây là đơn bánh có sẵn bình thường." });
                }

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

        // ==========================================
        // 5. API: XUẤT HÓA ĐƠN XML ĐƠN HÀNG
        // ==========================================
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

                byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(xmlString);
                string fileName = $"HoaDon_So_{id}.xml";

                return File(fileBytes, "application/xml", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi tạo XML: " + ex.Message });
            }
        }

        // ==========================================
        // 6. API: TẢI CHI TIẾT GIỎ MÓN ĂN ĐI KÈM
        // ==========================================
        [HttpGet("LayChiTietSanPhams/{donHangId}")]
        public async Task<IActionResult> LayChiTietSanPhams(int donHangId)
        {
            try
            {
                var danhSachMon = await _context.ChiTietDonHangs
                    .Where(ct => ct.DonHangId == donHangId)
                    .Select(ct => new
                    {
                        SanPhamId = ct.SanPhamId,
                        TenSanPham = ct.SanPham.TenSanPham,
                        HinhAnh = ct.SanPham.HinhAnh,
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia
                    })
                    .ToListAsync();

                return Ok(danhSachMon);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi tải chi tiết món: " + ex.Message });
            }
        }

        // ==========================================
        // 7. API: LẤY THÔNG TIN HÀNH CHÍNH ĐƠN THEO ID
        // ==========================================
        [HttpGet("LayDonHangTheoId/{id}")]
        public async Task<IActionResult> GetDonHangById(int id)
        {
            var dh = await _context.DonHangs.FindAsync(id);
            if (dh == null) return NotFound(new { Message = "Không tìm thấy đơn hàng!" });
            return Ok(dh);
        }

        // ==========================================
        // 8. API: CẬP NHẬT TỔNG HỢP HỒ SƠ ĐƠN HÀNG
        // ==========================================
        [HttpPut("CapNhatThongTinDonHang/{id}")]
        public async Task<IActionResult> CapNhatDonHang(int id, [FromBody] CapNhatDonHangTongHopDTO request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null) return NotFound(new { Message = "Không tìm thấy đơn hàng cần sửa!" });

            try
            {
                // A. Cập nhật bảng thông tin hành chính đơn hàng chính
                donHang.KhachHangId = request.Khach_Hang_ID == 0 ? null : request.Khach_Hang_ID;
                donHang.KhuyenMaiId = request.Khuyen_Mai_ID;
                donHang.TenNguoiNhan = request.Ten_Nguoi_Nhan;
                donHang.SdtNguoiNhan = request.SDT_Nguoi_Nhan;
                donHang.DiaChiGiao = request.Dia_Chi_Giao;
                donHang.NgayCapNhat = DateTime.Now;
                donHang.NguoiCapNhat = request.TenNhanVienCapNhat ?? "Nhân viên hệ thống";

                // B. Logic đồng bộ/chuyển đổi đơn Custom thông minh
                var customDon = await _context.DonBanhCustoms.FirstOrDefaultAsync(c => c.DonHangId == id);
                if (request.LaDonCustom)
                {
                    if (customDon != null)
                    {
                        // Đơn đã có sẵn hàng Custom -> Tiến hành lưu ghi đè thuộc tính mới thay đổi
                        customDon.LoaiYeuCau = request.LoaiYeuCau;
                        customDon.KichThuocSoLuong = request.KichThuocSoluong;
                        customDon.MauSacChuDao = request.MauSacChuDao;
                        customDon.GhiChu = request.GhiChu;
                        customDon.NgayLayHang = request.NgayLayHang ?? DateTime.Now;
                        customDon.HinhAnh = request.HinhAnh;
                    }
                    else
                    {
                        // 🛠️ BỌC LÓT CAO CẤP: Nếu đơn hàng cũ ban đầu là Đơn thường, nay gạt công tắc đổi thành đơn Custom
                        // hệ thống tự động khởi tạo luôn một hàng mới đập vào bảng DonBanhCustom để không bị trống rỗng DB
                        var totalCustomRecords = await _context.DonBanhCustoms.CountAsync() + 1;
                        var newCustom = new DonBanhCustom
                        {
                            MaCustomHienThi = $"DC{totalCustomRecords:D3}",
                            DonHangId = id,
                            LoaiYeuCau = request.LoaiYeuCau,
                            KichThuocSoLuong = request.KichThuocSoluong,
                            MauSacChuDao = request.MauSacChuDao,
                            GhiChu = request.GhiChu,
                            NgayLayHang = request.NgayLayHang ?? DateTime.Now,
                            HinhAnh = request.HinhAnh,
                            NhanBanh = "Cốt kem tươi vani tiêu chuẩn"
                        };
                        _context.DonBanhCustoms.Add(newCustom);
                    }
                }
                else
                {
                    // Nếu trước đó là đơn Custom, nay nhân viên tắt công tắc gạt trả về đơn bánh có sẵn -> Xóa sạch bản ghi Custom thừa thãi đi
                    if (customDon != null) _context.DonBanhCustoms.Remove(customDon);
                }

                // C. Làm sạch giỏ hàng chi tiết cũ để nạp lại giỏ mới cập nhật
                var chiTietsCu = await _context.ChiTietDonHangs.Where(ct => ct.DonHangId == id).ToListAsync();
                if (chiTietsCu.Any()) _context.ChiTietDonHangs.RemoveRange(chiTietsCu);

                decimal tongTienDonHang = request.LaDonCustom ? (request.TongTienCustom ?? 0) : 0;
                foreach (var item in request.ChiTietGioHang)
                {
                    var sanPhamTonTai = await _context.SanPhams.FindAsync(item.SanPham_ID);
                    if (sanPhamTonTai != null)
                    {
                        var chiTiet = new ChiTietDonHang
                        {
                            DonHangId = id,
                            SanPhamId = item.SanPham_ID,
                            SoLuong = item.So_Luong,
                            DonGia = sanPhamTonTai.DonGiaBan,
                        };
                        _context.ChiTietDonHangs.Add(chiTiet);

                        tongTienDonHang += ((sanPhamTonTai.DonGiaBan ?? 0) * item.So_Luong);
                    }
                }

                // Tính toán khấu trừ mã giảm giá khuyến mãi hành chính
                decimal soTienGiam = 0;
                if (request.Khuyen_Mai_ID.HasValue)
                {
                    var khuyenMai = await _context.KhuyenMais.FindAsync(request.Khuyen_Mai_ID.Value);
                    if (khuyenMai != null && khuyenMai.PhanTramGiam.HasValue)
                    {
                        soTienGiam = tongTienDonHang * ((decimal)khuyenMai.PhanTramGiam.Value / 100);
                    }
                }
                donHang.SoTienGiam = soTienGiam;
                donHang.TongTien = tongTienDonHang - soTienGiam;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Cập nhật hồ sơ đơn hàng tổng hợp thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi cập nhật: " + ex.Message });
            }
        }

        // ==========================================
        // 9. API: XÓA ĐƠN HÀNG KHỎI DB (CẤM XÓA KHI ĐÃ DUYỆT TRÊN JS)
        // ==========================================
        [HttpDelete("XoaDonHang/{id}")]
        public async Task<IActionResult> XoaDonHang(int id)
        {
            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null) return NotFound(new { Message = "Không tìm thấy đơn hàng cần xóa!" });

            try
            {
                var chiTiets = await _context.ChiTietDonHangs.Where(ct => ct.DonHangId == id).ToListAsync();
                if (chiTiets.Any()) _context.ChiTietDonHangs.RemoveRange(chiTiets);

                var customDon = await _context.DonBanhCustoms.FirstOrDefaultAsync(c => c.DonHangId == id);
                if (customDon != null) _context.DonBanhCustoms.Remove(customDon);

                _context.DonHangs.Remove(donHang);
                await _context.SaveChangesAsync();
                return Ok(new { Success = true, Message = "Đã xóa đơn hàng thành công khỏi hệ thống!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = "Lỗi khi xóa đơn hàng: " + ex.Message });
            }
        }

        // ==========================================
        // 10. API: TẢI FILE ẢNH MẪU LÊN SERVER VẬT LÝ
        // ==========================================
        [HttpPost("UploadAnhMauCustom")]
        public async Task<IActionResult> UploadAnh(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(new { Message = "File ảnh không hợp lệ!" });

            try
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var fileName = Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var linkAnhNoiBo = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
                return Ok(new { Success = true, LinkAnh = linkAnhNoiBo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi upload file: " + ex.Message });
            }
        }
    }
}