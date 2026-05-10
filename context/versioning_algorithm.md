# Thuật toán lưu file bằng Diff + Redis + Expiration + Safe Cleanup (Final)

## 1. Mục tiêu

* Diff cho text-based
* Full cho binary
* Có expiration để dọn rác
* Tránh xoá nhầm version đang dùng
* Đảm bảo an toàn khi concurrent

---

## 2. Phân loại file

### Diff

* .txt, .md, .json, .xml

### Full

* .docx, .pptx, .xlsx, .pdf, image, video

---

## 3. Rule version

* version_number tăng dần
* nếu version_number % 5 == 0 → full
* còn lại → diff

---

## 4. Redis keys

```text
file:{file_id}:latest
file:{file_id}:latest_number
file:{file_id}:lock
file:{file_id}:reconstruct_cache
file:{file_id}:active_ref
```

---

## 5. Flow lưu version

### Validate

* file hợp lệ
* user thuộc workspace
* check conflict

```text
latest_version_id != base_version_id → reject
```

---

### Lock

```text
SETNX file:{file_id}:lock user_id EX 30s
```

---

### Decide

```text
if not text_based → full
else if version_number % 5 == 0 → full
else → diff
```

---

### Sau khi lưu

* update Redis latest
* clear cache
* release lock

---

## 6. Expiration

### DB thêm

```sql
ALTER TABLE versions
ADD COLUMN expired_at TIMESTAMP NULL;
```

---

### Rule giữ version

* tìm full gần nhất: `full_base`
* giữ:

  * full_base
  * tất cả version > full_base
* expire:

  * tất cả version < full_base

---

## 7. Safe Cleanup Design (tránh xoá nhầm)

## 7.1 Nguyên tắc

Không xoá nếu:

* đang được download
* đang được restore
* đang là base của version khác
* đang là latest

---

## 7.2 Active reference tracking (Redis)

Khi sử dụng version:

### Download / Restore:

```text
INCR file:{file_id}:active_ref:{version_id}
EXPIRE 5m
```

Khi xong:

```text
DECR file:{file_id}:active_ref:{version_id}
```

---

## 7.3 Check trước khi xoá

Pseudo:

```csharp
bool CanDelete(version)
{
    if (version.IsLatest)
        return false;

    if (HasDependentVersion(version.Id))
        return false;

    var active = Redis.Get($"file:{fileId}:active_ref:{version.Id}");
    if (active > 0)
        return false;

    return true;
}
```

---

## 7.4 Cleanup Worker

```csharp
foreach (var v in expiredVersions)
{
    if (!CanDelete(v))
        continue;

    try
    {
        File.Delete(v.StoragePath);
        repo.Delete(v.Id);
    }
    catch
    {
        // retry lần sau
    }
}
```

---

## 7.5 Delay xoá

* expired_at + delay (ví dụ 1h)
* tránh race condition

---

## 7.6 Dependency check

```sql
SELECT 1
FROM versions
WHERE base_version_id = @versionId
LIMIT 1;
```

→ nếu tồn tại → không xoá

---

## 8. Redis cleanup

Khi xoá:

```text
DEL file:{file_id}:reconstruct_cache
DEL file:{file_id}:active_ref:{version_id}
```

---

## 9. Custom Validation bổ sung

* không thao tác trên version expired
* không restore từ version expired
* không download version expired

---

## 10. Edge cases

### Worker crash giữa chừng

* file xoá rồi nhưng DB chưa xoá
  → check file tồn tại trước khi xoá

---

### Redis mất dữ liệu

* fallback DB
* active_ref mất → chấp nhận (best effort)

---

### Concurrent upload + cleanup

* lock upload
* cleanup skip version đang active

---

## 11. Trade-off

* thêm Redis complexity
* không đảm bảo 100% nếu Redis mất data
* nhưng đủ an toàn cho production nhẹ

---

## 12. Kết luận

* expiration giúp giảm storage
* safe cleanup tránh mất dữ liệu
* Redis giúp tracking realtime
* thiết kế cân bằng giữa đơn giản và an toàn

---
