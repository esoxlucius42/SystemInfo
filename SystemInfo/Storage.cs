using System.Collections.Generic;
using System.IO;

namespace SystemInfo
{
    class StorageDrive
    {
        public string Name { get; set; } = string.Empty;
        public double TotalGb { get; set; }
        public double FreeGb { get; set; }
        public double UsedFraction => TotalGb > 0 ? (TotalGb - FreeGb) / TotalGb : 0;

        public static List<StorageDrive> GetDrives()
        {
            var result = new List<StorageDrive>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                    continue;

                const double Gb = 1024.0 * 1024 * 1024;
                result.Add(new StorageDrive
                {
                    Name    = drive.Name,
                    TotalGb = drive.TotalSize / Gb,
                    FreeGb  = drive.TotalFreeSpace / Gb
                });
            }
            return result;
        }
    }
}
