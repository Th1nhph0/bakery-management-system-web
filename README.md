# bakery-management-system-web
Bakery management system (ASP.NET Core Web API + SQL Server, 3-tier architecture)
Trình tự làm bài theo quy trình: 

1. Tải bài trên github về máy, và về thư mục mong muốn
   Mở Command Prompt (CMD) hoặc Terminal trên máy tính
   Dán cái này vào cd /d D:\Thư mục trong ổ D muốn để bài vào ( vd cd /d D:\LTCSDL)
   
   Dán này vào
   git clone https://github.com/Th1nhph0/bakery-management-system-web
   <img width="773" height="138" alt="image" src="https://github.com/user-attachments/assets/52f45cdb-2bb7-45aa-97e5-b662e060e131" />
   Như vậy là xong, sau đó vào file và mở file <img width="121" height="34" alt="image" src="https://github.com/user-attachments/assets/98542bf4-eb34-4cce-85eb-e9315598e324" />
   Sau đó phải tham chiếu các file như sau để tránh bị lỗi <img width="682" height="144" alt="image" src="https://github.com/user-attachments/assets/370192a3-67a0-48d8-8054-4fa565cd4c16" />

   
3. https://drive.google.com/drive/folders/1YI4FS6-1Uhkf-AGb-F_LUn8wcb2wpNEu?usp=drive_link
   Vào link này để tải cả 3 file sql về, sau đó kết nối với server của máy, sau đó excute theo thứ tự file, nhưng mà file 03_Test thì chỉ dùng để test những câu lệnh đã tạo thoi nha, k cần excute cũng đc
   Sau khi chạy thành công tất cả, ghi nhớ cái tên này:
   <img width="522" height="111" alt="image" src="https://github.com/user-attachments/assets/854ee0dc-b650-487f-8b1e-927f5446e0f7" />
   Vì đây là cái tên sẽ kết nối với SQL với API
4. Sau khi chạy file này xong <img width="121" height="34" alt="image" src="https://github.com/user-attachments/assets/c7b1cd0d-751e-4541-a6a7-7b23a81e2a52" />
   Tools -> Nuget Package ... -> Console <img width="1119" height="616" alt="image" src="https://github.com/user-attachments/assets/81f9f640-d9ae-45b6-8e07-65ad33020b7e" />
   Sau đó cứ luân phiên dán vào sau PM< và ENTER
   Install-Package Microsoft.EntityFrameworkCore.SqlServer
   Install-Package Microsoft.EntityFrameworkCore.Tools
   Install-Package Microsoft.Data.SqlClient
   Install-Package Microsoft.EntityFrameworkCore
   Install-Package Swashbuckle.AspNetCore
   Install-Package Microsoft.EntityFrameworkCore.Design
   Đã hoàn thành việc tạo các này kia kia nọ hihi
   Tiếp theo là kết nối với server SQL đã tạo trên máy, giữ nguyên cái console nãy giờ để tải á, dán cái này vào
   {
  "ConnectionStrings": {
    "DefaultConnection": "Server=TÊN_SERVER_CỦA_BẠN;Database=BakeryManagement;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": { ... }
}  Thay TÊN_SERVER_CỦA_BẠN bằng cái hồi nãy em nói trong phần 2. á xong rồi ENTER, nếu thấy lỗi time_out gì đó hãy nhớ tắt app SSMS mà chạy script nãy đi, xong ròi chạy lại, nếu như gặp thêm lỗi thì sửa TÊN_SERVER_CỦA_BẠN thành . 
   Sau khi thấy chạy thành công á, mình kiểm tra bên mục <img width="369" height="387" alt="image" src="https://github.com/user-attachments/assets/cf0c88c8-bc24-422c-9705-6457412422bf" /> có đủ như này chưa, đủ là thành công ròi á

5. Run Project (F5)
   Run thành công thì sẽ hiện ra như sau
   <img width="1201" height="614" alt="image" src="https://github.com/user-attachments/assets/f8ac3d6a-429d-4ef0-83d7-22a81831bb64" />
   TRong đó dòng link đầu tiên chính là nơi để mình test các api nãy giờ mình tạo ra, khi sao chép vào dán vào trình duyệt thì nhớ add thêm /swagger vào thì mới đc (vd https://localhost:7122/swagger/index.html)
   Sau đó để test thì cứ sổ các mục xuống, <img width="1149" height="364" alt="image" src="https://github.com/user-attachments/assets/c1892f31-bc44-4601-8ae0-a60de42f85df" /> bấm try it out
   Sau đó nó sẽ cho mình quyền điền thông tin vào cái này, theo ngôn ngữ JSON để có thể thực hiện Service mà mình cài đặt á, Service chính là những câu lệnh mình thiết kế trong SQL trước ròi, sau đó tới API mình mượn nó và cải tiến thêm để có thể tiện cho việc gắn lên FrontEnd
   <img width="1082" height="316" alt="image" src="https://github.com/user-attachments/assets/9eacccac-5205-46c2-8076-af2542e2d703" />
   trong đó, cứ số 0 thì mình điền số vào, string thì mình điền chuỗi, xong thì mình Exucte, cứ như là chạy mấy cái câu lệnh bên SQL á, nếu như k biết nhập gì thì lên cái Test SQL để biết hàm đó cần nhập gì nhaa









