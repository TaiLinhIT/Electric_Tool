CREATE OR ALTER PROCEDURE calculate_monthly_device_ratio (@Thang INT, @Nam INT)
AS
BEGIN
    -- 1. Xác định ngày bắt đầu và ngày kết thúc của tháng đã nhập
    -- DATEFROMPARTS(year, month, day) - Lấy ngày đầu tháng
    DECLARE @NgayBatDau DATE = DATEFROMPARTS(@Nam, @Thang, 1);
    -- EOMONTH() - Lấy ngày cuối tháng
    DECLARE @NgayKetThuc DATE = EOMONTH(@NgayBatDau);

    -- 2. Tính Mức tiêu thụ tuyệt đối (Max - Min của Imp + Exp) cho mỗi device
    WITH DeviceConsumption AS (
        SELECT
            A.devid,
            -- Tính mức tăng Imp
            (MAX(CASE WHEN B.Name = 'Imp' THEN A.Value END) - 
             MIN(CASE WHEN B.Name = 'Imp' THEN A.Value END)) AS MucTangImp,
            -- Tính mức tăng Exp
            (MAX(CASE WHEN B.Name = 'Exp' THEN A.Value END) - 
             MIN(CASE WHEN B.Name = 'Exp' THEN A.Value END)) AS MucTangExp
        FROM
            SensorData AS A
        INNER JOIN
            ControlCode AS B ON A.CodeID = B.CodeID
        WHERE
            -- Lọc theo tháng và năm cụ thể
            A.day >= @NgayBatDau 
            AND A.day <= @NgayKetThuc
            AND B.Name IN ('Imp', 'Exp')
        GROUP BY
            A.devid
    ),
    -- 3. Tính Tổng mức tiêu thụ toàn bộ và Mức tiêu thụ cuối cùng của từng device
    FinalCalculation AS (
        SELECT
            devid,
            -- Tính tổng mức tiêu thụ/sản xuất của device đó
            (MucTangImp + MucTangExp) AS TongMucTieuThu,
            -- Dùng Window Function để tính tổng tiêu thụ của TẤT CẢ các device trong tháng
            SUM(MucTangImp + MucTangExp) OVER () AS TongTieuThuTatCa
        FROM
            DeviceConsumption
        WHERE
            -- Đảm bảo chỉ tính các thiết bị có dữ liệu hợp lệ (không phải NULL nếu có lỗi)
            (MucTangImp + MucTangExp) IS NOT NULL
            AND (MucTangImp + MucTangExp) > 0 -- Loại bỏ thiết bị không có thay đổi
    )
    -- 4. Tính tỷ lệ phần trăm và trả về kết quả
    SELECT
        devid AS MaThietBi,
        TongMucTieuThu,
        -- Tính tỷ lệ phần trăm (nhân 100 và làm tròn 2 chữ số thập phân)
        ROUND((TongMucTieuThu * 100.0) / TongTieuThuTatCa, 2) AS TiLePhanTram
    FROM
        FinalCalculation
    ORDER BY
        TiLePhanTram DESC;
END;