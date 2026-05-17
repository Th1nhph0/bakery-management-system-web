
// ==========================================
// 1. HÀM ĐĂNG NHẬP (LOGIN) - BẢN SMART SCANNER CHỐNG LỆCH DTO BACKEND
// ==========================================
async function handleLogin(event) {
    event.preventDefault();
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    try {
        const response = await fetch(`${API_URL}/api/Auth/Login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: email, matKhau: password })
        });
        if (response.ok) {
            const data = await response.json();

            // Xóa sạch bộ nhớ cũ trước khi lưu người mới để tránh rác session
            localStorage.clear();

            // 🔍 THUẬT TOÁN QUÈT SÂU ĐỆ QUY: Tự động dò tìm từ khóa xuyên thấu object lồng nhau của C#
            function findValueSmart(obj, keywords) {
                if (!obj || typeof obj !== 'object') return null;
                // Tầng 1: Tìm ngay ở tầng gốc
                for (let key in obj) {
                    if (keywords.some(kw => key.toLowerCase().includes(kw))) {
                        if (typeof obj[key] !== 'object') return obj[key];
                    }
                }
                // Tầng 2: Nếu không thấy, đệ quy quét sâu vào các object con bên trong
                for (let key in obj) {
                    if (typeof obj[key] === 'object') {
                        let found = findValueSmart(obj[key], keywords);
                        if (found) return found;
                    }
                }
                return null;
            }

            // Tiến hành dò tìm thông minh bất chấp định dạng trả về từ API Login
            const tenThucTe = findValueSmart(data, ['ten', 'name', 'hoten', 'fullname']) || 'Thành viên mới';
            const quyenThucTe = findValueSmart(data, ['chucvu', 'chuc_vu', 'role', 'quyen', 'vaitro']) || 'Nhân viên';
            const idChuanXac = findValueSmart(data, ['nhanvienid', 'userid', 'id']) || '';

            // Lưu trữ chắc chắn vào bộ nhớ trình duyệt
            localStorage.setItem('userName', tenThucTe);
            localStorage.setItem('userRole', quyenThucTe);
            localStorage.setItem('userId', idChuanXac);

            // Bật cờ báo hiệu hiện thông báo chào mừng ngọt ngào ở trang index
            localStorage.setItem('showWelcome', 'true');

            window.location.href = 'index.html';
        } else {
            alert("Sai email hoặc mật khẩu!");
        }
    } catch (error) { console.error("Lỗi đăng nhập:", error); }
}

// ==========================================
// 2. HÀM THÊM / SỬA HỒ SƠ NHÂN VIÊN
// ==========================================
async function handleAddEmployee(event) {
    event.preventDefault();

    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');
    const currentRole = localStorage.getItem('userRole');

    const passTaoMoi = document.getElementById('matKhauTaoMoi') ? document.getElementById('matKhauTaoMoi').value : '';
    const passCu = document.getElementById('matKhauCu') ? document.getElementById('matKhauCu').value : '';
    const passMoi = document.getElementById('matKhauMoi') ? document.getElementById('matKhauMoi').value : '';
    const passXacNhan = document.getElementById('xacNhanMatKhau') ? document.getElementById('xacNhanMatKhau').value : '';

    let matKhauGuiDi = "";

    if (editId) {
        if (passMoi !== "") {
            if (passMoi !== passXacNhan) {
                alert("Mật khẩu mới và Xác nhận mật khẩu không khớp nhau!");
                return;
            }
            matKhauGuiDi = passMoi;
        }
    } else {
        matKhauGuiDi = passTaoMoi;
    }

    const employeeData = {
        TenNhanVien: document.getElementById('tenNhanVien').value,
        Sdt: document.getElementById('sdtNV').value,
        Email: document.getElementById('emailNV').value,
        ChucVu: document.getElementById('chucVu').value,
        MatKhau: matKhauGuiDi,
        MatKhauCu: passCu,
        RoleNguoiSua: currentRole
    };

    const method = editId ? 'PUT' : 'POST';
    const apiUrl = editId
        ? `${API_URL}/api/NhanVien/CapNhatNhanVien/${editId}`
        : `${API_URL}/api/NhanVien/ThemNhanVienMoi`;

    try {
        const response = await fetch(apiUrl, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(employeeData)
        });

        if (response.ok) {
            alert(editId ? "Cập nhật thành công!" : "Thêm nhân viên thành công!");
            window.location.href = 'list-employee.html';
        } else {
            const errorData = await response.json();
            alert("Thất bại! " + (errorData.message || "Vui lòng kiểm tra lại dữ liệu nhập vào."));
        }
    } catch (error) { console.error("Lỗi gửi form nhân viên:", error); }
}

// ==========================================
// 3. HÀM NẠP VÀ ĐỔ DANH SÁCH RA BẢNG TRANG LIST
// ==========================================
async function loadEmployees() {
    const currentRole = localStorage.getItem('userRole');
    const currentUserId = String(localStorage.getItem('userId'));

    // Chuẩn hóa chống lệch chữ HOA / thường từ SQL Server
    const roleLower = currentRole ? currentRole.toLowerCase().trim() : '';
    const isBoss = roleLower === 'admin' || roleLower === 'chủ quán' || roleLower === 'chuquan' || roleLower === 'quản trị web';

    const btnThem = document.getElementById('btnThemNhanVien');
    if (btnThem && isBoss) {
        btnThem.style.display = 'inline-block';
    }

    const tableBody = document.getElementById('employeeTableBody');
    if (!tableBody) return;

    try {
        const response = await fetch(`${API_URL}/api/NhanVien/HienThiDanhSachNhanVien`);
        const data = await response.json();

        tableBody.innerHTML = '';
        data.forEach(nv => {
            const tenCotId = Object.keys(nv).find(key => key.toLowerCase().includes('id'));
            const idChuẩn = String(tenCotId ? nv[tenCotId] : '');

            let actionButtons = '';

            // 🛡️ PHÂN QUYỀN GIAO DIỆN NÚT BẤM TUYỆT ĐỐI
            if (isBoss) {
                // Nếu là CẤP QUẢN LÝ (Admin/Chủ Quán): Hiện đầy đủ cả SỬA và XÓA cho tất cả mọi người!
                actionButtons = `
                    <button class="btn btn-sm btn-icon btn-outline-warning" onclick="editEmp(${idChuẩn})" title="Sửa hồ sơ">
                        <i class="bx bx-edit"></i>
                    </button>
                    <button class="btn btn-sm btn-icon btn-outline-danger ms-1" onclick="deleteEmp(${idChuẩn})" title="Xóa nhân sự">
                        <i class="bx bx-trash"></i>
                    </button>
                `;
            } else if (currentUserId === idChuẩn && currentUserId !== "") {
                // Nếu là NHÂN VIÊN THƯỜNG: Chỉ được thấy duy nhất nút Sửa ở dòng của chính mình để tự cập nhật thông tin
                actionButtons = `
                    <button class="btn btn-sm btn-icon btn-outline-warning" onclick="editEmp(${idChuẩn})" title="Sửa thông tin cá nhân">
                        <i class="bx bx-edit"></i>
                    </button>
                `;
            } else {
                // Các dòng của đồng nghiệp khác: Khóa cứng hiển thị Không có quyền
                actionButtons = `<span class="badge bg-label-secondary">Không có quyền</span>`;
            }

            tableBody.innerHTML += `               
                <tr>
                    <td>${nv.maNVHienThi || nv.ma_NV_HienThi || 'NV' + idChuẩn}</td>
                    <td><strong>${nv.tenNhanVien || nv.ten_Nhan_Vien}</strong></td>
                    <td>${nv.email}</td>
                    <td>${nv.sdt || '---'}</td>
                    <td><span class="badge bg-label-primary">${nv.chucVu || nv.chuc_Vu}</span></td>
                    <td>${actionButtons}</td> 
                </tr>`;
        });
    } catch (error) { console.error("Lỗi nạp bảng nhân sự:", error); }
}

// ==========================================
// 4. HÀM XÓA NHÂN VIÊN
// ==========================================
async function deleteEmp(id) {
    if (confirm("Bạn có chắc chắn muốn xóa vĩnh viễn nhân viên này khỏi hệ thống?")) {
        try {
            const response = await fetch(`${API_URL}/api/NhanVien/XoaNhanVien/${id}`, { method: 'DELETE' });
            if (response.ok) {
                alert("Xóa nhân viên thành công!");
                loadEmployees();
            } else {
                const errorData = await response.json();
                alert(errorData.message || "Không thể thực hiện xóa nhân viên này!");
            }
        } catch (error) { console.error(error); }
    }
}

// ==========================================
// 5. ĐIỀU HƯỚNG FORM SỬA
// ==========================================
function editEmp(id) {
    window.location.href = `add-employee.html?id=${id}`;
}

// ==========================================
// 6. KHỞI CHẠY HỆ THỐNG SỰ KIỆN DOM
// ==========================================
document.addEventListener('DOMContentLoaded', async () => {
    const loginForm = document.getElementById('formAuthentication');
    if (loginForm) loginForm.addEventListener('submit', handleLogin);

    const empForm = document.getElementById('formAddEmployee');
    if (empForm) empForm.addEventListener('submit', handleAddEmployee);

    if (document.getElementById('employeeTableBody')) {
        loadEmployees();
    }

    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');

    const formTitle = document.querySelector('.card-header h5');
    const submitBtn = document.querySelector('button[type="submit"]');

    if (editId) {
        const currentRole = localStorage.getItem('userRole');
        const currentUserId = String(localStorage.getItem('userId'));
        const targetId = String(editId);

        const roleLower = currentRole ? currentRole.toLowerCase().trim() : '';
        const isBoss = roleLower === 'admin' || roleLower === 'chủ quán' || roleLower === 'chuquan' || roleLower === 'quản trị web';

        if (!isBoss && currentUserId !== targetId) {
            alert("⚠️ Cảnh báo bảo mật: Bạn không có quyền chỉnh sửa hồ sơ tài khoản của người khác!");
            window.location.href = 'index.html';
            return;
        }

        const khuVucCV = document.getElementById('khuVucChucVu');
        const chucVuInput = document.getElementById('chucVu');
        if (!isBoss) {
            if (chucVuInput) chucVuInput.disabled = true;
            if (khuVucCV) khuVucCV.style.display = 'none';
        } else {
            if (chucVuInput) chucVuInput.disabled = false;
            if (khuVucCV) khuVucCV.style.display = 'block';
        }

        if (formTitle) formTitle.innerText = "Cập Nhật Thông Tin Nhân Viên";

        const khuVucPassMoi = document.getElementById('khuVucPassTaoMoi');
        if (khuVucPassMoi) khuVucPassMoi.style.display = 'none';

        const khuVucDoiP = document.getElementById('khuVucDoiPass');
        if (khuVucDoiP) khuVucDoiP.style.display = 'block';

        if (submitBtn) {
            submitBtn.innerText = "Lưu Cập Nhật";
            submitBtn.classList.replace('btn-primary', 'btn-warning');
        }

        try {
            const response = await fetch(`${API_URL}/api/NhanVien/LayNhanVien/${editId}`);
            if (response.ok) {
                const data = await response.json();
                document.getElementById('tenNhanVien').value = data.tenNhanVien || data.ten_Nhan_Vien;
                document.getElementById('sdtNV').value = data.sdt;
                document.getElementById('emailNV').value = data.email;
                if (chucVuInput) chucVuInput.value = data.chucVu || data.chuc_Vu;
            }
        } catch (error) { console.error("Lỗi lấy thông tin sửa:", error); }

    } else if (empForm) {
        if (formTitle) formTitle.innerText = "Tạo Hồ Sơ Nhân Viên Mới";

        const khuVucPassMoi = document.getElementById('khuVucPassTaoMoi');
        if (khuVucPassMoi) khuVucPassMoi.style.display = 'block';

        const khuVucDoiP = document.getElementById('khuVucDoiPass');
        if (khuVucDoiP) khuVucDoiP.style.display = 'none';

        if (submitBtn) {
            submitBtn.innerText = "Tạo Tài Khoản";
            submitBtn.classList.replace('btn-warning', 'btn-primary');
        }
    }
});