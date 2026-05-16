const API_URL = "https://localhost:7122";

// 1. HÀM LOGIN
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

            // Xóa sạch bộ nhớ cũ trước khi lưu người mới
            localStorage.clear();

            // Bắt mọi thể loại tên biến C# có thể trả về
            localStorage.setItem('userRole', data.role || data.Role || data.chucVu || data.chuc_Vu);
            localStorage.setItem('userId', data.id || data.Id || data.nhanVienId || data.nhan_Vien_ID);
            localStorage.setItem('userName', data.ten || data.Ten || data.tenNhanVien);

            window.location.href = 'index.html';
        } else {
            alert("Sai email hoặc mật khẩu!");
        }
    } catch (error) { console.error(error); }
}

// 2. HÀM THÊM / SỬA NHÂN VIÊN
async function handleAddEmployee(event) {
    event.preventDefault();

    // 🔥 SỬA LỖI TẠI ĐÂY: Đưa phần lấy editId lên trên cùng để xài cho toàn bộ hàm!
    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');

    // Lấy Role từ bộ nhớ để chuẩn bị gửi cho C#
    const currentRole = localStorage.getItem('userRole');

    // Lấy giá trị các ô Pass
    const passTaoMoi = document.getElementById('matKhauTaoMoi') ? document.getElementById('matKhauTaoMoi').value : '';
    const passCu = document.getElementById('matKhauCu') ? document.getElementById('matKhauCu').value : '';
    const passMoi = document.getElementById('matKhauMoi') ? document.getElementById('matKhauMoi').value : '';
    const passXacNhan = document.getElementById('xacNhanMatKhau') ? document.getElementById('xacNhanMatKhau').value : '';

    let matKhauGuiDi = "";

    // KIỂM TRA LOGIC MẬT KHẨU
    if (editId) {
        if (passMoi !== "") { // Có nhu cầu đổi pass
            if (passMoi !== passXacNhan) {
                alert("Mật khẩu mới và Xác nhận mật khẩu không khớp nhau!");
                return; // Dừng luôn, không cho gọi API
            }
            matKhauGuiDi = passMoi;
        }
    } else {
        matKhauGuiDi = passTaoMoi; // Đang tạo mới
    }

    // Đóng gói gửi đi
    const employeeData = {
        TenNhanVien: document.getElementById('tenNhanVien').value,
        Sdt: document.getElementById('sdtNV').value,
        Email: document.getElementById('emailNV').value,
        ChucVu: document.getElementById('chucVu').value,

        MatKhau: matKhauGuiDi,       // Pass mới (hoặc pass tạo tài khoản)
        MatKhauCu: passCu,           // Báo cáo pass cũ cho C#
        RoleNguoiSua: currentRole    // Trình thẻ bài cho C# biết ai đang sửa
    };

    // Xác định phương thức và API
    const method = editId ? 'PUT' : 'POST';
    const apiUrl = editId
        ? `https://localhost:7122/api/NhanVien/CapNhatNhanVien/${editId}`
        : `https://localhost:7122/api/NhanVien/ThemNhanVienMoi`;

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
            alert("Thất bại! " + (errorData.message || "Vui lòng kiểm tra lại dữ liệu."));
        }
    } catch (error) {
        console.error("Lỗi:", error);
    }
}
// 3. HÀM ĐỔ DỮ LIỆU RA BẢNG
async function loadEmployees() {
    const currentRole = localStorage.getItem('userRole');
    const currentUserId = String(localStorage.getItem('userId')); // Ép kiểu chuỗi để so sánh
    const isBoss = (currentRole === 'Admin' || currentRole === 'Quản trị web' || currentRole === 'Chủ quán' || currentRole === 'ChuQuan');

    // (Tui đã tháo cái lệnh đá văng về index.html ra để nhân viên vào được bảng)
    const btnThem = document.getElementById('btnThemNhanVien');
    if (btnThem && isBoss) {
        btnThem.style.display = 'inline-block'; // Nếu là sếp thì cho hiện nút lên công khai
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

            // 🔥 TUYỆT CHIÊU HIỂN THỊ NÚT BẤM THEO QUYỀN
            let actionButtons = '';

            if (isBoss) {
                // Sếp thì được thấy nút Sửa và Xóa của TẤT CẢ mọi người
                actionButtons = `
                    <button class="btn btn-sm btn-icon btn-outline-warning" onclick="editEmp(${idChuẩn})">
                        <i class="bx bx-edit"></i>
                    </button>
                    <button class="btn btn-sm btn-icon btn-outline-danger" onclick="deleteEmp(${idChuẩn})">
                        <i class="bx bx-trash"></i>
                    </button>
                `;
            } else if (currentUserId === idChuẩn) {
                // Nhân viên thường thì CHỈ THẤY NÚT SỬA ở đúng dòng tên của chính mình
                actionButtons = `
                    <button class="btn btn-sm btn-icon btn-outline-warning" onclick="editEmp(${idChuẩn})">
                        <i class="bx bx-edit"></i>
                    </button>
                `;
            } else {
                // Dòng của người khác thì khóa lại, không cho bấm
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
    } catch (error) { console.error("Lỗi load bảng:", error); }
}
// 4. HÀM XÓA
async function deleteEmp(id) {
    if (confirm("Chắc chắn muốn xóa nhân viên này?")) {
        try {
            const response = await fetch(`${API_URL}/api/NhanVien/XoaNhanVien/${id}`, { method: 'DELETE' });

            if (response.ok) {
                alert("Xóa thành công!");
                loadEmployees();
            } else {
                const errorData = await response.json();
                alert(errorData.message || "Không thể xóa nhân viên này!");
            }
        } catch (error) { console.error(error); }
    }
}

// 5. HÀM CHUYỂN TRANG SỬA
function editEmp(id) {
    window.location.href = `add-employee.html?id=${id}`;
}


/// ==========================================
// 6. GỘP TOÀN BỘ SỰ KIỆN VÀO 1 CHỖ DUY NHẤT
// ==========================================
document.addEventListener('DOMContentLoaded', async () => {
    // A. Gắn sự kiện cho Form Login & Form Thêm
    const loginForm = document.getElementById('formAuthentication');
    if (loginForm) loginForm.addEventListener('submit', handleLogin);

    const empForm = document.getElementById('formAddEmployee');
    if (empForm) empForm.addEventListener('submit', handleAddEmployee);

    // B. Gọi hàm nạp bảng (nếu đang ở trang list-employee)
    if (document.getElementById('employeeTableBody')) {
        loadEmployees();
    }

    // C. Logic Form Cập Nhật / Thêm Mới
    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');

    // Tìm sẵn các thẻ HTML cần đổi chữ (chỉ tìm nếu đang ở trang form)
    const formTitle = document.querySelector('.card-header');
    const submitBtn = document.querySelector('button[type="submit"]');
    const passLabel = document.querySelector('label[for="matKhauNV"]');

    if (editId) {
        // -----------------------------------------------------
        // KỊCH BẢN 1: ĐANG Ở CHẾ ĐỘ "SỬA" (Có ID trên link)
        // -----------------------------------------------------
        const currentRole = localStorage.getItem('userRole');
        const currentUserId = String(localStorage.getItem('userId'));
        const targetId = String(editId);

        const isBoss = (currentRole === 'Admin' || currentRole === 'Quản trị web' || currentRole === 'Chủ quán' || currentRole === 'ChuQuan');

        // 1. CHỐNG HACK SỬA CHÉO
        if (!isBoss && currentUserId !== targetId) {
            alert("Tính hack hệ thống hả ông thần? Chỉ được sửa hồ sơ của mình thôi!");
            window.location.href = 'index.html';
            return;
        }

        // 2. KHÓA CHỨC VỤ NẾU LÀ NHÂN VIÊN
        if (!isBoss) {
            const chucVuInput = document.getElementById('chucVu');
            if (chucVuInput) chucVuInput.disabled = true;

            const khuVuc = document.getElementById('khuVucChucVu');
            if (khuVuc) khuVuc.style.display = 'none';
        }

        // 3. ĐỔI GIAO DIỆN SANG CẬP NHẬT
        if (formTitle) formTitle.innerText = "Cập Nhật Thông Tin Nhân Viên";
        if (passLabel) passLabel.innerText = "MẬT KHẨU (BỎ TRỐNG NẾU KHÔNG MUỐN ĐỔI)";
        if (submitBtn) {
            submitBtn.innerText = "Lưu Cập Nhật";
            submitBtn.classList.replace('btn-primary', 'btn-warning');
        }

        // 4. API LẤY DỮ LIỆU CŨ ĐỔ VÀO FORM
        try {
            const response = await fetch(`https://localhost:7122/api/NhanVien/LayNhanVien/${editId}`);
            if (response.ok) {
                const data = await response.json();

                document.getElementById('tenNhanVien').value = data.tenNhanVien || data.ten_Nhan_Vien;
                document.getElementById('sdtNV').value = data.sdt;
                document.getElementById('emailNV').value = data.email;

                const chucVuInput = document.getElementById('chucVu');
                if (chucVuInput) chucVuInput.value = data.chucVu || data.chuc_Vu;
            }
        } catch (error) {
            console.error("Lỗi lấy thông tin sửa:", error);
        }

    } else if (empForm) {
        // -----------------------------------------------------
        // KỊCH BẢN 2: ĐANG Ở CHẾ ĐỘ "THÊM MỚI" (Không có ID)
        // -----------------------------------------------------
        if (formTitle) formTitle.innerText = "Tạo Hồ Sơ Nhân Viên Mới";
        if (passLabel) passLabel.innerText = "MẬT KHẨU (BẮT BUỘC)";
        if (submitBtn) {
            submitBtn.innerText = "Tạo Tài Khoản";
            submitBtn.classList.replace('btn-warning', 'btn-primary'); // Đảm bảo nút màu xanh
        }
    }
});