let gioHangTam = [];
let danhSachSanPhams = []; // Lưu trữ toàn cục danh sách sản phẩm để check tồn kho
let isCustomOrder = false;
let isProductLocked = false; // Biến cờ kiểm soát trạng thái khóa giỏ hàng bánh
let currentOrderStatus = "Chờ xử lý"; // Biến toàn cục: Giữ trạng thái đơn từ DB nạp vào để bảo toàn khi lưu

// ==========================================
// 1. KHỞI ĐỘNG HỆ THỐNG TRANG FORM
// ==========================================
document.addEventListener('DOMContentLoaded', async () => {
    // Ép hệ thống phải đợi tải xong hết toàn bộ các Dropdown dữ liệu nền từ DB lên trước
    await Promise.all([loadKhachHangs(), loadSanPhams(), loadKhuyenMais()]);

    // KÍCH HOẠT TÍNH NĂNG GÕ TÌM KIẾM CHO CÁC Ô CHỌN (SỬ DỤNG JQUERY SELECT2)
    if ($.fn.select2) {
        $('#selectKhachHang').select2({ width: '100%' });
        $('#selectSanPham').select2({ width: '100%' });
        $('#selectKhuyenMai').select2({ width: '100%' });
    }

    // Gắn sự kiện lắng nghe công tắc gạt Custom Order thủ công
    const toggleSwitch = document.getElementById('checkIsCustomOrder');
    if (toggleSwitch) {
        toggleSwitch.addEventListener('change', function () {
            isCustomOrder = this.checked;
            // Nếu người dùng chủ động tắt công tắc gạt -> Xóa sạch nội dung các ô Custom về rỗng
            if (!this.checked) {
                clearCustomFields();
            }
        });
    }

    // 🔥 THUẬT TOÁN ĐỘC CHIÊU: TỰ ĐỘNG BẬT CÔNG TẮC ĐƠN CUSTOM KHI PHÁT HIỆN CÓ HÀNH VI NHẬP LIỆU
    const danhSachOInputsCustom = document.querySelectorAll('#formCustomOrderFields input, #formCustomOrderFields textarea');
    danhSachOInputsCustom.forEach(input => {
        input.addEventListener('input', () => {
            let phatHienCoChu = false;

            // Duyệt qua tất cả các ô xem có ô nào đang chứa chữ hoặc số tiền không
            danhSachOInputsCustom.forEach(item => {
                if (item.type !== 'checkbox' && item.type !== 'file' && item.value.trim() !== '') {
                    phatHienCoChu = true;
                }
            });

            // Nếu phát hiện có dữ liệu gõ vào -> Tự động bật công tắc gạt và kích hoạt biến cờ đơn Custom
            const congTacGat = document.getElementById('checkIsCustomOrder');
            if (congTacGat) {
                congTacGat.checked = phatHienCoChu;
                isCustomOrder = phatHienCoChu;
            }
        });
    });

    // Tiến hành check dữ liệu sửa đơn
    checkEditMode();

    const form = document.getElementById('formAddOrder');
    if (form) form.addEventListener('submit', submitOrderForm);
});

// ==========================================
// 2. HÀM CHUYỂN ĐỔI CHẾ ĐỘ EDIT MODE - PHÒNG THỦ ĐA NỀN DỮ LIỆU
// ==========================================
// ==========================================
// ĐÃ VÁ LỖI: HÀM EDIT MODE TỰ ĐỘNG TÍNH NGƯỢC TIỀN CUSTOM ĐỂ ĐỔ LÊN FORM
// ==========================================
async function checkEditMode() {
    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');

    const formTitle = document.querySelector('.card-header h5');
    const submitBtn = document.querySelector('button[type="submit"]');

    if (!editId) return;

    if (formTitle) formTitle.innerText = "Cập Nhật Thông Tin Đơn Hàng";
    if (submitBtn) {
        submitBtn.innerText = "Lưu Cập Nhật Đơn Hàng";
        submitBtn.classList.replace('btn-primary', 'btn-warning');
    }

    let laDonCustomGoc = false;
    let totalDonHangGoc = 0; // Biến giữ tổng tiền gốc từ hóa đơn chính
    let soTienGiamGoc = 0;   // Biến giữ số tiền giảm giá

    // A. Lấy thông tin đơn hàng chung hành chính
    try {
        const response = await fetch(`${API_URL}/api/DonHang/LayDonHangTheoId/${editId}`);
        if (response.ok) {
            const dh = await response.json();

            const khachHangId = dh.khachHangId || dh.KhachHangId || "0";
            const khuyenMaiId = dh.khuyenMaiId || dh.KhuyenMaiId || '';

            $('#selectKhachHang').val(khachHangId).trigger('change');
            $('#selectKhuyenMai').val(khuyenMaiId).trigger('change');

            document.getElementById('tenNguoiNhan').value = dh.tenNguoiNhan || dh.TenNguoiNhan || '';
            document.getElementById('sdtNguoiNhan').value = dh.sdtNguoiNhan || dh.SdtNguoiNhan || '';
            document.getElementById('diaChiGiao').value = dh.diaChiGiao || dh.DiaChiGiao || '';

            currentOrderStatus = dh.trang_thai || dh.trangThai || dh.TrangThai || 'Chờ xử lý';
            laDonCustomGoc = dh.laDonCustom || dh.LaDonCustom || dh.la_don_custom || dh.La_Don_Custom || false;

            // Nạp giá trị tiền tệ tổng quan phục vụ tính toán ngược
            totalDonHangGoc = dh.tongTien || dh.TongTien || 0;
            soTienGiamGoc = dh.soTienGiam || dh.SoTienGiam || 0;
        }
    } catch (e) { console.error("Lỗi nạp đơn hàng chung:", e); }

    let tongTienBanhThuongHienTai = 0;

    // C. Đổ lại giỏ hàng bánh tiêu chuẩn đi kèm và tính tổng tiền bánh thường
    try {
        const resItems = await fetch(`${API_URL}/api/DonHang/LayChiTietSanPhams/${editId}`);
        if (resItems.ok) {
            const items = await resItems.json();
            gioHangTam = [];

            items.forEach(item => {
                let qty = item.soLuong || item.SoLuong || 0;
                let price = item.donGia || item.DonGia || 0;

                // Cộng dồn tiền giỏ bánh thường
                tongTienBanhThuongHienTai += (qty * price);

                gioHangTam.push({
                    SanPham_ID: item.sanPhamId !== undefined ? item.sanPhamId : item.SanPhamId,
                    Ten: item.tenSanPham !== undefined ? item.tenSanPham : item.TenSanPham,
                    So_Luong: qty
                });
            });
            renderCartTable();
        }
    } catch (e) { console.error(e); }

    // B. Kiểm tra và bốc dữ liệu Đơn bánh Custom lên Form
    try {
        const resCheck = await fetch(`${API_URL}/api/DonHang/KiemTraDonHang/${editId}`);
        if (resCheck.ok) {
            const resData = await resCheck.json();
            const laDonCustom = resData.laDonCustom !== undefined ? resData.laDonCustom : (resData.LaDonCustom !== undefined ? resData.LaDonCustom : laDonCustomGoc);
            const customData = resData.data || resData.Data || (resData.loaiYeuCau || resData.loai_yeu_cau || resData.LoaiYeuCau ? resData : null);

            if (laDonCustom || customData) {
                isCustomOrder = true;

                const toggleBtn = document.getElementById('checkIsCustomOrder');
                if (toggleBtn) toggleBtn.checked = true;

                const customSection = document.getElementById('formCustomOrderFields');
                if (customSection) customSection.style.display = "block";

                if (customData) {
                    document.getElementById('customLoaiYeuCau').value = customData.loaiYeuCau || customData.LoaiYeuCau || customData.loai_yeu_cau || '';
                    document.getElementById('customKichThuoc').value = customData.kichThuocSoluong || customData.KichThuocSoluong || customData.kich_thuoc_soluong || '';
                    document.getElementById('customMauSac').value = customData.mauSacChuDao || customData.MauSacChuDao || customData.mau_sac_chu_dao || '';
                    document.getElementById('customGhiChu').value = customData.ghiChu || customData.GhiChu || customData.ghi_chu || '';

                    // 🔥 THUẬT TOÁN ÉP ĐỔ TIỀN REAL: Lấy tổng đơn + giảm giá - tiền bánh thường = Giá trị báo giá bánh Custom
                    let giaCustomTinhToan = totalDonHangGoc + soTienGiamGoc - tongTienBanhThuongHienTai;
                    if (giaCustomTinhToan < 0) giaCustomTinhToan = 0;

                    document.getElementById('customBaoGia').value = giaCustomTinhToan;

                    const dateRaw = customData.ngayLayHang || customData.NgayLayHang || customData.ngay_lay_hang;
                    if (dateRaw) {
                        document.getElementById('customNgayLay').value = dateRaw.split('T')[0];
                    }

                    // 🔥 ĐOẠN ĐÃ SỬA: Radar quét sạch mọi kiểu viết sai chính tả HinhANh từ SQL Server
                    const linkAnh = customData.HinhANh || customData.hinhANh || customData.HinhAnh || customData.hinhAnh || customData.hinh_anh || '';
                    document.getElementById('customHinhAnh').value = linkAnh;

                    iif(linkAnh && linkAnh !== 'string' && linkAnh.trim() !== '') {
                        const previewContainer = document.getElementById('editPreviewContainer');
                        const previewImg = document.getElementById('customImgPreview');
                        if (previewContainer && previewImg) {
                            previewImg.src = linkAnh;
                            previewContainer.style.display = "block";
                        }
                    }
                }
            }
        }
    } catch (e) { console.error("Lỗi nạp thông tin bánh Custom:", e); }
 

    // PHÂN QUYỀN ĐỘNG: NẾU ĐƠN ĐÃ ĐƯỢC DUYỆT -> KHÓA CỨNG SẢN PHẨM KHI EDIT
    if (currentOrderStatus !== "Chờ xử lý") {
        isProductLocked = true;
        renderCartTable();

        const selectSanPham = document.getElementById('selectSanPham');
        const inputSoLuong = document.getElementById('inputSoLuong');
        if (selectSanPham) selectSanPham.disabled = true;
        if (inputSoLuong) inputSoLuong.disabled = true;

        const btnThemCart = document.querySelector("button[onclick='addItemToCart()']");
        if (btnThemCart) btnThemCart.disabled = true;

        const checkCustom = document.getElementById('checkIsCustomOrder');
        if (checkCustom) checkCustom.disabled = true;

        const customInputs = document.querySelectorAll('#formCustomOrderFields input, #formCustomOrderFields textarea, #formCustomOrderFields select');
        customInputs.forEach(el => el.disabled = true);

        const alertBox = document.createElement('div');
        alertBox.className = "alert alert-warning shadow-sm mb-3 fw-bold text-dark";
        alertBox.innerHTML = `🔒 ĐƠN HÀNG ĐÃ ĐƯỢC DUYỆT: Hệ thống khóa chỉnh sửa Giỏ bánh & Đơn Custom. Bạn được phép điều chỉnh Thông tin khách nhận và áp dụng thêm mã Khuyến mãi công khai.`;
        document.getElementById('formAddOrder').prepend(alertBox);
    }
}
// ==========================================
// 3. CÁC HÀM NẠP DROPDOWN (GIỮ NGUYÊN)
// ==========================================
async function loadKhachHangs() { const select = document.getElementById('selectKhachHang'); try { const res = await fetch(`${API_URL}/api/KhachHang/DanhSachKhachHang`); const data = await res.json(); select.innerHTML = `<option value="0">👤 Khách hàng vãng lai (Mua tại quầy)</option>`; data.forEach(kh => { select.innerHTML += `<option value="${kh.khachHangId || kh.Id}">${kh.tenKhachHang || kh.TenKhachHang} - ${kh.sdt || kh.Sdt}</option>`; }); } catch (e) { console.error(e); } }
async function loadSanPhams() { const select = document.getElementById('selectSanPham'); try { const res = await fetch(`${API_URL}/api/SanPham/HienThiDanhSachSanPham`); danhSachSanPhams = await res.json(); select.innerHTML = `<option value="">-- Chọn loại bánh --</option>`; danhSachSanPhams.forEach(sp => { const id = sp.sanPhamId || sp.Id || Object.values(sp)[0]; const ten = sp.tenSanPham || sp.TenSanPham; const maSP = `SP${id.toString().padStart(3, '0')}`; select.innerHTML += `<option value="${id}">[${maSP}] - ${ten}</option>`; }); } catch (e) { console.error(e); } }
async function loadKhuyenMais() { const select = document.getElementById('selectKhuyenMai'); try { const res = await fetch(`${API_URL}/api/KhuyenMai/HienThiCacMaKhuyenMai`); const data = await res.json(); select.innerHTML = `<option value="">-- Không áp dụng --</option>`; data.forEach(km => { select.innerHTML += `<option value="${km.khuyenMaiId || km.Id}">${km.maCode || km.MaCode} (Giảm ${km.phanTramGiam}%)</option>`; }); } catch (e) { console.error(e); } }
function addItemToCart() { if (isProductLocked) return; const selectSp = document.getElementById('selectSanPham'); const inputSl = document.getElementById('inputSoLuong'); const spId = Number(selectSp.value); const soLuongMuonThem = Number(inputSl.value); if (!spId || soLuongMuonThem < 1) { alert("Vui lòng chọn loại bánh và nhập số lượng!"); return; } const spTimThay = danhSachSanPhams.find(p => (p.sanPhamId || p.Id) == spId); const tonKhoThucTe = spTimThay ? (spTimThay.soLuongTon || spTimThay.SoLuongTon || spTimThay.so_luong_ton || 0) : 0; const monDaCoTrongGio = gioHangTam.find(item => item.SanPham_ID === spId); const tongSoLuongSauKhiThem = soLuongMuonThem + (monDaCoTrongGio ? monDaCoTrongGio.So_Luong : 0); if (tongSoLuongSauKhiThem > tonKhoThucTe) { alert(`⚠️ LỖI TỒN KHO: Không thể thêm! Loại bánh này trong kho chỉ còn đúng [ ${tonKhoThucTe} ] cái.`); return; } if (monDaCoTrongGio) { monDaCoTrongGio.So_Luong = tongSoLuongSauKhiThem; } else { const tenSp = selectSp.options[selectSp.selectedIndex].text; gioHangTam.push({ SanPham_ID: spId, Ten: tenSp, So_Luong: soLuongMuonThem }); } renderCartTable(); }
function removeItemFromCart(index) { if (isProductLocked) return; gioHangTam.splice(index, 1); renderCartTable(); }
function renderCartTable() { const tbody = document.getElementById('cartTableBody'); tbody.innerHTML = ''; if (gioHangTam.length === 0) { tbody.innerHTML = `<tr><td colspan="3" class="text-center text-muted py-2">Chưa có sản phẩm nào trong giỏ hàng</td></tr>`; return; } gioHangTam.forEach((item, index) => { const actionBtn = isProductLocked ? `<button type="button" class="btn btn-sm btn-icon btn-outline-secondary" disabled title="Món bánh đã khóa tiến độ"><i class="bx bx-lock-alt"></i></button>` : `<button type="button" class="btn btn-sm btn-icon btn-outline-danger" onclick="removeItemFromCart(${index})"><i class="bx bx-trash"></i></button>`; tbody.innerHTML += `<tr><td><strong>${item.Ten}</strong></td><td class="text-center"><span class="badge bg-label-info">${item.So_Luong}</span></td><td>${actionBtn}</td></tr>`; }); }

// ==========================================
// 4. LƯU TRỌN GÓI ĐƠN HÀNG (🔥 ĐÃ TINH CHỈNH ĐỌC BAO VÂY ĐA BIẾN)
// ==========================================
async function submitOrderForm(event) {
    event.preventDefault();

    const tenNguoiDangNhap = localStorage.getItem('userName') || "Quản trị viên";
    const idNguoiDangNhap = localStorage.getItem('userId') || 1;

    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');

    // Đọc cờ trạng thái nút gạt Custom tự động/thủ công trên giao diện
    const congTacMoiNhat = document.getElementById('checkIsCustomOrder');
    const giaTriCustomThucTe = congTacMoiNhat ? congTacMoiNhat.checked : isCustomOrder;

    const khachHangVal = $('#selectKhachHang').val();
    const promoValue = $('#selectKhuyenMai').val();

    // Nếu không gạt nút bánh Custom và giỏ hàng trống thì báo lỗi chặn form
    if (!giaTriCustomThucTe && gioHangTam.length === 0) {
        alert("Giỏ hàng trống! Vui lòng chọn bánh tiêu chuẩn hoặc điền thông tin Đơn bánh thiết kế riêng.");
        return;
    }

    // Thu thập dữ liệu thô từ các trường nhập liệu Custom bánh thiết kế
    const loaiYeuCauVal = document.getElementById('customLoaiYeuCau').value;
    const kichThuocVal = document.getElementById('customKichThuoc').value;
    const mauSacVal = document.getElementById('customMauSac').value;
    const ghiChuVal = document.getElementById('customGhiChu').value;
    const ngayLayVal = document.getElementById('customNgayLay').value;
    const hinhAnhVal = document.getElementById('customHinhAnh').value;
    const baoGiaVal = Number(document.getElementById('customBaoGia').value) || 0;

    const orderData = {
        Khach_Hang_ID: (khachHangVal === "0" || !khachHangVal) ? null : Number(khachHangVal),
        Nhan_Vien_ID: idNguoiDangNhap ? Number(idNguoiDangNhap) : 1,
        Khuyen_Mai_ID: promoValue ? Number(promoValue) : null,
        Ten_Nguoi_Nhan: document.getElementById('tenNguoiNhan').value.trim(),
        SDT_Nguoi_Nhan: document.getElementById('sdtNguoiNhan').value.trim(),
        Dia_Chi_Giao: document.getElementById('diaChiGiao').value.trim(),

        TrangThai: currentOrderStatus,
        Trang_Thai: currentOrderStatus,
        trang_thai: currentOrderStatus,

        // Giữ nguyên các trường hành chính phía trên...
        ChiTietGioHang: gioHangTam.map(item => ({
            SanPham_ID: item.SanPham_ID,
            So_Luong: item.So_Luong
        })),

        // 🎯 ĐỒNG BỘ CHUẨN ĐÉT VỚI DTO C# (Bỏ các biến snake_case thừa thãi đi cho gọn sạch code)
        LaDonCustom: giaTriCustomThucTe,
        LoaiYeuCau: giaTriCustomThucTe ? loaiYeuCauVal : null,
        KichThuocSoluong: giaTriCustomThucTe ? kichThuocVal : null,
        MauSacChuDao: giaTriCustomThucTe ? mauSacVal : null,
        GhiChu: giaTriCustomThucTe ? ghiChuVal : null,
        NgayLayHang: giaTriCustomThucTe && ngayLayVal ? ngayLayVal : null,
        HinhAnh: giaTriCustomThucTe ? hinhAnhVal : null,
        TongTienCustom: giaTriCustomThucTe ? baoGiaVal : null,

        TenNhanVienCapNhat: tenNguoiDangNhap,
    };

    const method = editId ? 'PUT' : 'POST';
    const apiUrl = editId
        ? `${API_URL}/api/DonHang/CapNhatThongTinDonHang/${editId}`
        : `${API_URL}/api/DonHang/TaoDonHang`;

    try {
        const response = await fetch(apiUrl, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(orderData)
        });

        if (response.ok) {
            alert(editId ? "Cập nhật hồ sơ đơn hàng thành công!" : "Tạo đơn hàng thành công!");
            window.location.href = 'list-order.html';
        } else {
            const err = await response.json();
            alert(err.message || "Lưu dữ liệu thất bại!");
        }
    } catch (e) { console.error("Lỗi lưu đơn hàng:", e); }
}

function clearCustomFields() {
    document.getElementById('customLoaiYeuCau').value = '';
    document.getElementById('customKichThuoc').value = '';
    document.getElementById('customMauSac').value = '';
    document.getElementById('customGhiChu').value = '';
    document.getElementById('customNgayLay').value = '';
    document.getElementById('customHinhAnh').value = '';
    document.getElementById('customBaoGia').value = '';
}