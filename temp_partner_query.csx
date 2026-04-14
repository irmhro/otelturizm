using System;
using MySqlConnector;
var cs = "Server=127.0.0.1;Port=3306;Database=otelturizmnew;User ID=root;Password=;CharSet=utf8mb4;";
await using var conn = new MySqlConnection(cs);
await conn.OpenAsync();
var sql = @"
SELECT u.id, u.eposta, u.rol, oks.otel_id, o.otel_adi
FROM users u
LEFT JOIN otel_kullanici_sahiplikleri oks ON oks.user_id = u.id AND oks.aktif_mi = 1
LEFT JOIN oteller o ON o.id = oks.otel_id
WHERE u.eposta IN ('216silvertuzla@gmail.com','216castlehotel@gmail.com','216bosphorus@gmail.com')
ORDER BY u.eposta, oks.otel_id;";
await using var cmd = new MySqlCommand(sql, conn);
await using var r = await cmd.ExecuteReaderAsync();
while (await r.ReadAsync())
{
    Console.WriteLine($"{r.GetInt64(0)}|{r.GetString(1)}|{r.GetString(2)}|{(r.IsDBNull(3)?"null":r.GetInt64(3).ToString())}|{(r.IsDBNull(4)?"null":r.GetString(4))}");
}
