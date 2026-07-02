# Tài liệu ôn tập vấn đáp môn Lập trình mạng - Dự án CaroNet

Tài liệu này chứa danh sách các câu hỏi vấn đáp mẫu chia theo mảng công việc thực hiện của từng thành viên trong nhóm, giúp chuẩn bị cho phần bảo vệ đồ án.

---

## 1. Nguyễn Trần Đình Chương (Leader, UI/Server Security, CI/CD)
*Mảng chính: Kiến trúc hệ thống, tích hợp, quản lý GitFlow/CI/CD, vá lỗi bảo mật (OutOfMemory, Rate Limiting, XSS).*

### Câu 1: Tại sao việc ném `FileNotFoundException` trong static constructor của `AppServices.cs` lại làm sập ứng dụng WinUI ngay lập tức lúc khởi động ở máy client? Em đã sửa lỗi này như thế nào?
* **Trả lời:** Static constructor/field initializers được chạy tự động bởi CLR trước khi lớp được sử dụng lần đầu tiên. Nếu một ngoại lệ không được bắt xảy ra tại đây, CLR sẽ bọc nó trong `TypeInitializationException` và chấm dứt tiến trình ngay lập tức. Ta đã sửa bằng cách chuyển sang fallback trả về đường dẫn database tương đối `"caronet.db"` thay vì ném lỗi trực tiếp, giúp app vẫn khởi động bình thường và chỉ thông báo lỗi khi người dùng bấm vào xem trang lịch sử.

### Câu 2: Tại sao client gửi thông điệp cờ Caro cần có cơ chế khóa `SemaphoreSlim` ở hàm gửi socket?
* **Trả lời:** TCP là giao thức hướng dòng byte (byte-stream oriented) và không có ranh giới tin nhắn. Nếu nhiều luồng đồng thời gọi `SendAsync` ghi dữ liệu lên socket cùng một lúc, các byte của các thông điệp khác nhau sẽ bị xen kẽ (interleaving) dẫn đến sai lệch frame (malformed frame). Sử dụng `_sendLock` đảm bảo mỗi thời điểm chỉ có đúng một frame được gửi toàn vẹn.

### Câu 3: Hãy giải thích cơ chế phòng chống lỗi tràn bộ nhớ (OutOfMemoryException) ở phía Client khi nhận thông điệp TCP?
* **Trả lời:** Trước đây, client đọc 4 byte chỉ độ dài gói tin (`payloadLength`) rồi trực tiếp cấp phát mảng byte: `new byte[payloadLength]`. Kẻ tấn công có thể giả mạo gói tin gửi độ dài cực lớn (ví dụ 2 GB) làm client cạn kiệt bộ nhớ và sập lập tức. Ta đã thêm kiểm tra giới hạn `payloadLength <= 1 MB` (`ProtocolFrameCodec.MaxPayloadLength`) trước khi cấp phát bộ nhớ, nếu quá giới hạn sẽ ném lỗi và đóng kết nối an toàn.

### Câu 4: Tại sao lại cần Rate Limiting ở server và giải quyết bằng cách nào?
* **Trả lời:** Để ngăn chặn các cuộc tấn công từ chối dịch vụ (DoS) hoặc spam tin nhắn từ Client độc hại (ví dụ spam CreateRoom hoặc MakeMove liên tục làm quá tải CPU server). Ta lưu vết thời gian của request cuối cùng cho mỗi Session ID bằng `ConcurrentDictionary`. Nếu khoảng cách giữa hai request nhỏ hơn 100ms (tối đa 10 reqs/s), server sẽ trả về lỗi `Rate limit exceeded` và từ chối xử lý tiếp.

### Câu 5: Sự khác biệt giữa nhánh `develop` và `main` trong GitFlow và tại sao chúng ta cần chạy CI/CD check?
* **Trả lời:** Nhánh `develop` dùng làm nơi tích hợp chính cho mọi tính năng đang phát triển, trong khi `main` chứa bản phát hành ổn định và hoạt động chuẩn để demo. CI/CD chạy các test tự động để đảm bảo các pull request mới không làm hỏng tính năng cũ (lỗi hồi quy/regression) trước khi cho phép merge vào `develop`.

---

## 2. Nguyễn Đức Thành (Protocol & Socket Server Infrastructure)
*Mảng chính: Thiết kế protocol envelope, bộ chuyển đổi framing length-prefix, hạ tầng socket server.*

### Câu 1: Frame codec trong CaroNet hoạt động như thế nào? Tại sao cần cơ chế length-prefix framing trên TCP?
* **Trả lời:** TCP là giao thức truyền dòng byte liên tục, không có ranh giới giữa các tin nhắn. Nếu ta gửi 2 tin nhắn liên tiếp, client có thể đọc được cả 2 cùng lúc (coalescing - gộp gói) hoặc chỉ đọc được một phần tin nhắn (fragmentation - xé gói). Length-prefix bọc thêm 4 byte độ dài Big-Endian ở đầu để bên nhận biết chính xác cần đọc bao nhiêu byte tiếp theo để giải mã thành 1 thông điệp JSON hoàn chỉnh.

### Câu 2: Tại sao lại dùng thứ tự byte Big-Endian (Network Byte Order) để ghi nhận độ dài gói tin?
* **Trả lời:** Big-Endian là chuẩn chung trong lập trình mạng để truyền số nguyên qua network, giúp đảm bảo tính tương thích và độc lập kiến trúc phần cứng giữa các thiết bị gửi và nhận (ví dụ thiết bị Little-Endian như x64 kết nối với Big-Endian).

### Câu 3: Lớp `ClientSession` có vai trò gì và socket listener của server chấp nhận kết nối như thế nào?
* **Trả lời:** `ClientSession` đại diện cho một phiên kết nối của client ở phía server. Nó bọc Socket kết nối, giữ thông tin định danh `SessionId` (Guid) và cung cấp phương thức `SendAsync` an toàn luồng (thread-safe). Socket listener ở Server chạy trong vòng lặp vô hạn gọi `AcceptAsync` bất đồng bộ để nhận kết nối mới và bọc chúng thành các `ClientSession`.

### Câu 4: Cách chuyển đổi giữa số nguyên và byte array trong C# để truyền qua mạng là gì?
* **Trả lời:** Sử dụng lớp `System.Buffers.Binary.BinaryPrimitives`: `ReadInt32BigEndian` để đọc từ byte array thành int, và `WriteInt32BigEndian` để ghi int vào byte array.

### Câu 5: Tại sao TCP socket listener của Server lại cần chạy bất đồng bộ (async/await) thay vì đồng bộ (synchronous blocking)?
* **Trả lời:** Gọi đồng bộ (như `Accept()` hoặc `Receive()`) sẽ chặn hoàn toàn luồng hiện tại của CPU cho đến khi có dữ liệu/kết nối mới. Việc này khiến server không thể quản lý nhiều kết nối đồng thời và cực kỳ tốn tài nguyên. Async/await giải phóng luồng về cho ThreadPool xử lý các công việc khác trong thời gian chờ đợi I/O mạng.

---

## 3. Bao Nguyễn Trường (Storage & Match History UI)
*Mảng chính: SQLite database, thiết kế các Repository (Dapper), UI lịch sử trận đấu.*

### Câu 1: Cơ sở dữ liệu SQLite được thiết lập ở đâu và tại sao lại dùng thư viện Dapper thay vì Entity Framework Core?
* **Trả lời:** SQLite database được lưu ở thư mục app data của user hoặc fallback về thư mục tương đối `"caronet.db"`. Chúng ta dùng Dapper vì đây là Micro-ORM cực kỳ nhẹ, hiệu năng cao, viết truy vấn SQL thuần túy rõ ràng, rất phù hợp cho mục tiêu học tập và quy mô của dự án lập trình mạng sinh viên thay vì EF Core cồng kềnh.

### Câu 2: Làm thế nào để đảm bảo SQLite không bị lỗi tranh chấp "database is locked" khi ghi dữ liệu đồng thời (Concurrency)?
* **Trả lời:** SQLite mặc định có chế độ khóa ghi độc quyền. Khi có nhiều luồng cố gắng ghi đồng thời, ta dễ gặp lỗi busy/locked. Ta khắc phục bằng cách thiết lập tham số `journal_mode=WAL` (Write-Ahead Logging) cho connection string giúp tăng tốc độ đọc ghi đồng thời, kết hợp với cơ chế `BusyTimeout` khoảng 5000ms để kết nối tự đợi ổ đĩa giải phóng thay vì ném lỗi ngay lập tức.

### Câu 3: Cấu trúc bảng lịch sử trận đấu (`MatchHistory`) lưu trữ những trường thông tin gì?
* **Trả lời:** Bảng gồm: `Id` (khóa chính), `Player1Name`, `Player2Name`, `WinnerName`, `WinnerSymbol`, `PlayedAt` (thời gian), và `BoardSnapshot` (chuỗi JSON lưu vết các quân cờ cuối cùng để tái hiện).

### Câu 4: Trong WinUI 3, làm thế nào để hiển thị danh sách lịch sử trận đấu từ cơ sở dữ liệu lên màn hình?
* **Trả lời:** Sử dụng control `ListView` trong XAML, bind thuộc tính `ItemsSource` vào một danh sách `ObservableCollection<MatchHistoryEntry>` trong ViewModel. Khi ViewModel tải dữ liệu từ `IMatchHistoryStore` bất đồng bộ, nó điền vào collection này và UI sẽ tự động cập nhật nhờ cơ chế Data Binding.

### Câu 5: Tại sao phương thức truy cập SQLite nên sử dụng `async/await`?
* **Trả lời:** Đọc/ghi đĩa cứng là một thao tác I/O chậm. Nếu gọi đồng bộ từ UI thread, màn hình game sẽ bị đơ (lag/freeze). Việc dùng `async` (ví dụ `ExecuteAsync` của Dapper) giúp giải phóng UI thread trong lúc chờ hệ điều hành ghi file SQLite xuống đĩa cứng.

---

## 4. Nguyễn Hoàng Phúc (Client Socket Receive Loop, Chat, Settings Persistence)
*Mảng chính: Receive loop socket của Client, chức năng chat room, lưu trữ cấu hình người chơi cục bộ.*

### Câu 1: Vòng lặp nhận dữ liệu (`ReceiveLoop`) của Client hoạt động như thế nào? Tại sao phải chạy trên luồng nền?
* **Trả lời:** Khi kết nối thành công, client khởi động một vòng lặp `while` chạy hoàn toàn trên luồng nền. Nó gọi `ReadFrameAsync` để đọc và phân tách gói tin, giải mã thành `MessageEnvelope` rồi kích hoạt event `MessageReceived` để đẩy dữ liệu lên tầng UI. Nếu chạy trên luồng UI, giao diện WinUI sẽ bị treo cứng ngay lập tức.

### Câu 2: Khi nhận được tin nhắn từ luồng nền, làm thế nào để cập nhật giao diện WinUI mà không bị lỗi chéo luồng (Cross-thread violation)?
* **Trả lời:** WinUI 3 quy định chỉ có luồng UI chính mới được thay đổi các thuộc tính giao diện. Khi luồng nền Socket gọi event cập nhật UI, ViewModel phải bọc mã nguồn cập nhật trong `DispatcherQueue.TryEnqueue` để đưa công việc về chạy trên luồng UI chính.

### Câu 3: Cơ chế Chat hoạt động như thế nào trong kiến trúc Client-Server?
* **Trả lời:** Khi người chơi gõ chat và bấm gửi, client gửi gói tin `Chat` chứa nội dung tin nhắn và `RoomId` lên Server. Server nhận thông điệp, xác định phòng của người chơi, tìm đối thủ cùng phòng rồi forward gói tin đó dưới dạng `ChatReceived` sang client đối thủ qua socket tương ứng.

### Câu 4: Thông tin người chơi (tên, IP, Port gần nhất) được lưu và tải lại như thế nào khi mở app?
* **Trả lời:** Được lưu vào registry của hệ điều hành hoặc file cấu hình cục bộ của app (qua `ApplicationData.Current.LocalSettings` hoặc file json cấu hình). Khi mở app, ViewModel đọc lại các cấu hình này để tự điền vào màn hình đăng nhập nhằm nâng cao trải nghiệm người dùng (UX).

### Câu 5: Điều gì xảy ra với ReceiveLoop khi Socket bị đóng đột ngột từ phía Server?
* **Trả lời:** Phương thức `ReceiveAsync` hoặc `ReadExactlyAsync` của Client Socket sẽ trả về số byte đọc được là `0` hoặc ném ra một ngoại lệ `SocketException`. ReceiveLoop phát hiện trường hợp này, thoát khỏi vòng lặp, gọi hàm dọn dẹp kết nối (`CleanupConnection`) và kích hoạt event `Disconnected` để thông báo cho người dùng trên UI.

---

## 5. Nguyễn Duy Tân (UI MainMenu & Caro Board, Turn Indicator)
*Mảng chính: Giao diện Menu chính và bàn cờ Caro 20x20, hiển thị lượt đi hiện tại.*

### Câu 1: Bàn cờ Caro 20x20 được xây dựng trên UI như thế nào? Làm thế nào để bind trạng thái các ô cờ?
* **Trả lời:** Bàn cờ được vẽ bằng một `GridView` hoặc một Grid chứa 400 ô cờ (`Button`). Trạng thái của bàn cờ là một mảng 2 chiều được quản lý trong ViewModel. Ta dùng kỹ thuật Data Binding để liên kết mỗi ô cờ trên View với trạng thái của ô cờ đó trong ViewModel (X, O, hoặc Trống) để UI tự động vẽ hình ảnh tương ứng.

### Câu 2: Turn Indicator (Chỉ báo lượt đi) hoạt động ra sao để người chơi biết khi nào đến lượt mình đánh?
* **Trả lời:** ViewModel có thuộc tính `IsMyTurn` (boolean) và `TurnMessage` (string). Thuộc tính `IsMyTurn` kiểm tra xem ký tự của người chơi (`MySymbol`) có trùng với lượt đi hiện tại từ server gửi về (`CurrentTurn`) hay không. UI sẽ bind thuộc tính này để vô hiệu hóa/kích hoạt các ô cờ (Enable/Disable) và hiển thị dòng thông báo trạng thái lượt tương ứng.

### Câu 3: Constructor của `GamePage.xaml.cs` cần khởi tạo và gán DataContext như thế nào để tránh màn hình trống khi chuyển trang?
* **Trả lời:** Constructor của Page phải gọi `InitializeComponent()` để dựng UI XAML, đồng thời khởi tạo thực thể `GameViewModel` và gán thuộc tính `DataContext = _viewModel` thì cơ chế Data Binding giữa XAML và ViewModel mới hoạt động.

### Câu 4: Điều gì xảy ra khi người dùng bấm vào một ô cờ trên bàn cờ?
* **Trả lời:** Click event của ô cờ sẽ kích hoạt một Command trong ViewModel. ViewModel kiểm tra nếu `IsMyTurn` là true và ô đó chưa có quân, nó sẽ gửi một request `MakeMove` chứa tọa độ dòng/cột lên Server. ViewModel **không** tự vẽ quân cờ của mình ngay lúc đó để tránh sai lệch trạng thái; nó đợi Server broadcast `GameStateUpdated` về mới hiển thị quân cờ.

### Câu 5: MVVM có ưu điểm gì so với lập trình kéo thả giao diện truyền thống (Code-behind)?
* **Trả lời:** MVVM (Model-View-ViewModel) tách biệt hoàn toàn giao diện (View - XAML) khỏi logic nghiệp vụ và điều khiển (ViewModel) và dữ liệu (Model). Điều này giúp code dễ bảo trì hơn, kiểm thử tự động (Unit Test) được các ViewModel mà không cần chạy giao diện, và dễ dàng thay đổi thiết kế UI mà không phải viết lại code logic.

---

## 6. Trọng Nhân (Caro Rule Engine, Win/Draw Algorithm, GameEnded Dialog)
*Mảng chính: Logic tính toán thắng/thua/hòa của ván đấu Caro, Dialog thông báo kết quả.*

### Câu 1: Thuật toán kiểm tra thắng/thua trong ván cờ Caro 20x20 hoạt động như thế nào?
* **Trả lời:** Sau mỗi nước đi hợp lệ của một người chơi, Rule Engine sẽ duyệt từ vị trí nước đi đó theo 4 hướng: hàng ngang, hàng dọc, đường chéo xuôi và đường chéo ngược. Trên mỗi hướng, ta đếm số quân cờ cùng loại liên tiếp. Nếu có đủ từ 5 quân liên tiếp trở lên, người chơi đó chiến thắng.

### Câu 2: Thuật toán làm sao phát hiện trạng thái Hòa (Draw) trong game Caro?
* **Trả lời:** Ván đấu được tính là Hòa khi tất cả 400 ô cờ trên bàn cờ 20x20 đã được đánh kín dữ liệu nhưng không có bất kỳ bên nào đạt được chuỗi 5 quân liên tiếp để giành chiến thắng.

### Câu 3: Tại sao Rule Engine và các thuật toán kiểm tra thắng thua lại cần được tách riêng ra thư viện `CaroNet.Shared`?
* **Trả lời:** Để cả Client và Server có thể tái sử dụng chung một mã nguồn logic, tránh việc copy-paste code và đảm bảo tính nhất quán (ví dụ Server dùng để thẩm định nước đi chính xác, còn Client có thể dùng để kiểm tra highlight nhanh hoặc viết unit test độc lập).

### Câu 4: Dialog thông báo kết quả Game kết thúc hoạt động như thế nào trong WinUI 3?
* **Trả lời:** Khi nhận được tin nhắn `GameEnded` từ Server, ViewModel sẽ kích hoạt hiển thị Dialog kết quả. Trong WinUI 3, ta sử dụng lớp `ContentDialog`, thiết lập các nút "OK" hoặc "Rematch" và gọi `ShowAsync()` để hiển thị thông báo Winner/Draw trực quan cho người chơi.

### Câu 5: Làm thế nào để viết xUnit test để xác thực thuật toán kiểm tra thắng theo đường chéo của em hoạt động chính xác?
* **Trả lời:** Ta tạo một bàn cờ trống trong bộ nhớ, mô phỏng việc đặt 5 quân cờ cùng loại (ví dụ X) liên tiếp dọc theo một đường chéo (chênh lệch tọa độ dòng và cột bằng nhau) và gọi hàm `CheckWin` của Rule Engine. Assert kết quả trả về phải là người chơi đó thắng cuộc.
