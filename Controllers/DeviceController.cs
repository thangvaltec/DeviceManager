using Microsoft.AspNetCore.Mvc;
using DeviceApi.Data;
using DeviceApi.Models;
using System;
using System.Linq;

namespace DeviceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly DeviceDbContext _context;

        public DeviceController(DeviceDbContext context)
        {
            _context = context;
        }

        // ==== BodyCamera 用：シリアルから認証モードを取得（なければ自動作成）====
        // POST: /api/device/getAuthMode
        [HttpPost("getAuthMode")]
        public IActionResult GetAuthMode([FromBody] SerialRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.SerialNo))
            {
                return BadRequest("SerialNo is required.");
            }

            var device = _context.Devices.FirstOrDefault(x => x.SerialNo == req.SerialNo);

            if (device == null)
            {
                // 初回アクセスの場合、自動的にレコードを作成
                device = new Device
                {
                    SerialNo = req.SerialNo,
                    DeviceName = "Unknown",
                    AuthMode = 0,          // デフォルト: 顔認証
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Devices.Add(device);
                _context.SaveChanges();
            }

            return Ok(new
            {
                authMode = device.AuthMode,
                deviceName = device.DeviceName,
                isActive = device.IsActive
            });
        }

        // ====== Web 管理画面用：デバイス情報を更新 ======
        // POST: /api/device/update
        [HttpPost("update")]
        public IActionResult UpdateDevice([FromBody] UpdateDeviceRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.SerialNo))
            {
                return BadRequest("SerialNo is required.");
            }

            var device = _context.Devices.FirstOrDefault(d => d.SerialNo == req.SerialNo);

            if (device == null)
            {
                return NotFound($"Device with SerialNo '{req.SerialNo}' not found.");
            }

            device.AuthMode = req.AuthMode;
            device.DeviceName = req.DeviceName;
            device.IsActive = req.IsActive;
            device.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            return Ok(new
            {
                serialNo = device.SerialNo,
                authMode = device.AuthMode,
                deviceName = device.DeviceName,
                isActive = device.IsActive
            });
        }
    }

    // ==== Web 管理画面から送られてくる更新用リクエスト DTO ====
    public class UpdateDeviceRequest
    {
        public string SerialNo { get; set; } = "";
        public int AuthMode { get; set; }
        public string DeviceName { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
