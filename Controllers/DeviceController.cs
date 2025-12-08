using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using DeviceApi.Data;
using DeviceApi.Models;

namespace DeviceApi.Controllers
{
    public class UpdateDeviceRequest
    {
        public string SerialNo { get; set; } = "";
        public int AuthMode { get; set; }
        public string DeviceName { get; set; } = "";
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

        // 1) BodyCamera gọi: lấy AuthMode từ serialNo
        [HttpPost("getAuthMode")]
        public IActionResult GetAuthMode([FromBody] SerialRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.SerialNo))
            {
                return BadRequest(new { message = "serialNo is required" });
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
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Devices.Add(device);
                _context.SaveChanges();

                _context.DeviceLogs.Add(new DeviceLog
                {
                    SerialNo = req.SerialNo,
                    Action = "Device auto-created",
                    CreatedAt = DateTime.Now
                });

                _context.SaveChanges();
            }

            return Ok(new
            {
                authMode = device.AuthMode,
                deviceName = device.DeviceName,
                isActive = device.IsActive
            });
        }

        [HttpPost("update")]
        public IActionResult UpdateDevice([FromBody] UpdateDeviceRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.SerialNo))
            {
                return BadRequest("SerialNo is required.");
            }

            var device = _context.Devices.FirstOrDefault(d => d.SerialNo == req.SerialNo);

            if (device == null)
            {
                return NotFound($"Device with SerialNo '{req.SerialNo}' not found.");
            }

            // Cập nhật thông tin
            device.AuthMode = req.AuthMode;
            device.DeviceName = req.DeviceName;
            device.IsActive = req.IsActive;
            device.UpdatedAt = DateTime.Now;   // nếu trong model có cột này

            _context.SaveChanges();

            return Ok(new
            {
                serialNo = device.SerialNo,
                authMode = device.AuthMode,
                deviceName = device.DeviceName,
                isActive = device.IsActive
            });
        }

        // 2) GET /api/device ↁEdanh sách thiết bềE
        [HttpGet]
        public IActionResult GetAllDevices()
        {
            var list = _context.Devices
                .Where(d => !d.DelFlg)
                .OrderByDescending(d => d.Id)
                .ToList();

            return Ok(list);
        }

        // 3) POST /api/device ↁEthêm thiết bềE(quản lý từ web)
        [HttpPost]
        public IActionResult CreateDevice([FromBody] Device model)
        {
            if (string.IsNullOrWhiteSpace(model.SerialNo))
            {
                return BadRequest(new { message = "SerialNo is required" });
            }

            if (_context.Devices.Any(x => x.SerialNo == model.SerialNo && !x.DelFlg))
            {
                return Conflict(new { message = "デバイスは既に存在します" });
            }

            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            _context.Devices.Add(model);
            _context.SaveChanges();

            _context.DeviceLogs.Add(new DeviceLog
            {
                SerialNo = model.SerialNo,
                Action = "Device created manually",
                CreatedAt = DateTime.Now
            });
            _context.SaveChanges();

            return Ok(model);
        }

        // 4) PUT /api/device/{serialNo} ↁEupdate device (authMode, deviceName, isActive)
        [HttpPut("{serialNo}")]
        public IActionResult UpdateDevice(string serialNo, [FromBody] Device model)
        {
            var device = _context.Devices.FirstOrDefault(x => x.SerialNo == serialNo && !x.DelFlg);
            if (device == null)
            {
                return NotFound(new { message = "Device not found" });
            }

            device.DeviceName = model.DeviceName;
            device.AuthMode = model.AuthMode;
            device.IsActive = model.IsActive;
            device.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            _context.DeviceLogs.Add(new DeviceLog
            {
                SerialNo = device.SerialNo,
                Action = $"Device updated (AuthMode={model.AuthMode})",
                CreatedAt = DateTime.Now
            });
            _context.SaveChanges();

            return Ok(device);
        }

        [HttpDelete("{serialNo}")]
        public IActionResult DeleteDevice(string serialNo)
        {
            var device = _context.Devices.FirstOrDefault(x => x.SerialNo == serialNo && !x.DelFlg);
            if (device == null)
            {
                return NotFound(new { message = "Device not found" });
            }

            device.DelFlg = true;
            device.IsActive = false;
            device.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            _context.DeviceLogs.Add(new DeviceLog
            {
                SerialNo = device.SerialNo,
                Action = "Device soft-deleted",
                CreatedAt = DateTime.Now
            });
            _context.SaveChanges();

            return Ok(new { message = "Device marked as deleted" });
        }

        // 5) GET /api/device/logs/{serialNo} ↁElog thay đổi
        [HttpGet("logs/{serialNo}")]
        public IActionResult GetLogs(string serialNo)
        {
            var logs = _context.DeviceLogs
                .Where(x => x.SerialNo == serialNo)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return Ok(logs);
        }
    }
}
