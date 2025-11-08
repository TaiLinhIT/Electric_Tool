using System.IO;

using Electric_Meter.Models;

using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;

namespace Electric_Meter.Services
{
    public class DatabaseSeeder
    {
        private readonly PowerTempWatchContext _context;

        public DatabaseSeeder(PowerTempWatchContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            await SeedSensorTypeAsync();
            await SeedActiveTypeAsync();
            await SeedControlCodeAsync();
            await SeedDevicesAsync();
            await SeedMachinesAsync();
        }

        private async Task SeedSensorTypeAsync()
        {
            if (!await _context.Set<SensorType>().AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "SeedData", "sensorType.json");
                if (File.Exists(path))
                {
                    var data = JsonConvert.DeserializeObject<List<SensorType>>(await File.ReadAllTextAsync(path));
                    if (data != null)
                    {
                        _context.Set<SensorType>().AddRange(data);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task SeedActiveTypeAsync()
        {
            if (!await _context.Set<ActiveType>().AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "SeedData", "activeType.json");
                if (File.Exists(path))
                {
                    var data = JsonConvert.DeserializeObject<List<ActiveType>>(await File.ReadAllTextAsync(path));
                    if (data != null)
                    {
                        _context.Set<ActiveType>().AddRange(data);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task SeedControlCodeAsync()
        {
            if (!await _context.Set<Controlcode>().AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "SeedData", "controlCode.json");
                if (File.Exists(path))
                {
                    var data = JsonConvert.DeserializeObject<List<Controlcode>>(await File.ReadAllTextAsync(path));
                    if (data != null)
                    {
                        _context.Set<Controlcode>().AddRange(data);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
        private async Task SeedDevicesAsync()
        {
            if (!await _context.Set<Device>().AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "SeedData", "devices.json");
                if (File.Exists(path))
                {
                    var data = JsonConvert.DeserializeObject<List<Device>>(await File.ReadAllTextAsync(path));
                    if (data != null)
                    {
                        _context.Set<Device>().AddRange(data);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
        private async Task SeedMachinesAsync()
        {
            if (!await _context.Set<Machine>().AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "SeedData", "dv_Machine.json");
                if (File.Exists(path))
                {
                    var data = JsonConvert.DeserializeObject<List<Machine>>(await File.ReadAllTextAsync(path));
                    if (data != null)
                    {
                        _context.Set<Machine>().AddRange(data);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

    }
}
