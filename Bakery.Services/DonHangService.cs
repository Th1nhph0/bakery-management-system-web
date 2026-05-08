using System.Xml.Linq;
using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace Bakery.Services
{
    public class DonHangService
    {
        private readonly BakeryManagementDbContext _context;

        public DonHangService(BakeryManagementDbContext context)
        {
            _context = context;
        }

        public async Task<int> TaoDonHangMoiAsync(TaoDonHangRequest request)
        {
            // 1. Chuyển mảng Giỏ hàng thành chuỗi JSON
            string gioHangJson = JsonSerializer.Serialize(request.ChiTietGioHang);

            // 2. Dùng ADO.NET bên trong EF Core để gọi Stored Procedure
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SP_TaoDonHangMoi";
                command.CommandType = CommandType.StoredProcedure;

                // Thêm các tham số (Tham số nào NULL thì phải dùng DBNull.Value)
                command.Parameters.Add(new SqlParameter("@Khach_Hang_ID", request.Khach_Hang_ID ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Nhan_Vien_ID", request.Nhan_Vien_ID ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Khuyen_Mai_ID", request.Khuyen_Mai_ID ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Ten_Nguoi_Nhan", request.Ten_Nguoi_Nhan));
                command.Parameters.Add(new SqlParameter("@SDT_Nguoi_Nhan", request.SDT_Nguoi_Nhan));
                command.Parameters.Add(new SqlParameter("@Dia_Chi_Giao", request.Dia_Chi_Giao));

                // Tham số quan trọng nhất: Giỏ hàng JSON
                command.Parameters.Add(new SqlParameter("@GioHang_JSON", gioHangJson));

                // 3. Mở kết nối và thực thi
                await _context.Database.OpenConnectionAsync();

                // Dùng ExecuteScalar để lấy cái ID Đơn Hàng từ lệnh SELECT @NewDonHangID trong SQL
                var result = await command.ExecuteScalarAsync();

                await _context.Database.CloseConnectionAsync();

                return Convert.ToInt32(result);
            }
        }
        // Xuất hóa đơn ra chuỗi XML bằng LINQ to XML
        public async Task<string?> XuatHoaDonXmlAsync(int donHangId)
        {
            // 1. Lấy thông tin Đơn hàng kèm theo danh sách Chi tiết
            var donHang = await _context.DonHangs.Include(dh => dh.ChiTietDonHangs).Include(dh => dh.DonBanhCustom).FirstOrDefaultAsync(dh => dh.DonHangId == donHangId);


            if (donHang == null) return null;

            // 2. Dùng LINQ to XML để vẽ cấu trúc cây XML
            XDocument xmlDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("HoaDon",
                    new XElement("ThongTinChung",
                        new XElement("MaDonHang", donHang.DonHangId),
                        new XElement("TenKhachHang", donHang.TenNguoiNhan),
                        new XElement("SoDienThoai", donHang.SdtNguoiNhan),
                        new XElement("DiaChi", donHang.DiaChiGiao),
                        // Lưu ý: Nếu DB của bạn tên khác thì đổi chữ NgayDat lại nhé
                        new XElement("NgayDat", donHang.NgayDatHang?.ToString("dd/MM/yyyy HH:mm"))
                    ),
                    new XElement("DanhSachMonHang",
                        // LINQ phát huy tác dụng ở đây: Duyệt mảng và tạo XElement tự động
                        donHang.ChiTietDonHangs.Select(ct =>
                            new XElement("Mon",
                                new XElement("MaSanPham", ct.SanPhamId),
                                new XElement("SoLuong", ct.SoLuong),
                                new XElement("DonGia", ct.DonGia),
                                new XElement("ThanhTien", ct.SoLuong * ct.DonGia)
                            )
                        )
                    )
                )
            );

            // Trả về chuỗi XML
            return xmlDoc.ToString();
        }
    }
}