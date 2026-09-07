# BAKERY MANAGEMENT & ANALYTICS SYSTEM
Bakery Management System: 3-Tier Web API with EF Core DB-First & T-SQL


File Script schema và data: run trước khi tiến hành web: [Bakery_Management_.sql](https://github.com/user-attachments/files/28186980/Bakery_Management_.sql)

# 🥐 Bakery Management System (3-Tier Web API & T-SQL)

The Bakery Management System is a comprehensive digital transformation platform designed to automate sales workflows, control inventory fluctuations, and handle custom order processing for modern bakeries[cite: 5].

---

## 🛠️ Tech Stack
* **Back-end:** ASP.NET Core Web API, C#[cite: 5].
* **ORM:** Entity Framework Core (Database First approach)[cite: 5].
* **Database:** Microsoft SQL Server, Advanced T-SQL (Stored Procedures, Triggers, Views, Functions, Transactions, Indexes)[cite: 5].
* **Front-end & UI:** Bootstrap 5, JavaScript (Fetch API), HTML5[cite: 5].
* **Software Architecture:** 3-Layer Architecture (Presentation Layer, Business Logic Layer, Data Access Layer)[cite: 5].

---

## 🏛️ System Structure & Architecture
The system is decomposed into 3 independent layers to ensure security, maintainability, and scalability:
1. **Presentation Layer:** Provides an intuitive management interface (Dashboard, staff, customer, product, promotion, and order management) and API documentation via Swagger[cite: 5].
2. **Business Logic Layer (BLL):** Contains core business rules such as discount calculation, dynamic pricing algorithms for custom cakes, and real-time inventory checking[cite: 5].
3. **Data Access Layer (DAL):** The layer responsible for direct database communication via EF Core, ensuring data safety through Database Transactions[cite: 5].

---

## 💡 Database & T-SQL Highlights
* **Optimized Views:** Utilizes `V_ThongKeSanPham` to track live inventory and product revenue, combined with `V_TraCuuDonHang` to flatten invoice data[cite: 5].
* **Stored Procedures & JSON:** Built `SP_TaoDonHangMoi` to support parsing complex cart arrays passed from the Client as JSON strings within a single Transaction[cite: 5].
* **Data Defense Triggers:** 
  * `TRG_NganXoaSanPham`: Blocks physical deletion of products that have historical transaction records[cite: 5].
  * `TRG_TruTonKhoKhiHoanTat`: Automatically deducts actual inventory when an order transitions to the "Completed" status[cite: 5].
* **Performance Optimization:** Implements Non-Clustered Indexes to speed up customer phone number lookups and order status filtering[cite: 5].

---

## ⚙️ Key Business Modules & APIs

### 1. Custom Orders API Module
* **Dynamic Pricing Algorithm (`DonBanhCustomService`):** The system automatically analyzes size specifications and scans for artistic complexity keywords (e.g., "2 tiers", "drawing") to apply flexible surcharges in real-time[cite: 5].
* **Specification Management Endpoints:** Provides API endpoints to initialize and update custom cake design requirements, tightly linked 1-1 with invoices via a unique foreign key[cite: 5].

### 2. Invoicing & XML Data Module (XML Invoicing)
* **Raw XML Data Structuring:** Employs LINQ to XML techniques via `XDocument` and `XElement` objects to automatically structure electronic invoice data[cite: 5].
* **Printing & Export Support:** Structured invoice data is exported directly into professional thermal printing forms and archive templates[cite: 5].

### 3. Other Core Business Modules
* **Order & Inventory Management:** Automatic inventory checking prior to order confirmation to prevent over-selling[cite: 5].
* **Promotion & Customer Management:** Applies real-time discount codes (`FN_TinhTienGiam`), controls purchase history, and blocks the deletion of customers with existing transactions[cite: 5].

---

## 🚀 Technical Challenges Overcome
* Completely resolved Namespace conflicts causing Swagger 500 errors[cite: 5].
* Successfully synchronized data mapping between C#'s `PascalCase` and JavaScript's `camelCase`[cite: 5].
* Resolved EF Core tracking issues when updating multi-table shopping carts using the `BeginTransactionAsync` mechanism[cite: 5].

---

## 👥 Contributors
* **Võ Hoàng Thịnh** (Team Lead)[cite: 5]
* **Đinh Thị Minh Châu** (Member)[cite: 5]
* **Nguyễn Thị Thanh Ngân** (Member)[cite: 5]

Tài liệu tham khảo:

[1] Steve (Ardalis) Smith, Architecting Modern Web Applications with ASP.NET Core
and Azure, Microsoft Corporation, 2023.

[2] Andrew Troelsen, Philip Japikse, Pro C# 10 with .NET 6: Foundational Principles
and Practices in Programming, Apress, 2022.

[3] Microsoft Documentation, Entity Framework Core Overview, Microsoft Learn,
2024.https://learn.microsoft.com/en-us/ef/core/

[4] ThemeSelection, Sneat - Free Bootstrap 5 HTML Admin Template, 2024.
https://themeselection.com/item/sneat-free-bootstrap-html-admin-template/

[5] Mark Otto, Jacob Thornton, and Bootstrap contributors, Bootstrap 5.0 Documenta-
tion, https://getbootstrap.com/docs/5.0/

[6] Mozilla Developer Network (MDN), JavaScript Guide & Fetch API, 2024. 
https://developer.mozilla.org/en-US/docs/Web/JavaScript

[7] Microsoft Documentation, SQL Server Technical Documentation, Microsoft Learn,
2024.https://learn.microsoft.com/en-us/sql/sql-server/
   
