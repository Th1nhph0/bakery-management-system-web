(function ($) {
    "use strict";

    // Spinner
    var spinner = function () {
        setTimeout(function () {
            if ($('#spinner').length > 0) {
                $('#spinner').removeClass('show');
            }
        }, 1);
    };
    spinner();
    
    
    // Initiate the wowjs
    new WOW().init();


    // Fixed Navbar
    $('.fixed-top').css('top', $('.top-bar').height());
    $(window).scroll(function () {
        if ($(this).scrollTop()) {
            $('.fixed-top').addClass('bg-dark').css('top', 0);
        } else {
            $('.fixed-top').removeClass('bg-dark').css('top', $('.top-bar').height());
        }
    });
    
    
    // Back to top button
    $(window).scroll(function () {
        if ($(this).scrollTop() > 300) {
            $('.back-to-top').fadeIn('slow');
        } else {
            $('.back-to-top').fadeOut('slow');
        }
    });
    $('.back-to-top').click(function () {
        $('html, body').animate({scrollTop: 0}, 1500, 'easeInOutExpo');
        return false;
    });


    // Header carousel
    $(".header-carousel").owlCarousel({
        autoplay: false,
        smartSpeed: 1500,
        loop: true,
        nav: true,
        dots: false,
        items: 1,
        navText : [
            '<i class="bi bi-chevron-left"></i>',
            '<i class="bi bi-chevron-right"></i>'
        ]
    });


    // Facts counter
    $('[data-toggle="counter-up"]').counterUp({
        delay: 10,
        time: 2000
    });


    // Testimonials carousel
    $(".testimonial-carousel").owlCarousel({
        autoplay: false,
        smartSpeed: 1000,
        margin: 25,
        loop: true,
        center: true,
        dots: false,
        nav: true,
        navText : [
            '<i class="bi bi-chevron-left"></i>',
            '<i class="bi bi-chevron-right"></i>'
        ],
        responsive: {
            0:{
                items:1
            },
            768:{
                items:2
            },
            992:{
                items:3
            }
        }
    });

    // Cart
    var CART_KEY = 'cart';

    initCartUi();
    renderCart();
    prepareReportCartState();

    function initCartUi() {
        if (!$('.cart-open-btn').length) {
            const cartButton = `
                <button type="button" class="btn btn-primary rounded-pill px-3 py-2 me-lg-3 mb-3 mb-lg-0 position-relative cart-open-btn" data-bs-toggle="modal" data-bs-target="#cartModal">
                    <i class="fa fa-shopping-cart me-2"></i>Giỏ Hàng
                    <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger cart-count">0</span>
                </button>
            `;
            $('.navbar-collapse > .d-none.d-lg-flex, .navbar-collapse > .d-lg-flex').first().before(cartButton);
        }

        if (!$('#cartModal').length) {
            $('body').append(`
                <div class="modal fade" id="cartModal" tabindex="-1" aria-labelledby="cartModalTitle" aria-hidden="true">
                    <div class="modal-dialog modal-lg modal-dialog-scrollable">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title" id="cartModalTitle">Giỏ Hàng</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body">
                                <div id="cartItems"></div>
                            </div>
                            <div class="modal-footer d-flex justify-content-between flex-wrap gap-3">
                                <button type="button" class="btn btn-outline-danger" id="clearCartBtn">
                                    <i class="fa fa-trash me-2"></i>Xóa Giỏ Hàng
                                </button>
                                <div class="ms-auto text-end">
                                    <div class="text-muted small">Tổng tiền</div>
                                    <strong class="fs-5" id="cartTotal">0 VNĐ</strong>
                                </div>
                                <button type="button" class="btn btn-primary" data-bs-dismiss="modal">Tiếp Tục Mua</button>
                            </div>
                        </div>
                    </div>
                </div>
            `);
        }

        if (!$('#cartSuccessModal').length) {
            $('body').append(`
                <div class="modal fade" id="cartSuccessModal" tabindex="-1" aria-labelledby="cartSuccessModalTitle" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-centered">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title" id="cartSuccessModalTitle">Thêm Vào Giỏ Hàng Thành Công</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body text-center py-4">
                                <div class="btn-lg-square bg-primary rounded-circle mx-auto mb-3">
                                    <i class="fa fa-check text-white"></i>
                                </div>
                                <h5 class="mb-2" id="cartSuccessProductName">Sản phẩm</h5>
                                <p class="text-muted mb-0">Sản phẩm đã được thêm vào giỏ hàng.</p>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Tiếp Tục Mua</button>
                                <button type="button" class="btn btn-primary" id="viewCartFromSuccessBtn">
                                    <i class="fa fa-shopping-cart me-2"></i>Xem Giỏ Hàng
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `);
        }
    }

    function getCart() {
        try {
            const cart = JSON.parse(localStorage.getItem(CART_KEY)) || [];
            return cart.map(normalizeCartItem).filter(item => item.productId);
        } catch {
            return [];
        }
    }

    function saveCart(cart) {
        localStorage.setItem(CART_KEY, JSON.stringify(cart.map(normalizeCartItem)));
        renderCart();
    }

    function normalizeCartItem(item) {
        return {
            productId: String(item.productId ?? item.sanPhamId ?? ''),
            productName: item.productName ?? item.tenSanPham ?? 'Sản phẩm',
            productPrice: Number(item.productPrice ?? item.donGiaBan ?? 0),
            category: item.category ?? item.phanLoai ?? '',
            quantity: Math.max(1, Number(item.quantity ?? 1))
        };
    }

    function addToCart(item) {
        const cart = getCart();
        const normalized = normalizeCartItem(item);
        const existingItem = cart.find(cartItem => cartItem.productId === normalized.productId);

        if (existingItem) {
            existingItem.quantity += 1;
        } else {
            cart.push(normalized);
        }

        saveCart(cart);
    }

    function showCartModal() {
        const cartModalElement = document.getElementById('cartModal');
        if (!cartModalElement) return;

        renderCart();
        const cartModal = bootstrap.Modal.getInstance(cartModalElement) || new bootstrap.Modal(cartModalElement);
        cartModal.show();
    }

    function showCartSuccessModal(productName) {
        const successModalElement = document.getElementById('cartSuccessModal');
        if (!successModalElement) return;

        $('#cartSuccessProductName').text(productName || 'Sản phẩm');
        const successModal = bootstrap.Modal.getInstance(successModalElement) || new bootstrap.Modal(successModalElement);
        successModal.show();
    }

    function prepareReportCartState() {
        const params = new URLSearchParams(window.location.search);
        if (params.get('reportCart') !== '1') return;

        saveCart([
            {
                productId: '1',
                productName: 'Bánh Tiramisu Ý (1 cái)',
                productPrice: 55000,
                category: 'Bánh ngọt',
                quantity: 2
            },
            {
                productId: '18',
                productName: 'Bơ lạt Anchor New Zealand (1kg)',
                productPrice: 195000,
                category: 'Nguyên liệu',
                quantity: 1
            }
        ]);

        setTimeout(showCartModal, 800);
    }

    function renderCart() {
        const cart = getCart();
        const totalQuantity = cart.reduce((sum, item) => sum + item.quantity, 0);
        const totalPrice = cart.reduce((sum, item) => sum + (item.productPrice * item.quantity), 0);

        $('.cart-count').text(totalQuantity);
        $('#cartTotal').text(formatCurrency(totalPrice));

        if (!$('#cartItems').length) return;

        if (!cart.length) {
            $('#cartItems').html('<div class="text-center text-muted py-4">Giỏ hàng đang trống.</div>');
            $('#clearCartBtn').prop('disabled', true);
            return;
        }

        $('#clearCartBtn').prop('disabled', false);
        $('#cartItems').html(cart.map(item => {
            const subtotal = item.productPrice * item.quantity;
            return `
                <div class="border rounded p-3 mb-3">
                    <div class="d-flex justify-content-between gap-3">
                        <div>
                            <h6 class="mb-1">${escapeHtml(item.productName)}</h6>
                            <div class="text-muted small">${escapeHtml(item.category || 'Chưa phân loại')}</div>
                        </div>
                        <button type="button" class="btn btn-sm btn-outline-danger cart-remove-btn" data-id="${escapeHtml(item.productId)}" aria-label="Xóa sản phẩm">
                            <i class="fa fa-trash"></i>
                        </button>
                    </div>
                    <div class="d-flex align-items-center justify-content-between flex-wrap gap-3 mt-3">
                        <div class="btn-group" role="group" aria-label="Số lượng">
                            <button type="button" class="btn btn-outline-secondary btn-sm cart-minus-btn" data-id="${escapeHtml(item.productId)}">-</button>
                            <span class="btn btn-light btn-sm disabled">${item.quantity}</span>
                            <button type="button" class="btn btn-outline-secondary btn-sm cart-plus-btn" data-id="${escapeHtml(item.productId)}">+</button>
                        </div>
                        <div class="text-end">
                            <div class="text-muted small">${formatCurrency(item.productPrice)} / sản phẩm</div>
                            <strong>${formatCurrency(subtotal)}</strong>
                        </div>
                    </div>
                </div>
            `;
        }).join(''));
    }

    $(document).on('click', '.cart-open-btn', function() {
        renderCart();
    });

    $(document).on('click', '#viewCartFromSuccessBtn', function() {
        const successModalElement = document.getElementById('cartSuccessModal');
        const successModal = successModalElement ? bootstrap.Modal.getInstance(successModalElement) : null;

        if (successModalElement && successModal) {
            $(successModalElement).one('hidden.bs.modal', showCartModal);
            successModal.hide();
        } else {
            showCartModal();
        }
    });

    $(document).on('click', '.cart-plus-btn', function() {
        const productId = String($(this).data('id'));
        const cart = getCart();
        const item = cart.find(cartItem => cartItem.productId === productId);
        if (item) item.quantity += 1;
        saveCart(cart);
    });

    $(document).on('click', '.cart-minus-btn', function() {
        const productId = String($(this).data('id'));
        const cart = getCart();
        const item = cart.find(cartItem => cartItem.productId === productId);
        if (!item) return;

        item.quantity -= 1;
        saveCart(cart.filter(cartItem => cartItem.quantity > 0));
    });

    $(document).on('click', '.cart-remove-btn', function() {
        const productId = String($(this).data('id'));
        saveCart(getCart().filter(item => item.productId !== productId));
    });

    $(document).on('click', '#clearCartBtn', function() {
        saveCart([]);
    });


    // Product Management
    var allProducts = [];
    var selectedCategory = ($('#productList').data('default-category') || '').toString();

    function loadProducts() {
        if (!$('#productList').length) return;

        fetch('/api/sanpham/HienThiDanhSachSanPham')
            .then(response => {
                if (!response.ok) {
                    throw new Error('Không tải được danh sách sản phẩm từ API.');
                }

                return response.json();
            })
            .then(data => {
                allProducts = Array.isArray(data) ? data : [];
                filterProducts();
            })
            .catch(error => {
                console.error('Error loading products:', error);
                $('#productList').html(`<div class="col-12 text-center text-danger"><p>${escapeHtml(error.message)}</p></div>`);
            });
    }

    function displayProducts(products) {
        const productList = $('#productList');
        if (!productList.length) return;

        productList.empty();

        if (products.length === 0) {
            productList.html('<div class="col-12 text-center"><p>Không tìm thấy sản phẩm</p></div>');
            return;
        }

        products.forEach(product => {
            const stock = product.soLuongTon || 0;
            const productHtml = `
                <div class="col-lg-4 col-md-6 wow fadeInUp">
                    <div class="product-item d-flex flex-column bg-white rounded overflow-hidden h-100">
                        <div class="text-center p-4">
                            <div class="d-inline-block border border-primary rounded-pill px-3 mb-3">${Number(product.donGiaBan || 0).toLocaleString('vi-VN')} VNĐ</div>
                            <h5 class="mb-3">${product.tenSanPham}</h5>
                            <span class="small">${product.phanLoai || 'Khác'}</span>
                        </div>
                        <div class="position-relative mt-auto">
                            <div class="bg-light p-4 text-center">
                                <p class="mb-2">Kho: ${stock}</p>
                            </div>
                            <div class="product-overlay">
                                <a class="btn btn-lg-square btn-outline-light rounded-circle product-detail-btn" href="#" data-id="${product.sanPhamId}"><i class="fa fa-eye text-primary"></i></a>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            productList.append(productHtml);
        });

        $('.product-detail-btn').click(function(e) {
            e.preventDefault();
            const productId = $(this).data('id');
            showProductDetail(productId);
        });
    }

    async function showProductDetail(productId) {
        const modalElement = document.getElementById('productDetailModal');
        if (!modalElement) return;

        $('#productDetailTitle').text('Đang Tải Chi Tiết');
        $('#productDetailName').text('Đang tải sản phẩm...');
        $('#productDetailDescription').text('Đang lấy dữ liệu chi tiết từ API.');
        $('#productDetailPrice').text('Giá: --');
        $('#productDetailCategory').text('--');
        $('#productDetailStock').text('--');
        $('#productDetailImage').attr('src', 'img/product-1.jpg');
        $('#addToCartBtn').prop('disabled', true).removeData('productId');

        const modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        modal.show();

        try {
            const response = await fetch(`/api/sanpham/ChiTietSanPham/${encodeURIComponent(productId)}`);
            const result = await readApiResponse(response);

            if (!response.ok) {
                throw new Error(getApiMessage(result) || 'Không lấy được chi tiết sản phẩm.');
            }

            renderProductDetail(result);
        } catch (error) {
            const message = getFetchErrorMessage(error, 'Không lấy được chi tiết sản phẩm. Vui lòng kiểm tra API và thử lại.');

            $('#productDetailTitle').text('Lỗi Chi Tiết Sản Phẩm');
            $('#productDetailName').text('Không thể hiển thị sản phẩm');
            $('#productDetailDescription').text(message);
            $('#productDetailPrice').text('Giá: --');
            $('#productDetailCategory').text('--');
            $('#productDetailStock').text('--');
            $('#addToCartBtn').prop('disabled', true).removeData('productId');
        }
    }

    function renderProductDetail(product) {
        const stock = product.soLuongTon || 0;

        $('#productDetailTitle').text('Chi Tiết Sản Phẩm');
        $('#productDetailName').text(product.tenSanPham || 'Sản phẩm');
        $('#productDetailDescription').text(product.moTa || 'Không có mô tả');
        $('#productDetailPrice').text('Giá: ' + Number(product.donGiaBan || 0).toLocaleString('vi-VN') + ' VNĐ');
        $('#productDetailCategory').text(product.phanLoai || 'Khác');
        $('#productDetailStock').text(stock);
        $('#productDetailImage').attr('src', 'img/product-1.jpg');

        $('#addToCartBtn').prop('disabled', false);
        $('#addToCartBtn').data('productId', product.sanPhamId);
        $('#addToCartBtn').data('productName', product.tenSanPham);
        $('#addToCartBtn').data('productPrice', product.donGiaBan);
        $('#addToCartBtn').data('productCategory', product.phanLoai || 'Khác');
    }

    $('#addToCartBtn').click(function() {
        const productId = $(this).data('productId');
        const productName = $(this).data('productName');
        const productPrice = $(this).data('productPrice');
        const productCategory = $(this).data('productCategory');

        if (!productId) return;

        addToCart({
            productId: productId,
            productName: productName,
            productPrice: productPrice,
            category: productCategory,
            quantity: 1
        });
        
        const detailModalElement = document.getElementById('productDetailModal');
        const detailModal = detailModalElement ? bootstrap.Modal.getInstance(detailModalElement) : null;

        if (detailModalElement && detailModal) {
            $(detailModalElement).one('hidden.bs.modal', function() {
                showCartSuccessModal(productName);
            });
            detailModal.hide();
        } else {
            showCartSuccessModal(productName);
        }
    });

    $('#searchBtn').click(function() {
        filterProducts();
    });

    $('#searchInput').keypress(function(e) {
        if (e.which === 13) {
            filterProducts();
            return false;
        }
    });

    $('.category-btn').click(function() {
        $('.category-btn').removeClass('active').addClass('btn-outline-primary').removeClass('btn-primary');
        $(this).addClass('active').removeClass('btn-outline-primary').addClass('btn-primary');
        selectedCategory = ($(this).data('category') || '').toString();
        filterProducts();
    });

    function filterProducts() {
        const searchTerm = normalizeText($('#searchInput').val() || '');
        
        const filtered = allProducts.filter(product => {
            const productText = normalizeText(`${product.tenSanPham || ''} ${product.moTa || ''} ${product.phanLoai || ''}`);
            const matchesSearch = !searchTerm || productText.includes(searchTerm);
            const matchesCategory = !selectedCategory || categoryMatches(product.phanLoai, selectedCategory);
            
            return matchesSearch && matchesCategory;
        });

        displayProducts(filtered);
    }

    function categoryMatches(productCategory, category) {
        const productValue = normalizeText(productCategory || '');
        const categoryValue = normalizeText(category || '');

        return productValue === categoryValue || productValue.includes(categoryValue);
    }

    function normalizeText(value) {
        return value
            .toString()
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase()
            .trim();
    }

    function formatCurrency(value) {
        return `${Number(value || 0).toLocaleString('vi-VN')} VNĐ`;
    }

    async function readApiResponse(response) {
        const text = await response.text();
        if (!text) return null;

        try {
            return JSON.parse(text);
        } catch {
            return { message: text };
        }
    }

    function getApiMessage(result) {
        return result?.message || result?.Message || result?.title || '';
    }

    function getFetchErrorMessage(error, fallbackMessage) {
        if (error instanceof TypeError && error.message === 'Failed to fetch') {
            return 'Không kết nối được API. Hãy chạy project bằng http://localhost:5094 rồi thử lại.';
        }

        return error.message || fallbackMessage;
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    $(document).ready(function() {
        loadProducts();
    });
    
})(jQuery);
