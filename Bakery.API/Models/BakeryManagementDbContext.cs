using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Bakery.API.Models;

public partial class BakeryManagementDbContext : DbContext
{
    public BakeryManagementDbContext()
    {
    }

    public BakeryManagementDbContext(DbContextOptions<BakeryManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

    public virtual DbSet<DonBanhCustom> DonBanhCustoms { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<KhuyenMai> KhuyenMais { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<VThongKeSanPham> VThongKeSanPhams { get; set; }

    public virtual DbSet<VTraCuuDonHang> VTraCuuDonHangs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=BakeryManagement_DB;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiTietDonHang>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("PK__ChiTietD__47308331F09A0067");

            entity.ToTable("ChiTietDonHang");

            entity.Property(e => e.DetailId).HasColumnName("Detail_ID");
            entity.Property(e => e.DonGia)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Don_Gia");
            entity.Property(e => e.DonHangId).HasColumnName("Don_Hang_ID");
            entity.Property(e => e.SanPhamId).HasColumnName("SanPham_ID");
            entity.Property(e => e.SoLuong).HasColumnName("So_Luong");

            entity.HasOne(d => d.DonHang).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(d => d.DonHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietDo__Don_H__66603565");

            entity.HasOne(d => d.SanPham).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietDo__SanPh__6754599E");
        });

        modelBuilder.Entity<DonBanhCustom>(entity =>
        {
            entity.HasKey(e => e.CustomId).HasName("PK__DonBanhC__A4CB7F7B4C81303E");

            entity.ToTable("DonBanhCustom");

            entity.HasIndex(e => e.DonHangId, "UQ__DonBanhC__53EABD9EB85973AA").IsUnique();

            entity.Property(e => e.CustomId).HasColumnName("Custom_ID");
            entity.Property(e => e.DonHangId).HasColumnName("Don_Hang_ID");
            entity.Property(e => e.GhiChu).HasColumnName("Ghi_Chu");
            entity.Property(e => e.HinhAnh).HasColumnName("Hinh_Anh");
            entity.Property(e => e.KichThuocSoLuong)
                .HasMaxLength(100)
                .HasColumnName("Kich_Thuoc_So_Luong");
            entity.Property(e => e.LoaiYeuCau)
                .HasMaxLength(100)
                .HasColumnName("Loai_Yeu_Cau");
            entity.Property(e => e.MaCustomHienThi)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasComputedColumnSql("('DC'+right('000'+CONVERT([varchar],[Custom_ID]),(3)))", false)
                .HasColumnName("Ma_Custom_HienThi");
            entity.Property(e => e.MauSacChuDao)
                .HasMaxLength(50)
                .HasColumnName("Mau_Sac_Chu_Dao");
            entity.Property(e => e.NgayLayHang)
                .HasColumnType("datetime")
                .HasColumnName("Ngay_Lay_Hang");
            entity.Property(e => e.NhanBanh)
                .HasMaxLength(100)
                .HasColumnName("Nhan_Banh");

            entity.HasOne(d => d.DonHang).WithOne(p => p.DonBanhCustom)
                .HasForeignKey<DonBanhCustom>(d => d.DonHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DonBanhCu__Don_H__6B24EA82");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.DonHangId).HasName("PK__DonHang__53EABD9FD29E0931");

            entity.ToTable("DonHang", tb => tb.HasTrigger("TRG_TruTonKhoKhiHoanTat"));

            entity.Property(e => e.DonHangId).HasColumnName("Don_Hang_ID");
            entity.Property(e => e.DiaChiGiao).HasColumnName("Dia_Chi_Giao");
            entity.Property(e => e.KhachHangId).HasColumnName("Khach_Hang_ID");
            entity.Property(e => e.KhuyenMaiId).HasColumnName("Khuyen_Mai_ID");
            entity.Property(e => e.MaDhHienThi)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasComputedColumnSql("('DH'+right('000'+CONVERT([varchar],[Don_Hang_ID]),(3)))", false)
                .HasColumnName("Ma_DH_HienThi");
            entity.Property(e => e.NgayCapNhat)
                .HasColumnType("datetime")
                .HasColumnName("Ngay_Cap_Nhat");
            entity.Property(e => e.NgayDatHang)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Ngay_Dat_Hang");
            entity.Property(e => e.NguoiCapNhat)
                .HasMaxLength(100)
                .HasColumnName("Nguoi_Cap_Nhat");
            entity.Property(e => e.NhanVienId).HasColumnName("Nhan_Vien_ID");
            entity.Property(e => e.SdtNguoiNhan)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("SDT_Nguoi_Nhan");
            entity.Property(e => e.SoTienGiam)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("So_Tien_Giam");
            entity.Property(e => e.TenNguoiNhan)
                .HasMaxLength(100)
                .HasColumnName("Ten_Nguoi_Nhan");
            entity.Property(e => e.TongTien)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Tong_Tien");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chờ xử lý")
                .HasColumnName("Trang_Thai");

            entity.HasOne(d => d.KhachHang).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.KhachHangId)
                .HasConstraintName("FK__DonHang__Khach_H__5FB337D6");

            entity.HasOne(d => d.KhuyenMai).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.KhuyenMaiId)
                .HasConstraintName("FK__DonHang__Khuyen___619B8048");

            entity.HasOne(d => d.NhanVien).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.NhanVienId)
                .HasConstraintName("FK__DonHang__Nhan_Vi__60A75C0F");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.KhachHangId).HasName("PK__KhachHan__491071C8637088A2");

            entity.ToTable("KhachHang");

            entity.HasIndex(e => e.Email, "UQ__KhachHan__A9D10534DAA8F8BC").IsUnique();

            entity.HasIndex(e => e.Sdt, "UQ__KhachHan__CA1930A54A82EFB2").IsUnique();

            entity.Property(e => e.KhachHangId).HasColumnName("Khach_Hang_ID");
            entity.Property(e => e.DiaChi)
                .HasMaxLength(255)
                .HasColumnName("Dia_Chi");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MaKhHienThi)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComputedColumnSql("('KH'+right('00'+CONVERT([varchar],[Khach_Hang_ID]),(2)))", false)
                .HasColumnName("Ma_KH_HienThi");
            entity.Property(e => e.MatKhau)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Mat_Khau");
            entity.Property(e => e.Sdt)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("SDT");
            entity.Property(e => e.TenKhachHang)
                .HasMaxLength(100)
                .HasColumnName("Ten_Khach_Hang");
        });

        modelBuilder.Entity<KhuyenMai>(entity =>
        {
            entity.HasKey(e => e.KhuyenMaiId).HasName("PK__KhuyenMa__0FC4B468923D909B");

            entity.ToTable("KhuyenMai");

            entity.HasIndex(e => e.MaCode, "UQ__KhuyenMa__2A12334857240FE8").IsUnique();

            entity.Property(e => e.KhuyenMaiId).HasColumnName("Khuyen_Mai_ID");
            entity.Property(e => e.MaCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Ma_Code");
            entity.Property(e => e.NgayBatDau)
                .HasColumnType("datetime")
                .HasColumnName("Ngay_Bat_Dau");
            entity.Property(e => e.NgayKetThuc)
                .HasColumnType("datetime")
                .HasColumnName("Ngay_Ket_Thuc");
            entity.Property(e => e.PhanTramGiam).HasColumnName("Phan_Tram_Giam");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.NhanVienId).HasName("PK__NhanVien__8ABC4B307F9B2335");

            entity.ToTable("NhanVien");

            entity.HasIndex(e => e.Email, "UQ__NhanVien__A9D1053435F91663").IsUnique();

            entity.HasIndex(e => e.Sdt, "UQ__NhanVien__CA1930A5E03C7113").IsUnique();

            entity.Property(e => e.NhanVienId).HasColumnName("Nhan_Vien_ID");
            entity.Property(e => e.ChucVu)
                .HasMaxLength(50)
                .HasColumnName("Chuc_Vu");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MaNvHienThi)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComputedColumnSql("('NV'+right('00'+CONVERT([varchar],[Nhan_Vien_ID]),(2)))", false)
                .HasColumnName("Ma_NV_HienThi");
            entity.Property(e => e.MatKhau)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Mat_Khau");
            entity.Property(e => e.NgayVaoLam)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Ngay_Vao_Lam");
            entity.Property(e => e.Sdt)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("SDT");
            entity.Property(e => e.TenNhanVien)
                .HasMaxLength(100)
                .HasColumnName("Ten_Nhan_Vien");
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.SanPhamId).HasName("PK__SanPham__5A29D2DD4023FB81");

            entity.ToTable("SanPham", tb => tb.HasTrigger("TRG_NganXoaSanPham"));

            entity.Property(e => e.SanPhamId).HasColumnName("SanPham_ID");
            entity.Property(e => e.DonGiaBan)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Don_Gia_Ban");
            entity.Property(e => e.HinhAnh).HasColumnName("Hinh_Anh");
            entity.Property(e => e.MaSpHienThi)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasComputedColumnSql("('SP'+right('000'+CONVERT([varchar],[SanPham_ID]),(3)))", false)
                .HasColumnName("Ma_SP_HienThi");
            entity.Property(e => e.MoTa).HasColumnName("Mo_Ta");
            entity.Property(e => e.PhanLoai)
                .HasMaxLength(50)
                .HasColumnName("Phan_Loai");
            entity.Property(e => e.SoLuongTon).HasColumnName("So_Luong_Ton");
            entity.Property(e => e.TenSanPham)
                .HasMaxLength(200)
                .HasColumnName("Ten_San_Pham");
        });

        modelBuilder.Entity<VThongKeSanPham>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("V_ThongKeSanPham");

            entity.Property(e => e.DonGiaBan)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Don_Gia_Ban");
            entity.Property(e => e.HinhAnh).HasColumnName("Hinh_Anh");
            entity.Property(e => e.MaSpHienThi)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("Ma_SP_HienThi");
            entity.Property(e => e.PhanLoai)
                .HasMaxLength(50)
                .HasColumnName("Phan_Loai");
            entity.Property(e => e.SanPhamId).HasColumnName("SanPham_ID");
            entity.Property(e => e.SoLuongTon).HasColumnName("So_Luong_Ton");
            entity.Property(e => e.TenSanPham)
                .HasMaxLength(200)
                .HasColumnName("Ten_San_Pham");
            entity.Property(e => e.TongSoLuongDaBan).HasColumnName("Tong_So_Luong_Da_Ban");
        });

        modelBuilder.Entity<VTraCuuDonHang>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("V_TraCuuDonHang");

            entity.Property(e => e.DonHangId).HasColumnName("Don_Hang_ID");
            entity.Property(e => e.MaDhHienThi)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("Ma_DH_HienThi");
            entity.Property(e => e.NgayCapNhat)
                .HasColumnType("datetime")
                .HasColumnName("Ngay_Cap_Nhat");
            entity.Property(e => e.NgayDatHang)
                .HasColumnType("datetime")
                .HasColumnName("Ngay_Dat_Hang");
            entity.Property(e => e.NguoiCapNhat)
                .HasMaxLength(100)
                .HasColumnName("Nguoi_Cap_Nhat");
            entity.Property(e => e.NhanVienTiepNhan)
                .HasMaxLength(100)
                .HasColumnName("Nhan_Vien_Tiep_Nhan");
            entity.Property(e => e.SdtNguoiNhan)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("SDT_Nguoi_Nhan");
            entity.Property(e => e.SoTienGiam)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("So_Tien_Giam");
            entity.Property(e => e.TenKhachMua)
                .HasMaxLength(100)
                .HasColumnName("Ten_Khach_Mua");
            entity.Property(e => e.ThanhTienThucTe)
                .HasColumnType("decimal(19, 2)")
                .HasColumnName("Thanh_Tien_Thuc_Te");
            entity.Property(e => e.TongTien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Tong_Tien");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasColumnName("Trang_Thai");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
