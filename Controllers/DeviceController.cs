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
        private readonly DeviceDbContext _context;

        public DeviceController(DeviceDbContext context)
        {
            _context = context;
        }

        // 1) BodyCamera から認証モードを取得（未登録なら自動作成）
        [HttpPost("getAuthMode")]
        public IActionResult GetAuthMode([FromBody] SerialRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.SerialNo))
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ContentType = "text/plain; charset=utf-8",
                    Content = "serialNo は必須です。"
                };
            }

            var device = _context.Devices.FirstOrDefault(x => x.SerialNo == req.SerialNo && !x.DelFlg);

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

                _context.Devices.Add(device);
                _context.SaveChanges();

                _context.DeviceLogs.Add(new DeviceLog
                {
                    SerialNo = req.SerialNo,
                    Action = "デバイスを自動登録（未登録のため新規作成）",
                    CreatedAt = DateTime.UtcNow
                });

                _context.SaveChanges();
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
            if (string.IsNullOrWhiteSpace(req.SerialNo))
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ContentType = "text/plain; charset=utf-8",
                    Content = "SerialNo は必須です。"
                };
            }

            var device = _context.Devices.FirstOrDefault(d => d.SerialNo == req.SerialNo);

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
            _context.SaveChanges();

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
            var list = _context.Devices
                .Where(d => !d.DelFlg)
                .OrderByDescending(d => d.Id)
                .ToList();

            return Ok(list);
        }

        // 4) 管理画面からデバイス新規登録
        [HttpPost]
        public IActionResult CreateDevice([FromBody] Device model)
        {
            if (string.IsNullOrWhiteSpace(model.SerialNo))
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ContentType = "text/plain; charset=utf-8",
                    Content = "SerialNo は必須です。"
                };
            }

            if (_context.Devices.Any(x => x.SerialNo == model.SerialNo && !x.DelFlg))
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

            _context.Devices.Add(model);
            _context.SaveChanges();

            _context.DeviceLogs.Add(new DeviceLog
            {
                SerialNo = model.SerialNo,
                Action = "デバイスを新規登録（手動）",
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            return Ok(model);
        }

        // 5) 管理画面からデバイス更新（authMode / deviceName / isActive）
        [HttpPut("{serialNo}")]
        public IActionResult UpdateDevice(string serialNo, [FromBody] Device model)
        {
            var device = _context.Devices.FirstOrDefault(x => x.SerialNo == serialNo && !x.DelFlg);
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

            _context.SaveChanges();

            _context.DeviceLogs.Add(new DeviceLog
            {
                SerialNo = device.SerialNo,
                Action = $"デバイスを更新（認証モード={GetAuthModeLabel(model.AuthMode)}）",
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            return Ok(device);
        }

        // 6) ソフト削除（DelFlg = true）
        [HttpDelete("{serialNo}")]
        public IActionResult DeleteDevice(string serialNo)
        {
            var device = _context.Devices.FirstOrDefault(x => x.SerialNo == serialNo && !x.DelFlg);
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
            _context.SaveChanges();

            _context.DeviceLogs.Add(new DeviceLog
            {
                SerialNo = device.SerialNo,
                Action = "デバイスを削除（ソフト削除）",
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

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
            var logs = _context.DeviceLogs
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
