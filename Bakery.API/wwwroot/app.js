const API_BASE = '/api/SanPham';

const state = {
  allProducts: [],
  filteredProducts: [],
  activeCategory: 'all',
  searchTerm: '',
  selectedProduct: null,
  cart: loadCart(),
};

const elements = {
  productGrid: document.getElementById('productGrid'),
  productCount: document.getElementById('productCount'),
  cartCount: document.getElementById('cartCount'),
  cartList: document.getElementById('cartList'),
  cartTotal: document.getElementById('cartTotal'),
  searchInput: document.getElementById('searchInput'),
  categoryGroup: document.getElementById('categoryGroup'),
  detailTitle: document.getElementById('detailTitle'),
  detailState: document.getElementById('detailState'),
  detailCard: document.getElementById('detailCard'),
  detailArt: document.getElementById('detailArt'),
  detailCategory: document.getElementById('detailCategory'),
  detailCode: document.getElementById('detailCode'),
  detailPrice: document.getElementById('detailPrice'),
  detailStock: document.getElementById('detailStock'),
  detailDescription: document.getElementById('detailDescription'),
  addToCartBtn: document.getElementById('addToCartBtn'),
  clearCartBtn: document.getElementById('clearCartBtn'),
  loadingState: document.getElementById('loadingState'),
};

const template = document.getElementById('productCardTemplate');

const categoryMatchers = {
  all: () => true,
  'banh-san': (product) => matchCategory(product, ['banh', 'cake', 'bread', 'ngot', 'san']),
  'nguyen-lieu': (product) => matchCategory(product, ['nguyen lieu', 'nguyenlieu', 'ingredient', 'bột', 'bot']),
  'dung-cu': (product) => matchCategory(product, ['dung cu', 'dungcu', 'tool', 'phu kien', 'phụ kiện']),
};

init();

function init() {
  bindEvents();
  renderCart();
  loadProducts();
}

function bindEvents() {
  elements.searchInput.addEventListener('input', (event) => {
    state.searchTerm = event.target.value;
    applyFilters();
  });

  elements.categoryGroup.addEventListener('click', (event) => {
    const button = event.target.closest('button[data-category]');
    if (!button) return;

    state.activeCategory = button.dataset.category;
    document.querySelectorAll('.category-btn').forEach((item) => item.classList.toggle('active', item === button));
    applyFilters();
  });

  elements.addToCartBtn.addEventListener('click', () => {
    if (!state.selectedProduct) return;
    addToCart(state.selectedProduct);
  });

  elements.clearCartBtn.addEventListener('click', () => {
    state.cart = [];
    persistCart();
    renderCart();
  });

  const checkoutBtn = document.getElementById('checkoutBtn');
  if (checkoutBtn) {
    checkoutBtn.addEventListener('click', async () => {
        if (state.cart.length === 0) {
            alert('Giỏ hàng trống!');
            return;
        }

        const form = document.getElementById('checkoutForm');
        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }

        const payload = {
            Khach_Hang_ID: null,
            Nhan_Vien_ID: null,
            Khuyen_Mai_ID: null,
            Ten_Nguoi_Nhan: document.getElementById('orderName').value,
            SDT_Nguoi_Nhan: document.getElementById('orderPhone').value,
            Dia_Chi_Giao: document.getElementById('orderAddress').value,
            ChiTietGioHang: state.cart.map(c => ({
                SanPham_ID: c.sanPhamId,
                So_Luong: c.quantity
            }))
        };

        const msgEl = document.getElementById('checkoutMessage');
        msgEl.className = 'mt-2 text-primary';
        msgEl.textContent = 'Đang xử lý đặt hàng...';
        checkoutBtn.disabled = true;

        try {
            const response = await fetch('/api/DonHang/TaoDonHang', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            const data = await response.json();
            if (response.ok) {
                // Nhận kết quả từ API, hiện thông báo "Đặt thành công mã đơn #123" hoặc báo lỗi.
                const orderId = data.donHangId || data.DonHangId;
                msgEl.className = 'mt-2 text-success fw-bold';
                msgEl.textContent = data.message || `Đặt thành công mã đơn #${orderId}`;
                
                state.cart = [];
                persistCart();
                renderCart();
                form.reset();
                form.classList.remove('was-validated');
            } else {
                msgEl.className = 'mt-2 text-danger';
                msgEl.textContent = data.message || 'Lỗi đặt hàng!';
            }
        } catch (error) {
            msgEl.className = 'mt-2 text-danger';
            msgEl.textContent = 'Lỗi kết nối máy chủ.';
        } finally {
            checkoutBtn.disabled = false;
        }
    });
  }
}

async function loadProducts() {
  setLoading(true, 'Đang tải...');

  try {
    const response = await fetch(`${API_BASE}/HienThiDanhSachSanPham`);
    if (!response.ok) {
      throw new Error(`Không tải được danh sách sản phẩm (${response.status})`);
    }

    state.allProducts = await response.json();
    elements.productCount.textContent = state.allProducts.length;
    applyFilters();
    setLoading(false, 'Sẵn sàng');
  } catch (error) {
    console.error(error);
    elements.productGrid.innerHTML = `<div class="empty-state">Không tải được danh sách sản phẩm. Kiểm tra API và thử lại.</div>`;
    setLoading(true, 'Lỗi tải dữ liệu');
  }
}

function applyFilters() {
  const categoryMatcher = categoryMatchers[state.activeCategory] || categoryMatchers.all;
  const searchTerm = normalizeText(state.searchTerm);

  state.filteredProducts = state.allProducts.filter((product) => {
    const matchesCategory = categoryMatcher(product);
    const matchesSearch = !searchTerm || normalizeText(`${product.tenSanPham ?? ''} ${product.phanLoai ?? ''}`).includes(searchTerm);
    return matchesCategory && matchesSearch;
  });

  renderProducts();
}

function renderProducts() {
  if (!state.filteredProducts.length) {
    elements.productGrid.innerHTML = `<div class="empty-state">Không có sản phẩm nào khớp với bộ lọc hiện tại.</div>`;
    return;
  }

  const fragment = document.createDocumentFragment();

  state.filteredProducts.forEach((product) => {
    const card = template.content.firstElementChild.cloneNode(true);

    card.querySelector('.product-avatar').textContent = getInitial(product.tenSanPham);
    card.querySelector('.product-category').textContent = product.phanLoai ?? 'Chưa phân loại';
    card.querySelector('.product-name').textContent = product.tenSanPham ?? 'Không có tên';
    card.querySelector('.product-desc').textContent = product.moTa || 'Chưa có mô tả.';
    card.querySelector('.product-price').textContent = formatCurrency(product.donGiaBan);
    card.querySelector('.product-stock').textContent = `Tồn: ${product.soLuongTon ?? 0}`;

    card.addEventListener('click', () => openDetail(product.sanPhamId));
    card.addEventListener('keydown', (event) => {
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        openDetail(product.sanPhamId);
      }
    });

    fragment.appendChild(card);
  });

  elements.productGrid.innerHTML = '';
  elements.productGrid.appendChild(fragment);
}

async function openDetail(productId) {
  elements.detailState.classList.add('hidden');
  elements.detailCard.classList.remove('hidden');
  elements.detailTitle.textContent = 'Đang tải chi tiết...';
  
  // Show modal
  const modalEl = document.getElementById('productDetailModal');
  if (modalEl) {
    const modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    modal.show();
  }

  try {
    const response = await fetch(`${API_BASE}/ChiTietSanPham/${productId}`);
    if (!response.ok) {
      throw new Error(`Không lấy được chi tiết sản phẩm (${response.status})`);
    }

    const product = await response.json();
    state.selectedProduct = product;
    renderDetail(product);
  } catch (error) {
    console.error(error);
    elements.detailTitle.textContent = 'Không tải được chi tiết';
    elements.detailState.classList.remove('hidden');
    elements.detailState.innerHTML = '<p>Không thể lấy thông tin chi tiết từ API. Kiểm tra endpoint và thử lại.</p>';
    elements.detailCard.classList.add('hidden');
    state.selectedProduct = null;
  }
}

function renderDetail(product) {
  elements.detailTitle.textContent = product.tenSanPham ?? 'Không có tên';
  elements.detailArt.textContent = getInitial(product.tenSanPham);
  elements.detailCategory.textContent = product.phanLoai ?? 'Chưa phân loại';
  elements.detailCode.textContent = `Mã: ${product.maSpHienThi ?? `SP-${product.sanPhamId}`}`;
  elements.detailPrice.textContent = formatCurrency(product.donGiaBan);
  elements.detailStock.textContent = `Tồn kho: ${product.soLuongTon ?? 0}`;
  elements.detailDescription.textContent = product.moTa || 'Sản phẩm chưa có mô tả chi tiết.';
  elements.detailCard.classList.remove('hidden');
  elements.detailState.classList.add('hidden');
}

function addToCart(product) {
  const index = state.cart.findIndex((item) => item.sanPhamId === product.sanPhamId);

  if (index >= 0) {
    state.cart[index].quantity += 1;
  } else {
    state.cart.push({
      sanPhamId: product.sanPhamId,
      tenSanPham: product.tenSanPham,
      donGiaBan: product.donGiaBan ?? 0,
      quantity: 1,
      phanLoai: product.phanLoai,
    });
  }

  persistCart();
  renderCart();
}

function renderCart() {
  elements.cartCount.textContent = state.cart.reduce((sum, item) => sum + item.quantity, 0);

  const total = state.cart.reduce((sum, item) => sum + (Number(item.donGiaBan) * item.quantity), 0);
  elements.cartTotal.textContent = formatCurrency(total);

  if (!state.cart.length) {
    elements.cartList.innerHTML = '<div class="empty-state text-muted text-center py-3">Giỏ hàng đang trống.</div>';
    return;
  }

  elements.cartList.innerHTML = state.cart
    .map((item) => `
      <div class="cart-item d-flex justify-content-between align-items-center mb-2 border-bottom pb-2">
        <div class="text-start">
          <h6 class="mb-0">${escapeHtml(item.tenSanPham ?? 'Sản phẩm')}</h6>
          <small class="text-muted">${escapeHtml(item.phanLoai ?? 'Chưa phân loại')} · SL: ${item.quantity}</small>
        </div>
        <strong class="text-primary">${formatCurrency(item.donGiaBan * item.quantity)}</strong>
      </div>
    `)
    .join('');
}

function persistCart() {
  localStorage.setItem('bakery-cart', JSON.stringify(state.cart));
}

function loadCart() {
  try {
    const raw = localStorage.getItem('bakery-cart');
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
}

function normalizeText(value) {
  return value
    .toString()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase();
}

function matchCategory(product, keywords) {
  const categoryText = normalizeText(product.phanLoai ?? '');
  return keywords.some((keyword) => categoryText.includes(normalizeText(keyword)));
}

function getInitial(name) {
  const text = (name ?? '?').trim();
  return text ? text[0].toUpperCase() : '?';
}

function formatCurrency(value) {
  const amount = Number(value ?? 0);
  return `${new Intl.NumberFormat('vi-VN').format(amount)} đ`;
}

function setLoading(isLoading, text) {
  elements.loadingState.textContent = text;
  elements.loadingState.style.opacity = isLoading ? '1' : '0.72';
}

function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}