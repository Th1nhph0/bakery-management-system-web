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
    const customSection = document.getElementById('formCustomOrderFields');

    if (toggleSwitch && customSection) {
        toggleSwitch.addEventListener('change', function () {
            isCustomOrder = this.checked;
            if (this.checked) {
                customSection.style.display = "block"; // Bật công tắc -> Hiện khung vàng
            } else {
                customSection.style.display = "none";  // Tắt công tắc -> Ẩn khung vàng
                clearCustomFields();
            }
        });
    }

    // 🔥 ĐỘC CHIÊU: TỰ ĐỘNG ĐÓNG KHUNG NẾU NHÂN VIÊN XÓA SẠCH CHỮ TRONG CÁC Ô CUSTOM
    const danhSachOInputsCustom = document.querySelectorAll('#formCustomOrderFields input, #formCustomOrderFields textarea');
    danhSachOInputsCustom.forEach(input => {
        input.addEventListener('input', () => {
            let phatHienCoChu = false;
            danhSachOInputsCustom.forEach(item => {
                if (item.type !== 'checkbox' && item.type !== 'file' && item.value.trim() !== '') {
                    phatHienCoChu = true;
                }
            });

            if (toggleSwitch && customSection && !isProductLocked) {
                // Nếu xóa sạch sành sanh chữ -> Tự tắt công tắc và ẩn luôn khung cho gọn
                if (!phatHienCoChu) {
                    toggleSwitch.checked = false;
                    isCustomOrder = false;
                    customSection.style.display = "none";
                }
            }
        });
    });

    // Tiến hành check dữ liệu sửa đơn
    checkEditMode();

    const form = document.getElementById('formAddOrder');
    if (form) form.addEventListener('submit', submitOrderForm);
});

// ==========================================
// 2. HÀM CHUYỂN ĐỔI CHẾ ĐỘ EDIT MODE - ĐIỀU PHỐI ĐÓNG/MỞ KHUNG THÔNG MINH
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
    let totalDonHangGoc = 0;
    let soTienGiamGoc = 0;

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
        const customSection = document.getElementById('formCustomOrderFields');
        const toggleBtn = document.getElementById('checkIsCustomOrder');

        if (resCheck.ok) {
            const resData = await resCheck.json();
            const laDonCustom = resData.laDonCustom !== undefined ? resData.laDonCustom : (resData.LaDonCustom !== undefined ? resData.LaDonCustom : laDonCustomGoc);
            const customData = resData.data || resData.Data || (resData.loaiYeuCau || resData.loai_yeu_cau || resData.LoaiYeuCau ? resData : null);

            // 🔥 QUY ĐỊNH BẢO MẬT: Nếu là đơn Custom thì mới được phép bật công tắc và mở khung vàng!
            if (laDonCustom || customData) {
                isCustomOrder = true;
                if (toggleBtn) toggleBtn.checked = true;
                if (customSection) customSection.style.display = "block";

                if (customData) {
                    document.getElementById('customLoaiYeuCau').value = customData.loaiYeuCau || customData.LoaiYeuCau || customData.loai_yeu_cau || '';

                    // 🎯 ĐÃ VÁ: Dò quét sạch cả chữ S viết hoa lẫn chữ s viết thường để lốt thuộc tính kích thước
                    document.getElementById('customKichThuoc').value = customData.kichThuocSoLuong || customData.KichThuocSoLuong || customData.kichThuocSoluong || customData.KichThuocSoluong || customData.kich_thuoc_soluong || '';

                    document.getElementById('customMauSac').value = customData.mauSacChuDao || customData.MauSacChuDao || customData.mau_sac_chu_dao || '';
                    document.getElementById('customGhiChu').value = customData.ghiChu || customData.GhiChu || customData.ghi_chu || '';

                    let giaCustomTinhToan = totalDonHangGoc + soTienGiamGoc - tongTienBanhThuongHienTai;
                    if (giaCustomTinhToan < 0) giaCustomTinhToan = 0;
                    document.getElementById('customBaoGia').value = giaCustomTinhToan;

                    const dateRaw = customData.ngayLayHang || customData.NgayLayHang || customData.ngay_lay_hang;
                    if (dateRaw) {
                        document.getElementById('customNgayLay').value = dateRaw.split('T')[0];
                    }

                    const linkAnh = customData.HinhANh || customData.hinhANh || customData.HinhAnh || customData.hinhAnh || customData.hinh_anh || '';
                    document.getElementById('customHinhAnh').value = linkAnh;

                    if (linkAnh && linkAnh !== 'string' && linkAnh.trim() !== '') {
                        const previewContainer = document.getElementById('editPreviewContainer');
                        const previewImg = document.getElementById('customImgPreview');
                        if (previewContainer && previewImg) {
                            previewImg.src = linkAnh;
                            previewContainer.style.display = "block";
                        }
                    }
                }
            }
            else {
                // 🔥 ĐÃ THÊM: Nếu là đơn hàng tiêu chuẩn (bánh có sẵn) -> Ép tắt công tắc gạt và ẩn biến mất khung vàng!
                isCustomOrder = false;
                if (toggleBtn) toggleBtn.checked = false;
                if (customSection) customSection.style.display = "none";
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
// 3. CÁC HÀM DROPDOWN NỀN (GIỮ NGUYÊN)
// ==========================================
async function loadKhachHangs() { const select = document.getElementById('selectKhachHang'); try { const res = await fetch(`${API_URL}/api/KhachHang/DanhSachKhachHang`); const data = await res.json(); select.innerHTML = `<option value="0">👤 Khách hàng vãng lai (Mua tại quầy)</option>`; data.forEach(kh => { select.innerHTML += `<option value="${kh.khachHangId || kh.Id}">${kh.tenKhachHang || kh.TenKhachHang} - ${kh.sdt || kh.Sdt}</option>`; }); } catch (e) { console.error(e); } }
async function loadSanPhams() { const select = document.getElementById('selectSanPham'); try { const res = await fetch(`${API_URL}/api/SanPham/HienThiDanhSachSanPham`); danhSachSanPhams = await res.json(); select.innerHTML = `<option value="">-- Chọn loại bánh --</option>`; danhSachSanPhams.forEach(sp => { const id = sp.sanPhamId || sp.Id || Object.values(sp)[0]; const ten = sp.tenSanPham || sp.TenSanPham; const maSP = `SP${id.toString().padStart(3, '0')}`; select.innerHTML += `<option value="${id}">[${maSP}] - ${ten}</option>`; }); } catch (e) { console.error(e); } }
async function loadKhuyenMais() { const select = document.getElementById('selectKhuyenMai'); try { const res = await fetch(`${API_URL}/api/KhuyenMai/HienThiCacMaKhuyenMai`); const data = await res.json(); select.innerHTML = `<option value="">-- Không áp dụng --</option>`; data.forEach(km => { select.innerHTML += `<option value="${km.khuyenMaiId || km.Id}">${km.maCode || km.MaCode} (Giảm ${km.phanTramGiam}%)</option>`; }); } catch (e) { console.error(e); } }
function addItemToCart() { if (isProductLocked) return; const selectSp = document.getElementById('selectSanPham'); const inputSl = document.getElementById('inputSoLuong'); const spId = Number(selectSp.value); const soLuongMuonThem = Number(inputSl.value); if (!spId || soLuongMuonThem < 1) { alert("Vui lòng chọn loại bánh và nhập số lượng!"); return; } const spTimThay = danhSachSanPhams.find(p => (p.sanPhamId || p.Id) == spId); const tonKhoThucTe = spTimThay ? (spTimThay.soLuongTon || spTimThay.SoLuongTon || spTimThay.so_luong_ton || 0) : 0; const monDaCoTrongGio = gioHangTam.find(item => item.SanPham_ID === spId); const tongSoLuongSauKhiThem = soLuongMuonThem + (monDaCoTrongGio ? monDaCoTrongGio.So_Luong : 0); if (tongSoLuongSauKhiThem > tonKhoThucTe) { alert(`⚠️ LỖI TỒN KHO: Không thể thêm! Loại bánh này trong kho chỉ còn đúng [ ${tonKhoThucTe} ] cái.`); return; } if (monDaCoTrongGio) { monDaCoTrongGio.So_Luong = tongSoLuongSauKhiThem; } else { const tenSp = selectSp.options[selectSp.selectedIndex].text; gioHangTam.push({ SanPham_ID: spId, Ten: tenSp, So_Luong: soLuongMuonThem }); } renderCartTable(); }
function removeItemFromCart(index) { if (isProductLocked) return; gioHangTam.splice(index, 1); renderCartTable(); }
function renderCartTable() { const tbody = document.getElementById('cartTableBody'); tbody.innerHTML = ''; if (gioHangTam.length === 0) { tbody.innerHTML = `<tr><td colspan="3" class="text-center text-muted py-2">Chưa có sản phẩm nào trong giỏ hàng</td></tr>`; return; } gioHangTam.forEach((item, index) => { const actionBtn = isProductLocked ? `<button type="button" class="btn btn-sm btn-icon btn-outline-secondary" disabled title="Món bánh đã khóa tiến độ"><i class="bx bx-lock-alt"></i></button>` : `<button type="button" class="btn btn-sm btn-icon btn-outline-danger" onclick="removeItemFromCart(${index})"><i class="bx bx-trash"></i></button>`; tbody.innerHTML += `<tr><td><strong>${item.Ten}</strong></td><td class="text-center"><span class="badge bg-label-info">${item.So_Luong}</span></td><td>${actionBtn}</td></tr>`; }); }

// ==========================================
// 4. LƯU TRỌN GÓI ĐƠN HÀNG (ĐÃ TÍCH HỢP ĐỌC LỖI TIẾNG VIỆT)
// ==========================================
async function submitOrderForm(event) {
    event.preventDefault();

    const tenNguoiDangNhap = localStorage.getItem('userName') || "Quản trị viên";
    const idNguoiDangNhap = localStorage.getItem('userId') || 1;

    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');

    const congTacMoiNhat = document.getElementById('checkIsCustomOrder');
    const giaTriCustomThucTe = congTacMoiNhat ? congTacMoiNhat.checked : isCustomOrder;

    const khachHangVal = $('#selectKhachHang').val();
    const promoValue = $('#selectKhuyenMai').val();

    if (!giaTriCustomThucTe && gioHangTam.length === 0) {
        alert("Giỏ hàng trống! Vui lòng chọn bánh tiêu chuẩn hoặc điền thông tin Đơn bánh thiết kế riêng.");
        return;
    }

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

        ChiTietGioHang: gioHangTam.map(item => ({
            SanPham_ID: item.SanPham_ID,
            So_Luong: item.So_Luong
        })),

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
            if (err.errors) {
                let thongBaoLoi = "⚠️ HỆ THỐNG TỪ CHỐI LƯU VÌ CÁC LỖI SAU:\n\n";
                for (let key in err.errors) {
                    thongBaoLoi += "❌ " + err.errors[key][0] + "\n";
                }
                alert(thongBaoLoi);
            } else {
                alert(err.message || err.Message || "Lưu dữ liệu thất bại!");
            }
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

// ==========================================
// 5. HÀM TỰ ĐỘNG UPLOAD ẢNH KHI NGƯỜI DÙNG CHỌN FILE
// ==========================================
async function uploadFileTuDong() {
    const fileInput = document.getElementById('fileInputCustom');
    const hiddenInput = document.getElementById('customHinhAnh');
    const statusText = document.getElementById('uploadStatus');
    const previewContainer = document.getElementById('editPreviewContainer');
    const previewImg = document.getElementById('customImgPreview');

    // Nếu không chọn file gì thì bỏ qua
    if (!fileInput.files || fileInput.files.length === 0) return;

    const file = fileInput.files[0];

    // Gói file vào form data để gửi đi
    const formData = new FormData();
    formData.append('file', file); // 'file' là tên tham số trùng với IFormFile file trong C#

    statusText.innerText = "⏳ Đang tải ảnh lên server...";
    statusText.className = "text-warning fw-bold d-block mt-1";

    try {
        // Gọi API Upload bên DonHangController
        const response = await fetch(`${API_URL}/api/DonHang/UploadAnhMauCustom`, {
            method: 'POST',
            body: formData
            // Lưu ý: Không set Header 'Content-Type' ở đây, trình duyệt sẽ tự động set boundary cho file
        });

        if (response.ok) {
            const result = await response.json();

            // Lấy link ảnh từ API trả về gắn vào ô input ẩn để chuẩn bị bấm Lưu đơn
            const imgUrl = result.linkAnh || result.LinkAnh;
            hiddenInput.value = imgUrl;

            statusText.innerText = "✅ Tải ảnh lên thành công!";
            statusText.className = "text-success fw-bold d-block mt-1";

            // Cập nhật luôn cái khung Preview cho nhân viên thấy ảnh mới vừa đổi
            if (previewContainer && previewImg) {
                previewImg.src = imgUrl;
                previewContainer.style.display = "block";
                previewContainer.querySelector('small').innerText = "Ảnh mẫu chuẩn bị lưu thay thế:";
            }
        } else {
            statusText.innerText = "❌ Lỗi: Server từ chối file ảnh!";
            statusText.className = "text-danger fw-bold d-block mt-1";
        }
    } catch (error) {
        console.error("Lỗi upload:", error);
        statusText.innerText = "❌ Mất kết nối đến Server tải ảnh!";
        statusText.className = "text-danger fw-bold d-block mt-1";
    }
}