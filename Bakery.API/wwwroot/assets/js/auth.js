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
            localStorage.setItem('userRole', data.role);
            localStorage.setItem('userId', data.id);
            localStorage.setItem('userName', data.ten);
            localStorage.setItem('showWelcome', 'true');
            window.location.href = 'index.html';
        } else {
            alert("Sai email hoặc mật khẩu!");
        }
    } catch (error) { console.error(error); }
}

// 2. HÀM THÊM NHÂN VIÊN (Dùng đúng Route: ThemNhanVienMoi)
async function handleAddEmployee(event) {
    event.preventDefault();
    const newEmployee = {
        TenNhanVien: document.getElementById('tenNhanVien').value,
        Sdt: document.getElementById('sdtNV').value,
        Email: document.getElementById('emailNV').value,
        MatKhau: document.getElementById('matKhauNV').value,
        ChucVu: document.getElementById('chucVu').value
    };

    try {
        const response = await fetch(`${API_URL}/api/NhanVien/ThemNhanVienMoi`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(newEmployee)
        });

        if (response.ok) {
            alert("Thêm nhân viên thành công!");
            document.getElementById('formAddEmployee').reset();
            // Nếu đang ở trang danh sách thì load lại bảng
            if (document.getElementById('employeeTableBody')) loadEmployees();
        } else {
            alert("Thất bại! Kiểm tra lại dữ liệu.");
        }
    } catch (error) { console.error(error); }
}

// 3. HÀM ĐỔ DỮ LIỆU RA BẢNG (Dùng đúng Route: HienThiDanhSachNhanVien)
async function loadEmployees() {
    const tableBody = document.getElementById('employeeTableBody');
    if (!tableBody) return;

    try {
        const response = await fetch(`${API_URL}/api/NhanVien/HienThiDanhSachNhanVien`);
        const data = await response.json();

        tableBody.innerHTML = '';
        data.forEach(nv => {
            // LƯU Ý: Soi kỹ Swagger xem 'tenNhanVien' hay 'ten_Nhan_Vien' để sửa ở đây
            tableBody.innerHTML += `
                <tr>
                    <td>${nv.maNVHienThi || nv.ma_NV_HienThi || 'NV' + nv.nhanVienId || 'NV' + nv.nhan_Vien_ID}</td>
                    <td><strong>${nv.tenNhanVien || nv.ten_Nhan_Vien}</strong></td>
                    <td>${nv.email}</td>
                    <td>${nv.sdt || '---'}</td>
                    <td><span class="badge bg-label-primary">${nv.chucVu || nv.chuc_Vu}</span></td>
                    <td>
                        <button class="btn btn-sm btn-icon btn-outline-warning" onclick="editEmp(${nv.nhan_Vien_ID})">
                            <i class="bx bx-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-icon btn-outline-danger" onclick="deleteEmp(${nv.nhan_Vien_ID})">
                            <i class="bx bx-trash"></i>
                        </button>
                    </td>
                </tr>`;
        });
    } catch (error) { console.error("Lỗi load bảng:", error); }
}

// 4. HÀM XÓA (Dùng đúng Route: XoaNhanVien)
async function deleteEmp(id) {
    if (confirm("Chắc chắn muốn xóa nhân viên này?")) {
        try {
            const response = await fetch(`${API_URL}/api/NhanVien/XoaNhanVien/${id}`, { method: 'DELETE' });
            if (response.ok) {
                alert("Xóa thành công!");
                loadEmployees();
            }
        } catch (error) { console.error(error); }
    }
}

// 5. HÀM SỬA
function editEmp(id) {
    window.location.href = `add-employee.html?id=${id}`;
}

// KHỞI TẠO SỰ KIỆN
document.addEventListener('DOMContentLoaded', () => {
    loadEmployees();
    const loginForm = document.getElementById('formAuthentication');
    if (loginForm) loginForm.addEventListener('submit', handleLogin);
    const empForm = document.getElementById('formAddEmployee');
    if (empForm) empForm.addEventListener('submit', handleAddEmployee);
});