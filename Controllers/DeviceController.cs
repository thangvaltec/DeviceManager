using System;
using System.Linq;
using DeviceApi.Data;
using DeviceApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeviceApi.Controllers
{
    public class UpdateDeviceRequest
    {
        public string SerialNo { get; set; } = string.Empty;
        public int AuthMode { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly TenantDbContext _masterDb;
        private readonly TenantDbContextFactory _factory;

        public DeviceController(
            TenantDbContext masterDb,
            TenantDbContextFactory factory)
        {
            _masterDb = masterDb;
            _factory = factory;
        }

        // ★ JWTのtenantCodeからテナントDBを取得するメソッド
        private DeviceDbContext GetTenantDb()
        {
            // ① リクエストヘッダーまたはJWTから tenantCode を取得
            var tenantCode = User.FindFirst("tenantCode")?.Value;
            
            // ② ヘッダーから取得を試みる（JWT未実装時の暫定対応）
            if (string.IsNullOrWhiteSpace(tenantCode))
            {
                tenantCode = Request.Headers["X-Tenant-Code"].ToString();
            }

            // ③ クエリパラメーターから取得を試みる（デバッグ用）
            if (string.IsNullOrWhiteSpace(tenantCode))
            {
                tenantCode = Request.Query["tenantCode"].ToString();
            }

            // ④ Cookie から取得を試みる（ログイン後の自動保存）
            if (string.IsNullOrWhiteSpace(tenantCode))
            {
                Request.Cookies.TryGetValue("tenantCode", out tenantCode);
            }
            
            if (string.IsNullOrWhiteSpace(tenantCode))
                throw new Exception("tenantCode が JWT、ヘッダー(X-Tenant-Code)、クエリパラメータ(tenantCode)、または Cookie に含まれていません");

            // ⑤ masterDB から接続先情報を取得
            var tenant = _masterDb.Tenants.FirstOrDefault(t => t.TenantCode == tenantCode);
            if (tenant == null)
                throw new Exception($"MasterDB にテナント情報がありません (取得しようとした tenantCode: '{tenantCode}')");

            // ⑥ テナントDB用の接続文字列
            string connStr = $"Host=localhost;Port=5432;" + $"Database={tenant.TenantCode};" + $"Username=postgres;Password=Valtec;SslMode=Disable;";

            // ⑦ 動的に DeviceDbContext を生成
            return _factory.Create(connStr);
        }

        // 1) BodyCamera から認証モードを取得（未登録なら自動作成）
        [HttpPost("getAuthMode")]
        public IActionResult GetAuthMode([FromBody] SerialRequest req)
        {
            using var db = GetTenantDb(); // テナントDB
            if (req == null || string.IsNullOrWhiteSpace(req.SerialNo))
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ContentType = "text/plain; charset=utf-8",
                    Content = "serialNo は必須です。"
                };
            }

            var device = db.Devices.FirstOrDefault(x => x.SerialNo == req.SerialNo && !x.DelFlg);

            if (device == null)
            {
                device = new Device
                {
                    SerialNo = req.SerialNo,
                    DeviceName = "Unknown",
                    AuthMode = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.Devices.Add(device);
                db.SaveChanges();

                db.DeviceLogs.Add(new DeviceLog
                {
                    SerialNo = req.SerialNo,
                    Action = "デバイスを自動登録（未登録のため新規作成）",
                    CreatedAt = DateTime.UtcNow
                });

                db.SaveChanges();
            }

            if (!device.IsActive)
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    ContentType = "text/plain; charset=utf-8",
                    Content = "このデバイスは無効化されています。"
                };
            }

            return Ok(new
            {
                authMode = device.AuthMode,
                deviceName = device.DeviceName,
                isActive = device.IsActive
            });
        }

        // 2) BodyCamera からの簡易更新（serialNo で上書き）
        [HttpPost("update")]
        public IActionResult UpdateDevice([FromBody] UpdateDeviceRequest req)
        {
            using var db = GetTenantDb();
            if (string.IsNullOrWhiteSpace(req.SerialNo))
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ContentType = "text/plain; charset=utf-8",
                    Content = "SerialNo は必須です。"
                };
            }

            var device = db.Devices.FirstOrDefault(d => d.SerialNo == req.SerialNo);

            if (device == null)
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    ContentType = "text/plain; charset=utf-8",
                    Content = $"SerialNo '{req.SerialNo}' のデバイスが見つかりません。"
                };
            }

            device.AuthMode = req.AuthMode;
            device.DeviceName = req.DeviceName;
            device.IsActive = req.IsActive;
            device.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();

            return Ok(new
            {
                serialNo = device.SerialNo,
                authMode = device.AuthMode,
                deviceName = device.DeviceName,
                isActive = device.IsActive
            });
        }

        // 3) デバイス一覧を取得（削除済みを除外）
        [HttpGet]
        public IActionResult GetAllDevices()
        {
            using var db = GetTenantDb();
            var list = db.Devices
                .Where(d => !d.DelFlg)
                .OrderByDescending(d => d.Id)
                .ToList();

            return Ok(list);
        }

        // 4) 管理画面からデバイス新規登録
        [HttpPost]
        public IActionResult CreateDevice([FromBody] Device model)
        {
            using var db = GetTenantDb();
            if (string.IsNullOrWhiteSpace(model.SerialNo))
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ContentType = "text/plain; charset=utf-8",
                    Content = "SerialNo は必須です。"
                };
            }

            if (db.Devices.Any(x => x.SerialNo == model.SerialNo && !x.DelFlg))
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status409Conflict,
                    ContentType = "text/plain; charset=utf-8",
                    Content = "デバイスは既に存在します。"
                };
            }

            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            db.Devices.Add(model);
            db.SaveChanges();

            db.DeviceLogs.Add(new DeviceLog
            {
                SerialNo = model.SerialNo,
                Action = "デバイスを新規登録（手動）",
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();

            return Ok(model);
        }

        // 5) 管理画面からデバイス更新（authMode / deviceName / isActive）
        [HttpPut("{serialNo}")]
        public IActionResult UpdateDevice(string serialNo, [FromBody] Device model)
        {
            using var db = GetTenantDb();
            var device = db.Devices.FirstOrDefault(x => x.SerialNo == serialNo && !x.DelFlg);
            if (device == null)
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    ContentType = "text/plain; charset=utf-8",
                    Content = "デバイスが見つかりません。"
                };
            }

            device.DeviceName = model.DeviceName;
            device.AuthMode = model.AuthMode;
            device.IsActive = model.IsActive;
            device.UpdatedAt = DateTime.UtcNow;

            db.SaveChanges();

            db.DeviceLogs.Add(new DeviceLog
            {
                SerialNo = device.SerialNo,
                Action = $"デバイスを更新（認証モード={GetAuthModeLabel(model.AuthMode)}）",
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();

            return Ok(device);
        }

        // 6) ソフト削除（DelFlg = true）
        [HttpDelete("{serialNo}")]
        public IActionResult DeleteDevice(string serialNo)
        {
            using var db = GetTenantDb();
            var device = db.Devices.FirstOrDefault(x => x.SerialNo == serialNo && !x.DelFlg);
            if (device == null)
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    ContentType = "text/plain; charset=utf-8",
                    Content = "デバイスが見つかりません。"
                };
            }

            device.DelFlg = true;
            device.IsActive = false;
            device.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();

            db.DeviceLogs.Add(new DeviceLog
            {
                SerialNo = device.SerialNo,
                Action = "デバイスを削除（ソフト削除）",
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();

            return new ContentResult
            {
                StatusCode = StatusCodes.Status200OK,
                ContentType = "text/plain; charset=utf-8",
                Content = "デバイスを削除しました（ソフト削除）。"
            };
        }

        // 7) デバイス変更履歴を取得
        [HttpGet("logs/{serialNo}")]
        public IActionResult GetLogs(string serialNo)
        {
            using var db = GetTenantDb();
            var logs = db.DeviceLogs
                .Where(x => x.SerialNo == serialNo)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return Ok(logs);
        }

        private static string GetAuthModeLabel(int authMode)
        {
            return authMode switch
            {
                0 => "顔認証",
                1 => "静脈認証",
                2 => "顔＋静脈（二要素）",
                _ => $"不明 ({authMode})"
            };
        }
    }
}
